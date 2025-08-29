import 'dotenv/config';
import express from 'express';
import Docker from 'dockerode';
import net from 'node:net';
import http from 'http';
import { WebSocketServer } from 'ws';
import crypto from 'crypto';
import session from 'express-session';
import { ErrorCodes, RoomState } from './errorCodes.js';
import { initDB, findUserByProvider, createUser, checkNicknameDuplicate, setUserNickname, generateTempNickname } from './db.js';
import passport from './auth.js';
import { OAuth2Client } from 'google-auth-library';

// --- 로깅 유틸리티 ---
const logger = {
    _log(level, ...args) {
        const now = new Date();
        const h = String(now.getHours()).padStart(2, '0');
        const m = String(now.getMinutes()).padStart(2, '0');
        const s = String(now.getSeconds()).padStart(2, '0');
        const ms = String(now.getMilliseconds()).padStart(3, '0');
        const timestamp = `[${h}:${m}:${s}.${ms}]`;

        // 모든 인자를 하나의 문자열로 변환하고 합칩니다.
        const message = args.map(arg => {
            if (typeof arg === 'object' && arg !== null) {
                return JSON.stringify(arg, null, 2);
            }
            return String(arg);
        }).join(' ');

        const stream = level === 'ERROR' || level === 'WARN' ? console.error : console.log;

        if (level === 'INFO') {
            stream(`${timestamp} ${message}`);
        } else if (level === 'WARN') {
            stream(`${timestamp} ⚠️ ${message}`);
        } else if (level === 'ERROR') {
            stream(`${timestamp} ❌ ${message}`);
        }
    },
    info(...args) { this._log('INFO', ...args); },
    warn(...args) { this._log('WARN', ...args); },
    error(...args) { this._log('ERROR', ...args); }
};
// --------------------

const app = express();
const httpServer = http.createServer(app);
const wss = new WebSocketServer({ server: httpServer });

// 미들웨어 설정
app.use(express.json());
app.use(session({
    secret: process.env.SESSION_SECRET || 'your-secret-key-change-this',
    resave: false,
    saveUninitialized: false,
    cookie: { maxAge: 24 * 60 * 60 * 1000 } // 24시간
}));
app.use(passport.initialize());
app.use(passport.session());

// WebSocket 연결 관리
const connections = new Map(); // ws -> { sessionId, nickname }

// Docker 연결 (플랫폼 자동 감지)
const docker = process.platform === 'win32'
    ? new Docker()
    : new Docker({ socketPath: '/var/run/docker.sock' });

// 상태 저장소
const rooms = new Map(); // roomId -> { containerId, ip, port, state }
const users = new Map(); // sessionId -> { nickname }
const pendingRequests = new Map(); // roomId -> [{ ws, type, responseData }]

// 설정
const IMAGE = 'dartagnan-gameserver:latest';
const INTERNAL_PORT = 7777;

// 게임 서버 공개 주소 (배포 환경에서는 도메인, 로컬에서는 localhost)
const PUBLIC_DOMAIN = process.env.PUBLIC_DOMAIN || '127.0.0.1';
logger.info(`게임 서버 공개 주소: ${PUBLIC_DOMAIN}`);

async function createRoom(roomId) {
    logger.info(`[방 ${roomId}] 생성 요청 접수`);

    if (rooms.has(roomId)) {
        logger.info(`[방 ${roomId}] 기존에 생성된 방이 존재하여 반환합니다.`);
        return rooms.get(roomId);
    }

    // Docker 컨테이너에서 로비 서버 접근 주소 자동 설정
    const lobbyUrl = process.platform === 'win32' || process.platform === 'darwin'
        ? `http://host.docker.internal:3000`
        : `http://172.17.0.1:3000`;
    logger.info(`[방 ${roomId}] 컨테이너 생성을 시작합니다. (로비 URL: ${lobbyUrl})`);

    const container = await docker.createContainer({
        Image: IMAGE,
        Env: [
            `PORT=${INTERNAL_PORT}`,
            `ROOM_ID=${roomId}`,
            `LOBBY_URL=${lobbyUrl}`
        ],
        ExposedPorts: { [`${INTERNAL_PORT}/tcp`]: {} },
        HostConfig: {
            PortBindings: { [`${INTERNAL_PORT}/tcp`]: [{ HostPort: '0' }] },
            AutoRemove: true
        }
    });
    logger.info(`[방 ${roomId}] 컨테이너 생성 완료: ${container.id.substring(0, 12)}`);

    await container.start();
    logger.info(`[방 ${roomId}] 컨테이너 시작 완료.`);

    const info = await container.inspect();
    const bindings = info.NetworkSettings.Ports[`${INTERNAL_PORT}/tcp`];
    const hostPort = bindings?.[0]?.HostPort;
    if (!hostPort) {
        logger.error(`[방 ${roomId}] 호스트 포트 바인딩 정보를 가져오는 데 실패했습니다.`);
        throw new Error('Failed to get host port binding');
    }
    logger.info(`[방 ${roomId}] 포트 바인딩 확인: ${PUBLIC_DOMAIN}:${hostPort}`);

    const room = { containerId: container.id, ip: PUBLIC_DOMAIN, port: Number(hostPort), state: -1 };
    rooms.set(roomId, room);
    logger.info(`[방 ${roomId}] 방 정보 사전 저장 완료 (상태: 대기중)`);

    logger.info(`[방 ${roomId}] 포트(${hostPort}) 활성화 대기를 시작합니다.`);
    logger.info(`[방 ${roomId}] 포트(${hostPort})가 활성화되었습니다.`);

    container.wait().then(() => {
        logger.info(`[방 ${roomId}] 컨테이너가 정지되어 관련 리소스를 정리합니다.`);
        rooms.delete(roomId);
        pendingRequests.delete(roomId);
    }).catch(() => {});

    return room;
}

function pickRandomWaitingRoom() {
    const waiting = Array.from(rooms.entries()).filter(([, r]) => r.state === RoomState.WAITING);
    return waiting.length > 0 ? waiting[Math.floor(Math.random() * waiting.length)][0] : null;
}

function generateRoomId() {
    return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}

// === OAuth 로그인 API ===

// Google OAuth 클라이언트 설정 (Client Secret 포함)
const googleClient = new OAuth2Client(
    process.env.GOOGLE_CLIENT_ID,
    process.env.GOOGLE_CLIENT_SECRET
);

// Unity에서 Google Authorization Code 검증용 API
app.post('/auth/google/verify-token', async (req, res) => {
    try {
        const { authCode } = req.body;
        
        if (!authCode) {
            return res.status(400).json({ error: 'Authorization Code is required.' });
        }

        logger.info(`[Unity OAuth] Received Auth Code: ${authCode.substring(0, 10)}...`);

        // Google Play Games Authorization Code를 직접 처리
        logger.info(`[Unity OAuth] Starting token exchange with Google`);
        logger.info(`[Unity OAuth] Using Client ID: ${process.env.GOOGLE_CLIENT_ID ? 'SET' : 'NOT_SET'}`);
        logger.info(`[Unity OAuth] Using Client Secret: ${process.env.GOOGLE_CLIENT_SECRET ? 'SET' : 'NOT_SET'}`);

        const tokenRequestBody = {
            code: authCode,
            client_id: process.env.GOOGLE_CLIENT_ID,
            client_secret: process.env.GOOGLE_CLIENT_SECRET,
            grant_type: 'authorization_code'
        };

        logger.info(`[Unity OAuth] Token request body:`, tokenRequestBody);

        const tokenResponse = await fetch('https://oauth2.googleapis.com/token', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: new URLSearchParams(tokenRequestBody)
        });

        logger.info(`[Unity OAuth] Token response status: ${tokenResponse.status}`);
        logger.info(`[Unity OAuth] Token response headers:`, Object.fromEntries(tokenResponse.headers));

        const tokens = await tokenResponse.json();
        logger.info(`[Unity OAuth] Token response body:`, tokens);
        
        if (!tokenResponse.ok) {
            throw new Error(`Google token exchange failed: ${JSON.stringify(tokens)}`);
        }
        
        if (!tokens.access_token) {
            throw new Error('No access token received from Google');
        }

        logger.info(`[Unity OAuth] Token exchange successful - Access token received`);

        // Google Play Games API를 사용하여 플레이어 정보 가져오기
        logger.info(`[Unity OAuth] Fetching player info with Google Play Games API`);
        logger.info(`[Unity OAuth] Access token (first 20 chars): ${tokens.access_token.substring(0, 20)}...`);
        
        // Google Play Games Player API 사용
        const playerInfoUrl = `https://www.googleapis.com/games/v1/players/me`;
        logger.info(`[Unity OAuth] Requesting player info from Play Games API`);
        
        const playerInfoResponse = await fetch(playerInfoUrl, {
            headers: {
                'Authorization': `Bearer ${tokens.access_token}`
            }
        });
        
        logger.info(`[Unity OAuth] Player info response status: ${playerInfoResponse.status}`);
        logger.info(`[Unity OAuth] Player info response headers:`, Object.fromEntries(playerInfoResponse.headers));
        
        const playerInfoText = await playerInfoResponse.text();
        logger.info(`[Unity OAuth] Player info raw response: ${playerInfoText}`);
        
        if (!playerInfoResponse.ok) {
            throw new Error(`Failed to fetch player info: ${playerInfoResponse.status} - ${playerInfoText}`);
        }
        
        const playerInfo = JSON.parse(playerInfoText);
        logger.info(`[Unity OAuth] Player info parsed:`, playerInfo);
        
        // Google Play Games API는 playerId와 displayName을 제공
        const providerId = playerInfo.playerId;
        const name = playerInfo.displayName;
        const email = null; // Play Games API는 이메일을 제공하지 않음
        
        if (!providerId) {
            throw new Error(`No player ID in Play Games response: ${playerInfoText}`);
        }
        
        logger.info(`[Unity OAuth] Google Play Games player info retrieved - ID: ${providerId}, DisplayName: ${name || 'N/A'}`);

        // 기존 OAuth 콜백과 동일한 로직
        let user = await findUserByProvider('google', providerId);
        let isTemporary = false;

        if (!user) {
            // 신규 사용자 → 즉시 임시 닉네임으로 회원가입
            const tempNickname = generateTempNickname();
            const userId = await createUser('google', providerId, tempNickname);
            
            user = { id: userId, nickname: tempNickname };
            isTemporary = true;
            logger.info(`[Unity OAuth] New user auto registration: ${email} → ${tempNickname}`);
        } else {
            // 기존 사용자 - 임시 닉네임인지 확인
            isTemporary = user.nickname.startsWith('User') && /^User[a-z0-9]+$/.test(user.nickname);
            logger.info(`[Unity OAuth] Existing user login: ${email} → ${user.nickname}`);
        }

        // sessionId 발급
        const sessionId = Math.random().toString(36).slice(2);
        users.set(sessionId, {
            id: user.id,
            nickname: user.nickname,
            isTemporary,
            provider: 'google',
            providerId,
            email,
            name
        });

        logger.info(`[Unity OAuth] Login processing complete: ${user.nickname} (${sessionId})`);
        res.json({
            success: true,
            sessionId,
            nickname: user.nickname,
            isTemporary
        });

    } catch (error) {
        logger.error(`[Unity OAuth] Token verification failed:`, error);
        res.status(401).json({ error: 'Invalid authorization code or token.' });
    }
});

// 구글 로그인 시작 (기존 웹 방식 유지)
app.get('/auth/google', passport.authenticate('google', { scope: ['profile', 'email'] }));

// 구글 로그인 콜백 (구글에서 돌아올 때)
app.get('/auth/google/callback', passport.authenticate('google', { failureRedirect: '/login' }), async (req, res) => {
    try {
        const { provider, providerId, email, name } = req.user;
        logger.info(`[OAuth] ${provider} 로그인 성공: ${email}`);

        // DB에서 기존 사용자 찾기
        let user = await findUserByProvider(provider, providerId);
        let isTemporary = false;

        if (!user) {
            // 신규 사용자 → 즉시 임시 닉네임으로 회원가입
            const tempNickname = generateTempNickname();
            const userId = await createUser(provider, providerId, tempNickname);
            
            user = { id: userId, nickname: tempNickname };
            isTemporary = true;
            logger.info(`[OAuth] 신규 사용자 자동 회원가입: ${email} → ${tempNickname}`);
        } else {
            // 기존 사용자 - 임시 닉네임인지 확인
            isTemporary = user.nickname.startsWith('User') && /^User[a-z0-9]+$/.test(user.nickname);
            logger.info(`[OAuth] 기존 사용자 로그인: ${email} → ${user.nickname}`);
        }

        // sessionId 발급
        const sessionId = Math.random().toString(36).slice(2);
        users.set(sessionId, {
            id: user.id,
            nickname: user.nickname,
            isTemporary,
            provider,
            providerId,
            email,
            name
        });

        logger.info(`[OAuth] 로그인 처리 완료: ${user.nickname} (${sessionId})`);
        res.json({
            success: true,
            sessionId,
            nickname: user.nickname,
            isTemporary
        });
    } catch (error) {
        logger.error(`[OAuth] 로그인 처리 중 오류:`, error);
        res.status(500).json({ error: '로그인 처리 중 오류가 발생했습니다.' });
    }
});

// === 기존 닉네임 로그인 (호환성 유지) ===
app.post('/login', (req, res) => {
    logger.info(`[로그인] 요청 수신:`, req.body);
    const nickname = (req.body?.nickname || '').trim();
    if (!nickname) {
        return res.status(400).json({ code: ErrorCodes.NULL_NICKNAME, message: '닉네임을 입력해주세요.' });
    }
    if (nickname.length < 1 || nickname.length > 16) {
        return res.status(400).json({ code: ErrorCodes.INVALID_NICKNAME, message: '닉네임은 1자 이상 16자 이하로 입력해주세요.' });
    }
    for (const user of users.values()) {
        if (user.nickname === nickname) {
            logger.warn(`[로그인] 닉네임 중복 시도: ${nickname}`);
            return res.status(409).json({ code: ErrorCodes.DUPLICATE_NICKNAME, message: '이미 사용 중인 닉네임입니다.' });
        }
    }
    const sessionId = Math.random().toString(36).slice(2);
    users.set(sessionId, { nickname });
    logger.info(`[로그인] 성공: ${nickname} (세션ID: ${sessionId})`);
    res.json({ sessionId, nickname });
});


// 게임서버 상태 업데이트
app.post('/internal/rooms/:roomId/state', (req, res) => {
    const { roomId } = req.params;
    const { state: newState } = req.body;
    logger.info(`[상태 업데이트] [방 ${roomId}] 상태 변경 요청: ${newState}`);

    const room = rooms.get(roomId);
    if (!room) {
        const currentRooms = Array.from(rooms.keys());
        logger.error(`[상태 업데이트] [방 ${roomId}] 존재하지 않는 방에 대한 요청입니다. (현재 방: ${currentRooms.join(', ') || '없음'})`);
        return res.status(404).json({ code: ErrorCodes.ROOM_NOT_FOUND, message: '방을 찾을 수 없습니다.' });
    }

    logger.info(`[방 ${roomId}] 상태 변경: ${room.state} -> ${newState}`);
    room.state = newState;

    if (newState === RoomState.WAITING && pendingRequests.has(roomId)) {
        const requests = pendingRequests.get(roomId);
        logger.info(`[방 ${roomId}] 준비 완료. 대기 중인 요청 ${requests.length}건에 대해 응답을 전송합니다.`);

        requests.forEach(({ ws, type, responseData }) => {
            logger.info(`[방 ${roomId}] 대기열 응답 전송: ${type}`);
            sendMessage(ws, type, responseData);
        });
        pendingRequests.delete(roomId);
    }

    res.json({ ok: true });
});

// WebSocket 메시지 처리
function sendMessage(ws, type, data) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type, ...data }));
    }
}

function sendError(ws, code) {
    sendMessage(ws, 'error', { code });
}

function addPendingRequest(roomId, ws, type, responseData) {
    if (!pendingRequests.has(roomId)) {
        pendingRequests.set(roomId, []);
    }
    pendingRequests.get(roomId).push({ ws, type, responseData });
    logger.info(`[방 ${roomId}] 요청이 대기열에 추가되었습니다. (현재 ${pendingRequests.get(roomId).length}개 대기)`);
}

function cleanupPendingRequests(ws) {
    let cleanedCount = 0;
    for (const [roomId, requests] of pendingRequests.entries()) {
        const originalLength = requests.length;
        const filteredRequests = requests.filter(req => req.ws !== ws);

        if (filteredRequests.length !== originalLength) {
            cleanedCount += originalLength - filteredRequests.length;
            if (filteredRequests.length === 0) {
                pendingRequests.delete(roomId);
            } else {
                pendingRequests.set(roomId, filteredRequests);
            }
        }
    }
    if (cleanedCount > 0) {
        logger.info(`[연결 해제] 사용자의 대기중인 요청 ${cleanedCount}개를 정리했습니다.`);
    }
}

wss.on('connection', (ws, req) => {
    const connectionId = crypto.randomBytes(4).toString('hex');
    logger.info(`[WebSocket] 새로운 연결 수립: ${connectionId}`);

    let authenticated = false;
    let sessionId = null;

    ws.on('message', async (message) => {
        try {
            const data = JSON.parse(message.toString());
            logger.info(`[WebSocket ${connectionId}] 메시지 수신:`, data);

            if (data.type === 'auth') {
                sessionId = data.sessionId;
                if (!sessionId || !users.has(sessionId)) {
                    logger.error(`[WebSocket ${connectionId}] 인증 실패: 유효하지 않은 세션ID (${sessionId})`);
                    sendError(ws, ErrorCodes.UNAUTHORIZED);
                    ws.close();
                    return;
                }
                authenticated = true;
                const user = users.get(sessionId);
                connections.set(ws, { sessionId, nickname: user.nickname });
                
                // 닉네임 및 임시 여부 포함하여 응답
                sendMessage(ws, 'auth_success', { 
                    ok: true, 
                    nickname: user.nickname,
                    isTemporary: user.isTemporary || false,
                    needNickname: !user.nickname || user.isTemporary
                });
                logger.info(`[WebSocket ${connectionId}] 인증 성공: ${user.nickname || '닉네임미설정'} (${sessionId})`);
                return;
            }

            if (!authenticated) {
                logger.warn(`[WebSocket ${connectionId}] 인증되지 않은 요청 시도: ${data.type}`);
                sendError(ws, ErrorCodes.NOT_AUTHENTICATED);
                return;
            }

            const { nickname } = connections.get(ws);

            if (data.type === 'set_nickname') {
                const requestedNickname = data.nickname?.trim();
                if (!requestedNickname || requestedNickname.length < 1 || requestedNickname.length > 16) {
                    sendError(ws, ErrorCodes.INVALID_NICKNAME);
                    return;
                }

                try {
                    const user = users.get(sessionId);
                    
                    // 닉네임 중복 체크
                    const isDuplicate = await checkNicknameDuplicate(requestedNickname);
                    if (isDuplicate) {
                        sendError(ws, ErrorCodes.DUPLICATE_NICKNAME);
                        return;
                    }

                    if (user.provider && user.providerId) {
                        // OAuth 사용자 - DB에 저장
                        if (user.id) {
                            // 기존 사용자 닉네임 업데이트
                            await setUserNickname(user.provider, user.providerId, requestedNickname);
                        } else {
                            // 신규 사용자 생성
                            const userId = await createUser(user.provider, user.providerId, requestedNickname);
                            user.id = userId;
                        }
                    }
                    
                    // 메모리 업데이트
                    user.nickname = requestedNickname;
                    user.isTemporary = false;  // 이제 임시 닉네임이 아님
                    connections.get(ws).nickname = requestedNickname;
                    
                    sendMessage(ws, 'nickname_set', { success: true, nickname: requestedNickname });
                    logger.info(`[닉네임설정] ${requestedNickname} (${sessionId})`);
                } catch (error) {
                    logger.error(`[닉네임설정] 오류:`, error);
                    sendMessage(ws, 'nickname_set', { success: false, error: '닉네임 설정에 실패했습니다.' });
                }
                return;
            }

            if (data.type === 'create_room') {
                logger.info(`[${nickname}] 방 생성 요청`);
                try {
                    const roomId = data.roomId || generateRoomId();
                    const room = await createRoom(roomId);
                    const responseData = { ok: true, roomId, ip: room.ip, port: room.port };

                    if (room.state === RoomState.WAITING) {
                        logger.info(`[방 ${roomId}] 즉시 응답 가능. 응답을 전송합니다.`);
                        sendMessage(ws, 'create_room_response', responseData);
                    } else {
                        logger.info(`[방 ${roomId}] 게임 서버 준비 대기. 요청을 큐에 추가합니다.`);
                        addPendingRequest(roomId, ws, 'create_room_response', responseData);
                    }
                } catch (e) {
                    logger.error(`[${nickname}] 방 생성 처리 중 오류 발생: ${e.message}`);
                    sendError(ws, ErrorCodes.ROOM_CREATE_FAILED);
                }
            }

            else if (data.type === 'join_room') {
                logger.info(`[${nickname}] 방 참가 요청`, data.roomId ? { roomId: data.roomId } : { random: true });
                try {
                    let roomId = data.roomId;
                    let room = null;
                    let responseData = null;

                    if (roomId) { // 특정 방 참가
                        room = rooms.get(roomId);
                        if (!room) {
                            sendError(ws, ErrorCodes.ROOM_NOT_FOUND);
                            return;
                        }
                        if (room.state !== RoomState.WAITING && room.state !== -1) {
                            sendError(ws, ErrorCodes.ROOM_NOT_JOINABLE);
                            return;
                        }
                        responseData = { ok: true, ip: room.ip, port: room.port };
                    } else { // 랜덤 매칭
                        roomId = pickRandomWaitingRoom() || generateRoomId();
                        logger.info(`[${nickname}] 참가할 방 결정: ${roomId}`);
                        room = await createRoom(roomId); // 없으면 생성, 있으면 정보 가져오기
                        responseData = { ok: true, ip: room.ip, port: room.port, roomId };
                    }

                    if (room.state === RoomState.WAITING) {
                        logger.info(`[방 ${roomId}] 즉시 응답 가능. 응답을 전송합니다.`);
                        sendMessage(ws, 'join_room_response', responseData);
                    } else {
                        logger.info(`[방 ${roomId}] 게임 서버 준비 대기. 요청을 큐에 추가합니다.`);
                        addPendingRequest(roomId, ws, 'join_room_response', responseData);
                    }
                } catch (e) {
                    logger.error(`[${nickname}] 방 참가 처리 중 오류 발생: ${e.message}`);
                    sendError(ws, ErrorCodes.ROOM_NOT_AVAILABLE);
                }
            }
        } catch (e) {
            logger.error(`[WebSocket ${connectionId}] 메시지 처리 오류: ${e.message}`);
            sendError(ws, ErrorCodes.INVALID_MESSAGE);
        }
    });

    ws.on('close', () => handleDisconnection(ws, 'close'));
    ws.on('error', (err) => handleDisconnection(ws, 'error', err));

    function handleDisconnection(ws, reason, err = null) {
        const connection = connections.get(ws);
        if (connection) {
            const { sessionId, nickname } = connection;
            logger.info(`[WebSocket] 연결 해제: ${nickname} (${sessionId}), 사유: ${reason}${err ? `, 오류: ${err.message}` : ''}`);
            users.delete(sessionId);
            connections.delete(ws);
            cleanupPendingRequests(ws);
        } else {
            logger.info(`[WebSocket ${connectionId}] 인증되지 않은 연결 해제, 사유: ${reason}`);
            connections.delete(ws);
        }
    }
});

const PORT = 3000;

// 서버 시작
async function startServer() {
    try {
        // DB 초기화
        await initDB();

        // 서버 시작
        httpServer.listen(PORT, () => {
            const baseUrl = process.env.BASE_URL || `http://localhost:${PORT}`;
            logger.info(`🚀 로비 서버가 포트 ${PORT}에서 실행됩니다.`);
            logger.info(`🔗 Google OAuth: ${baseUrl}/auth/google`);
        });
    } catch (error) {
        logger.error('서버 시작 실패:', error);
        process.exit(1);
    }
}

startServer();