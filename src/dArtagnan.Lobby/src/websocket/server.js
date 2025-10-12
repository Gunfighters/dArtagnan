import { WebSocketServer } from 'ws';
import { logger } from '../logger.js';
import { handlers, handleDisconnection } from './handlers.js';
import { cleanupPendingRequests } from '../rooms/manager.js';
import { getActiveSession } from '../auth/session.js';

/**
 * WebSocket 서버 생성
 */
export function createWebSocketServer(httpServer) {
    const wss = new WebSocketServer({ server: httpServer });

    wss.on('connection', (ws) => {
        handleConnection(ws);
    });

    logger.info('[WS][System] WebSocket 서버 시작');
}

/**
 * 새 WebSocket 연결 처리
 */
function handleConnection(ws) {
    logger.info('[WS][Guest] 새 연결 수립');

    // ws 객체에 직접 인증 상태 저장
    ws.authenticated = false;
    ws.providerId = null;

    ws.on('message', async (message) => {
        try {
            const data = JSON.parse(message.toString());

            // 로그용 닉네임 조회
            let nickname = 'Guest';
            if (ws.authenticated && ws.providerId) {
                const session = getActiveSession(ws.providerId);
                nickname = session?.nickname || 'Unknown';
            }

            logger.info(`[WS][${nickname}] 메시지 수신: ${data.type}`);

            // 인증되지 않은 상태에서는 auth만 허용
            if (!ws.authenticated && data.type !== 'auth') {
                sendError(ws, '로그인이 필요합니다.');
                return;
            }

            // 메시지 핸들러 실행
            const handler = handlers[data.type];
            if (handler) {
                await handler(ws, data);
            } else {
                sendError(ws, '알 수 없는 메시지 타입입니다.');
            }

        } catch (error) {
            let nickname = 'Guest';
            if (ws.authenticated && ws.providerId) {
                const session = getActiveSession(ws.providerId);
                nickname = session?.nickname || 'Unknown';
            }
            logger.error(`[WS][${nickname}] 메시지 처리 오류:`, error);
            sendError(ws, '잘못된 요청입니다.');
        }
    });

    ws.on('close', () => {
        handleDisconnection(ws, 'close');
        cleanupPendingRequests(ws);
    });

    ws.on('error', (err) => {
        handleDisconnection(ws, 'error', err);
        cleanupPendingRequests(ws);
    });
}

/**
 * 에러 메시지 전송
 */
function sendError(ws, message) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type: 'error', message }));
    }
}