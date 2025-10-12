import { config } from '../config.js';
import { logger } from '../logger.js';
import { createGameServerContainer, onContainerStop } from './docker.js';
import { broadcastToAll } from '../auth/session.js';
import { generateRandomRoomName } from './roomNames.js';

// 방 저장소
const internalRooms = new Map(); // roomId -> { containerId, ip, port, state }
const pendingRequests = new Map(); // roomId -> [{ ws, type, responseData }]

/**
 * 모든 방 목록 조회 (클라이언트용)
 */
export function getAllRoomsForClient() {
    return Array.from(internalRooms.entries()).map(([roomId, room]) => ({
        roomId,
        roomName: room.roomName,
        playerCount: room.playerCount,
        maxPlayers: room.maxPlayers,
        joinable: room.state === config.roomState.WAITING && room.playerCount < room.maxPlayers,
        ip: room.ip,
        port: room.port
    }));
}

/**
 * 방 목록 브로드캐스트
 */
function broadcastRoomListUpdate() {
    const rooms = getAllRoomsForClient();
    broadcastToAll({
        type: 'rooms_update',
        rooms: rooms
    });
}

/**
 * 방 정보 변경을 감지하는 Proxy 생성
 */
function createRoomProxy(room, roomId) {
    return new Proxy(room, {
        set(target, property, value) {
            const oldValue = target[property];
            target[property] = value;

            // 중요한 속성이 변경되었을 때만 브로드캐스트
            const importantProperties = ['state', 'roomName', 'playerCount', 'maxPlayers'];
            if (importantProperties.includes(property) && oldValue !== value) {
                logger.info(`[Room][Room-${roomId.substring(0, 6)}] 속성 변경: ${property}=${oldValue} → ${value}`);
                broadcastRoomListUpdate();
            }

            return true;
        }
    });
}

// Proxy로 래핑된 rooms Map
const rooms = new Proxy(internalRooms, {
    set(target, property, value) {
        if (property === 'set') {
            return target[property];
        }
        target[property] = value;
        return true;
    },
    get(target, property) {
        if (property === 'set') {
            return function(roomId, room) {
                const proxiedRoom = createRoomProxy(room, roomId);
                target.set(roomId, proxiedRoom);
                // 새 방 추가 시 브로드캐스트
                broadcastRoomListUpdate();
                return target;
            };
        }
        if (property === 'delete') {
            return function(roomId) {
                const result = target.delete(roomId);
                if (result) {
                    // 방 삭제 시 브로드캐스트
                    broadcastRoomListUpdate();
                }
                return result;
            };
        }
        if (property === 'has') {
            return function(roomId) {
                return target.has(roomId);
            };
        }
        if (property === 'get') {
            return function(roomId) {
                return target.get(roomId);
            };
        }
        if (property === 'entries') {
            return function() {
                return target.entries();
            };
        }
        if (property === 'keys') {
            return function() {
                return target.keys();
            };
        }
        if (property === 'values') {
            return function() {
                return target.values();
            };
        }
        return target[property];
    }
});

/**
 * 방 생성
 */
export async function createRoom(roomName = null) {
    // 방 이름이 제공되지 않으면 랜덤 생성
    const finalRoomName = roomName || generateRandomRoomName();

    // 고유한 방 ID 생성 (중복 방지)
    let roomId;
    do {
        roomId = generateRoomId();
    } while (rooms.has(roomId));

    logger.info(`[Room][Room-${roomId.substring(0, 6)}] 방 생성 시작: "${finalRoomName}"`);

    try {
        // Docker 컨테이너 생성
        const containerInfo = await createGameServerContainer(roomId, finalRoomName);

        // 방 정보 저장
        const room = {
            roomId: roomId,
            containerId: containerInfo.containerId,
            ip: containerInfo.ip,
            port: containerInfo.port,
            state: config.roomState.INITIALIZING,
            roomName: finalRoomName,
            playerCount: 0,
            //todo: maxPlayer상수박아 넣는게 아니라 아름답게 바꾸기
            maxPlayers: 8
        };

        rooms.set(roomId, room);
        logger.info(`[Room][Room-${roomId.substring(0, 6)}] 방 정보 저장 완료`);

        // 컨테이너 종료 시 정리
        onContainerStop(containerInfo.container, () => {
            logger.info(`[Room][Room-${roomId.substring(0, 6)}] 방 정리`);
            rooms.delete(roomId);
            pendingRequests.delete(roomId);
        });

        return room;

    } catch (error) {
        logger.error(`[Room][Room-${roomId.substring(0, 6)}] 방 생성 실패:`, error);
        throw error;
    }
}

/**
 * 대기 중인 방 중 랜덤 선택
 */
export function pickRandomWaitingRoom() {
    const waitingRooms = Array.from(rooms.entries())
        .filter(([, room]) => room.state === config.roomState.WAITING);

    if (waitingRooms.length === 0) {
        return null;
    }

    const randomIndex = Math.floor(Math.random() * waitingRooms.length);
    return waitingRooms[randomIndex][0]; // roomId 반환
}

/**
 * 방 정보 조회
 */
export function getRoom(roomId) {
    return rooms.get(roomId);
}


/**
 * 방 상태 업데이트
 */
export function updateRoomState(roomId, newState) {
    const room = rooms.get(roomId);
    if (!room) {
        return null;
    }

    logger.info(`[Room][Room-${roomId.substring(0, 6)}] 상태 변경: ${room.state} → ${newState}`);
    room.state = newState;

    // 대기 중인 요청 처리
    if (newState === config.roomState.WAITING && pendingRequests.has(roomId)) {
        const requests = pendingRequests.get(roomId);
        logger.info(`[Room][Room-${roomId.substring(0, 6)}] 대기 요청 처리: ${requests.length}건`);

        requests.forEach(({ ws, type, responseData }) => {
            if (ws.readyState === ws.OPEN) {
                ws.send(JSON.stringify({ type, ...responseData }));
            }
        });
        pendingRequests.delete(roomId);
    }

    return room;
}

/**
 * 방 제목 업데이트
 */
export function updateRoomName(roomId, newRoomName) {
    const room = rooms.get(roomId);
    if (!room) {
        return null;
    }

    logger.info(`[Room][Room-${roomId.substring(0, 6)}] 제목 변경: "${room.roomName}" → "${newRoomName}"`);
    room.roomName = newRoomName;

    return room;
}

/**
 * 방 인원수 업데이트
 */
export function updatePlayerCount(roomId, playerCount) {
    const room = rooms.get(roomId);
    if (!room) {
        return null;
    }

    logger.info(`[Room][Room-${roomId.substring(0, 6)}] 인원수 변경: ${room.playerCount} → ${playerCount}`);
    room.playerCount = playerCount;

    return room;
}

/**
 * 대기 요청 추가
 */
export function addPendingRequest(roomId, ws, type, responseData) {
    if (!pendingRequests.has(roomId)) {
        pendingRequests.set(roomId, []);
    }
    pendingRequests.get(roomId).push({ ws, type, responseData });
    logger.info(`[Room][Room-${roomId.substring(0, 6)}] 대기 요청 추가: 현재 ${pendingRequests.get(roomId).length}건`);
}

/**
 * 연결 해제 시 대기 요청 정리
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
        logger.info(`[Room][System] 대기 요청 정리: ${cleanedCount}건`);
    }
}

/**
 * 모든 방 ID 목록
 */
export function getAllRoomIds() {
    return Array.from(rooms.keys());
}

/**
 * 방 ID 생성
 */
export function generateRoomId() {
    return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}

// 방 상태 상수 export
export const RoomState = config.roomState;