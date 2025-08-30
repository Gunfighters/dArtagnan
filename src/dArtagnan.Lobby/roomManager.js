import Docker from 'dockerode';
import { RoomState } from './errorCodes.js';

// Docker 연결 (플랫폼 자동 감지)
const docker = process.platform === 'win32'
    ? new Docker()
    : new Docker({ socketPath: '/var/run/docker.sock' });

// 설정
const IMAGE = 'dartagnan-gameserver:latest';
const INTERNAL_PORT = 7777;
const PUBLIC_DOMAIN = process.env.PUBLIC_DOMAIN || '127.0.0.1';

// 상태 저장소
const rooms = new Map(); // roomId -> { containerId, ip, port, state }
const pendingRequests = new Map(); // roomId -> [{ ws, type, responseData }]

// 로깅 유틸리티
const logger = {
    _log(level, ...args) {
        const now = new Date();
        const h = String(now.getHours()).padStart(2, '0');
        const m = String(now.getMinutes()).padStart(2, '0');
        const s = String(now.getSeconds()).padStart(2, '0');
        const ms = String(now.getMilliseconds()).padStart(3, '0');
        const timestamp = `[${h}:${m}:${s}.${ms}]`;

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

/**
 * 방 생성
 */
export async function createRoom(roomId) {
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

    // 컨테이너 종료 시 정리
    container.wait().then(() => {
        logger.info(`[방 ${roomId}] 컨테이너가 정지되어 관련 리소스를 정리합니다.`);
        rooms.delete(roomId);
        pendingRequests.delete(roomId);
    }).catch(() => {});

    return room;
}

/**
 * 대기 중인 방 중 랜덤 선택
 */
export function pickRandomWaitingRoom() {
    const waiting = Array.from(rooms.entries()).filter(([, r]) => r.state === RoomState.WAITING);
    return waiting.length > 0 ? waiting[Math.floor(Math.random() * waiting.length)][0] : null;
}

/**
 * 방 ID 생성
 */
export function generateRoomId() {
    return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}

/**
 * 방 상태 업데이트
 */
export function updateRoomState(roomId, newState) {
    const room = rooms.get(roomId);
    if (!room) {
        return null;
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

    return room;
}

/**
 * 대기 중인 요청 추가
 */
export function addPendingRequest(roomId, ws, type, responseData) {
    if (!pendingRequests.has(roomId)) {
        pendingRequests.set(roomId, []);
    }
    pendingRequests.get(roomId).push({ ws, type, responseData });
    logger.info(`[방 ${roomId}] 요청이 대기열에 추가되었습니다. (현재 ${pendingRequests.get(roomId).length}개 대기)`);
}

/**
 * 연결 해제 시 대기 중인 요청 정리
 */
export function cleanupPendingRequests(ws) {
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

/**
 * 방 정보 반환
 */
export function getRoom(roomId) {
    return rooms.get(roomId);
}

/**
 * 모든 방 ID 반환
 */
export function getAllRoomIds() {
    return Array.from(rooms.keys());
}

// WebSocket 메시지 전송 (의존성 주입 방식으로 사용)
function sendMessage(ws, type, data) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type, ...data }));
    }
}