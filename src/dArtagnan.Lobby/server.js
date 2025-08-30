import 'dotenv/config';
import express from 'express';
import http from 'http';
import { WebSocketServer } from 'ws';
import crypto from 'crypto';
import { ErrorCodes, RoomState } from './errorCodes.js';
import { initDB, checkNicknameDuplicate, setUserNickname, createUser } from './db.js';
import { processUnityOAuth, processDevLogin } from './oauth.js';
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

// OAuth 전용 임시 세션 (HTTP → WebSocket 연결용)
const oauthSessions = new Map(); // sessionId -> { user, createdAt }

// 게임 서버 공개 주소 로그
const PUBLIC_DOMAIN = process.env.PUBLIC_DOMAIN || '127.0.0.1';
logger.info(`게임 서버 공개 주소: ${PUBLIC_DOMAIN}`);

// OAuth 세션 TTL (5분)
const OAUTH_SESSION_TIMEOUT = 5 * 60 * 1000; // 5분
const OAUTH_CLEANUP_INTERVAL = 60 * 1000;    // 1분마다 체크

function cleanupOAuthSessions() {
    const now = Date.now();
    let cleanedCount = 0;
    
    for (const [sessionId, session] of oauthSessions) {
        if (now - session.createdAt > OAUTH_SESSION_TIMEOUT) {
            oauthSessions.delete(sessionId);
            cleanedCount++;
        }
    }
    
    if (cleanedCount > 0) {
        logger.info(`[OAuth 세션 정리] ${cleanedCount}개의 미연결 세션을 정리했습니다.`);
    }
}

// OAuth 세션 정기 정리 시작
setInterval(cleanupOAuthSessions, OAUTH_CLEANUP_INTERVAL);
logger.info(`[OAuth 세션 관리] 미연결 세션 자동 정리 시작 (${OAUTH_SESSION_TIMEOUT/60000}분 타임아웃)`);

// === Unity OAuth API ===
app.post('/auth/google/verify-token', async (req, res) => {
    try {
        const { authCode } = req.body;
        const result = await processUnityOAuth(authCode, oauthSessions);
        res.json(result);
    } catch (error) {
        res.status(401).json({ error: 'Invalid authorization code or token.' });
    }
});

// === 개발용 로그인 (OAuth 구조 사용) ===
app.post('/login', async (req, res) => {
    try {
        const { providerId } = req.body;
        const result = await processDevLogin(providerId, oauthSessions);
        res.json(result);
    } catch (error) {
        let statusCode = 400;
        let errorCode = ErrorCodes.INVALID_NICKNAME;
        
        if (error.message.includes('Nickname is required')) {
            errorCode = ErrorCodes.NULL_NICKNAME;
        } else if (error.message.includes('already exists')) {
            statusCode = 409;
            errorCode = ErrorCodes.DUPLICATE_NICKNAME;
        }
        
        res.status(statusCode).json({ 
            error: error.message,
            code: errorCode 
        });
    }
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
    let currentUser = null; // 사용자 정보 로컬 저장

    ws.on('message', async (message) => {
        try {
            const data = JSON.parse(message.toString());
            logger.info(`[WebSocket ${connectionId}] 메시지 수신:`, data);

            // 인증 처리
            if (data.type === 'auth') {
                const sessionId = data.sessionId;
                const session = oauthSessions.get(sessionId);
                
                if (!sessionId || !session) {
                    logger.error(`[WebSocket ${connectionId}] 인증 실패: 유효하지 않은 세션ID (${sessionId})`);
                    sendError(ws, ErrorCodes.UNAUTHORIZED);
                    ws.close();
                    return;
                }
                
                authenticated = true;
                currentUser = session.user; // 사용자 정보 복사
                
                // OAuth 세션 즉시 삭제 (목적 달성)
                oauthSessions.delete(sessionId);
                
                sendMessage(ws, 'auth_success', { 
                    ok: true, 
                    nickname: currentUser.nickname,
                    needSetNickname: currentUser.needSetNickname || false,
                    needNickname: !currentUser.nickname || currentUser.needSetNickname
                });
                logger.info(`[WebSocket ${connectionId}] 인증 성공: ${currentUser.nickname || '닉네임미설정'} (${sessionId})`);
                return;
            }

            // 인증되지 않은 요청 차단
            if (!authenticated || !currentUser) {
                logger.warn(`[WebSocket ${connectionId}] 인증되지 않은 요청 시도: ${data.type}`);
                sendError(ws, ErrorCodes.NOT_AUTHENTICATED);
                return;
            }

            // 모든 로직에서 currentUser 직접 사용
            const { nickname } = currentUser;

            // 닉네임 설정
            if (data.type === 'set_nickname') {
                const requestedNickname = data.nickname?.trim();
                if (!requestedNickname || requestedNickname.length < 1 || requestedNickname.length > 16) {
                    sendError(ws, ErrorCodes.INVALID_NICKNAME);
                    return;
                }

                try {
                    // 닉네임 중복 체크
                    const isDuplicate = await checkNicknameDuplicate(requestedNickname);
                    if (isDuplicate) {
                        sendError(ws, ErrorCodes.DUPLICATE_NICKNAME);
                        return;
                    }

                    if (currentUser.provider && currentUser.providerId) {
                        // OAuth 사용자 - DB에 저장
                        if (currentUser.id) {
                            await setUserNickname(currentUser.provider, currentUser.providerId, requestedNickname);
                        } else {
                            const userId = await createUser(currentUser.provider, currentUser.providerId, requestedNickname, currentUser.is_guest || false);
                            currentUser.id = userId;
                        }
                    }
                    
                    // 로컬 사용자 정보 업데이트
                    currentUser.nickname = requestedNickname;
                    currentUser.needSetNickname = false;
                    
                    sendMessage(ws, 'nickname_set', { success: true, nickname: requestedNickname });
                    logger.info(`[닉네임설정] ${requestedNickname} (${connectionId})`);
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
        if (authenticated && currentUser) {
            logger.info(`[WebSocket] 연결 해제: ${currentUser.nickname} (${connectionId}), 사유: ${reason}${err ? `, 오류: ${err.message}` : ''}`);
        } else {
            logger.info(`[WebSocket ${connectionId}] 인증되지 않은 연결 해제, 사유: ${reason}`);
        }
        
        cleanupPendingRequests(ws);
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