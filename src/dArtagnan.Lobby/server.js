import 'dotenv/config';
import express from 'express';
import http from 'http';
import { WebSocketServer } from 'ws';
import crypto from 'crypto';
import { ErrorCodes, RoomState } from './errorCodes.js';
import { initDB, checkNicknameDuplicate, setUserNickname, createUser } from './db.js';
import { processUnityOAuth } from './oauth.js';
import { 
    createRoom, 
    pickRandomWaitingRoom, 
    generateRoomId, 
    updateRoomState, 
    addPendingRequest, 
    cleanupPendingRequests,
    getRoom,
    getAllRoomIds
} from './roomManager.js';
import { logger } from './logger.js';

const app = express();
const httpServer = http.createServer(app);
const wss = new WebSocketServer({ server: httpServer });

// 미들웨어 설정
app.use(express.json());

// 연결 관리
const connections = new Map(); // ws -> { sessionId, nickname }
const users = new Map(); // sessionId -> { nickname, ... }

// 게임 서버 공개 주소 로그
const PUBLIC_DOMAIN = process.env.PUBLIC_DOMAIN || '127.0.0.1';
logger.info(`게임 서버 공개 주소: ${PUBLIC_DOMAIN}`);

// === Unity OAuth API ===
app.post('/auth/google/verify-token', async (req, res) => {
    try {
        const { authCode } = req.body;
        const result = await processUnityOAuth(authCode, users);
        res.json(result);
    } catch (error) {
        res.status(401).json({ error: 'Invalid authorization code or token.' });
    }
});

// === 닉네임 로그인 (개발용) ===
app.post('/login', (req, res) => {
    logger.info(`[로그인] 요청 수신:`, req.body);
    const nickname = (req.body?.nickname || '').trim();
    
    if (!nickname) {
        return res.status(400).json({ code: ErrorCodes.NULL_NICKNAME, message: '닉네임을 입력해주세요.' });
    }
    if (nickname.length < 1 || nickname.length > 16) {
        return res.status(400).json({ code: ErrorCodes.INVALID_NICKNAME, message: '닉네임은 1자 이상 16자 이하로 입력해주세요.' });
    }
    
    // 중복 검사
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

// === 게임서버 상태 업데이트 ===
app.post('/internal/rooms/:roomId/state', (req, res) => {
    const { roomId } = req.params;
    const { state: newState } = req.body;
    logger.info(`[상태 업데이트] [방 ${roomId}] 상태 변경 요청: ${newState}`);

    const room = updateRoomState(roomId, newState);
    if (!room) {
        const currentRooms = getAllRoomIds();
        logger.error(`[상태 업데이트] [방 ${roomId}] 존재하지 않는 방에 대한 요청입니다. (현재 방: ${currentRooms.join(', ') || '없음'})`);
        return res.status(404).json({ code: ErrorCodes.ROOM_NOT_FOUND, message: '방을 찾을 수 없습니다.' });
    }

    res.json({ ok: true });
});

// === WebSocket 처리 ===
function sendMessage(ws, type, data) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type, ...data }));
    }
}

function sendError(ws, code) {
    sendMessage(ws, 'error', { code });
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

            // 인증 처리
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
                
                sendMessage(ws, 'auth_success', { 
                    ok: true, 
                    nickname: user.nickname,
                    isTemporary: user.isTemporary || false,
                    needNickname: !user.nickname || user.isTemporary
                });
                logger.info(`[WebSocket ${connectionId}] 인증 성공: ${user.nickname || '닉네임미설정'} (${sessionId})`);
                return;
            }

            // 인증되지 않은 요청 차단
            if (!authenticated) {
                logger.warn(`[WebSocket ${connectionId}] 인증되지 않은 요청 시도: ${data.type}`);
                sendError(ws, ErrorCodes.NOT_AUTHENTICATED);
                return;
            }

            const { nickname } = connections.get(ws);

            // 닉네임 설정
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
                            await setUserNickname(user.provider, user.providerId, requestedNickname);
                        } else {
                            const userId = await createUser(user.provider, user.providerId, requestedNickname);
                            user.id = userId;
                        }
                    }
                    
                    // 메모리 업데이트
                    user.nickname = requestedNickname;
                    user.isTemporary = false;
                    connections.get(ws).nickname = requestedNickname;
                    
                    sendMessage(ws, 'nickname_set', { success: true, nickname: requestedNickname });
                    logger.info(`[닉네임설정] ${requestedNickname} (${sessionId})`);
                } catch (error) {
                    logger.error(`[닉네임설정] 오류:`, error);
                    sendMessage(ws, 'nickname_set', { success: false, error: '닉네임 설정에 실패했습니다.' });
                }
                return;
            }

            // 방 생성
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

            // 방 참가
            else if (data.type === 'join_room') {
                logger.info(`[${nickname}] 방 참가 요청`, data.roomId ? { roomId: data.roomId } : { random: true });
                try {
                    let roomId = data.roomId;
                    let room = null;
                    let responseData = null;

                    if (roomId) {
                        // 특정 방 참가
                        room = getRoom(roomId);
                        if (!room) {
                            sendError(ws, ErrorCodes.ROOM_NOT_FOUND);
                            return;
                        }
                        if (room.state !== RoomState.WAITING && room.state !== -1) {
                            sendError(ws, ErrorCodes.ROOM_NOT_JOINABLE);
                            return;
                        }
                        responseData = { ok: true, ip: room.ip, port: room.port };
                    } else {
                        // 랜덤 매칭
                        roomId = pickRandomWaitingRoom() || generateRoomId();
                        logger.info(`[${nickname}] 참가할 방 결정: ${roomId}`);
                        room = await createRoom(roomId);
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

// === 서버 시작 ===
async function startServer() {
    try {
        await initDB();
        
        httpServer.listen(PORT, () => {
            const baseUrl = process.env.BASE_URL || `http://localhost:${PORT}`;
            logger.info(`🚀 로비 서버가 포트 ${PORT}에서 실행됩니다.`);
            logger.info(`🔗 Unity OAuth: ${baseUrl}/auth/google/verify-token`);
        });
    } catch (error) {
        logger.error('서버 시작 실패:', error);
        process.exit(1);
    }
}

startServer();