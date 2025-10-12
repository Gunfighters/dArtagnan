import { logger } from '../logger.js';
import { config } from '../config.js';
import { ShopBox1 } from '../constants.js';
import {
    checkNicknameDuplicate,
    setUserNickname,
    createUser,
    updateUserGold,
    updateUserCostume,
    addCostumeToInventory,
    getUserInventory,
    updateLastLogout
} from '../database.js';
import {
    verifyOAuthSession,
    removeOAuthSession,
    getActiveSession,
    setActiveSession,
    removeActiveSession,
    getUserCurrentRoom,
    updateSessionGold,
    updateSessionNickname,
    updateSessionCostume,
    updateSessionUserId,
    getSessionUser
} from '../auth/session.js';
import {
    createRoom,
    getRoom,
    getAllRoomsForClient,
    pickRandomWaitingRoom,
    addPendingRequest,
    updateRoomName,
    RoomState
} from '../rooms/manager.js';

/**
 * 메시지 핸들러 맵
 */
export const handlers = {
    auth: handleAuth,
    set_nickname: handleSetNickname,
    create_room: handleCreateRoom,
    join_room: handleJoinRoom,
    update_room_name: handleUpdateRoomName,
    shop_costume_box1: handleShopCostumeBox1,
    get_costume_rates: handleGetCostumeRates,
    change_costume: handleChangeCostume
};

/* =========================
   확률/샘플링 유틸 함수들
   ========================= */

/**
 * 티어 엔트리 정규화 및 필터링
 * - 빈 티어 제거(list.length > 0)
 * - 가중치가 양수인 티어만 사용(weight > 0)
 * - 가중치 합이 1이 아니어도 정규화
 * - 합이 0이면 균등 분배
 * 반환: [{ tier, weight(정규화됨), list }]
 */
function getNormalizedNonEmptyTiers(shop) {
    const entries = Object.entries(shop.TIER_WEIGHTS)
        .map(([tier, w]) => ({
            tier,
            weight: Number(w) || 0,
            list: Array.isArray(shop.TIERS[tier]) ? shop.TIERS[tier] : []
        }))
        .filter(x => x.list.length > 0 && x.weight > 0);

    if (entries.length === 0) return [];

    const sum = entries.reduce((a, x) => a + x.weight, 0);
    if (sum <= 0) {
        const equal = 1 / entries.length;
        return entries.map(x => ({ ...x, weight: equal }));
    }
    return entries.map(x => ({ ...x, weight: x.weight / sum }));
}

/**
 * 경계 보정 포함 티어 샘플
 * - r < 누적합 조건으로 못 잡히는 극소 경계값을 위해 기본값을 마지막으로 둠
 */
function sampleTier(normalizedEntries) {
    let r = Math.random();
    let cum = 0;
    let chosen = normalizedEntries[normalizedEntries.length - 1]; // 기본값: 마지막 티어
    for (const x of normalizedEntries) {
        cum += x.weight;
        if (r < cum) {
            chosen = x;
            break;
        }
    }
    return chosen;
}

/**
 * 한 번의 코스튬 추첨
 * - 빈 티어 제외/정규화 → 티어 샘플 → 해당 티어 내 균등 샘플
 */
function drawCostumeOnce(shop) {
    const normalized = getNormalizedNonEmptyTiers(shop);
    if (normalized.length === 0) {
        throw new Error('No available costumes across tiers (all tiers empty or non-positive weights).');
    }
    const chosenTier = sampleTier(normalized);
    const list = chosenTier.list;
    const idx = Math.floor(Math.random() * list.length);
    return list[idx];
}

/* =========================
   인증/기본 핸들러
   ========================= */

async function handleAuth(ws, data) {
    // [REMOVED FOR PORTFOLIO]
}

async function handleSetNickname(ws, data) {
    const { nickname: requestedNickname } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, '세션을 찾을 수 없습니다.');
        return;
    }

    if (!requestedNickname || requestedNickname.trim().length < 2 || requestedNickname.trim().length > 8) {
        sendMessage(ws, 'nickname_set', { success: false, error: '닉네임은 2-8자여야 합니다.' });
        return;
    }

    const cleanNickname = requestedNickname.trim();

    try {
        const isDuplicate = await checkNicknameDuplicate(cleanNickname);
        if (isDuplicate) {
            sendError(ws, '이미 사용 중인 닉네임입니다.');
            return;
        }

        if (session.provider && session.providerId) {
            if (session.userId) {
                await setUserNickname(session.provider, session.providerId, cleanNickname);
            } else {
                const userId = await createUser(session.provider, session.providerId, cleanNickname);
                updateSessionUserId(providerId, userId);
            }
        }

        updateSessionNickname(providerId, cleanNickname); // 자동으로 update_nickname 전송

        sendMessage(ws, 'nickname_set', { success: true, nickname: cleanNickname });

        logger.info(`[Auth][${cleanNickname}] 닉네임 설정 완료`);

    } catch (error) {
        logger.error('[Auth][Unknown] 닉네임 설정 오류:', error);
        sendMessage(ws, 'nickname_set', { success: false, error: '닉네임 설정에 실패했습니다.' });
    }
}

async function handleCreateRoom(ws, data) {
    const { roomName } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, '세션을 찾을 수 없습니다.');
        return;
    }

    logger.info(`[Room][${session.nickname}] 방 생성 요청: "${roomName || '랜덤'}"`);

    try {
        const room = await createRoom(roomName);
        const responseData = { ok: true, roomId: room.roomId, roomName: room.roomName, ip: room.ip, port: room.port };

        if (room.state === RoomState.WAITING) {
            logger.info(`[Room][Room-${room.roomId.substring(0, 6)}] 즉시 입장 가능`);
            sendMessage(ws, 'create_room_response', responseData);
        } else {
            logger.info(`[Room][Room-${room.roomId.substring(0, 6)}] 준비 대기 중`);
            addPendingRequest(room.roomId, ws, 'create_room_response', responseData);
        }

    } catch (error) {
        logger.error(`[Room][${session.nickname}] 방 생성 실패:`, error);
        sendError(ws, '방 생성에 실패했습니다.');
    }
}

async function handleJoinRoom(ws, data) {
    const { roomId } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, '세션을 찾을 수 없습니다.');
        return;
    }

    logger.info(`[Room][${session.nickname}] 방 참가 요청:`, roomId ? `Room-${roomId.substring(0, 6)}` : '랜덤 매칭');

    try {
        let targetRoomId = roomId;
        let room = null;
        let responseData = null;

        if (targetRoomId) {
            room = getRoom(targetRoomId);
            if (!room) {
                sendError(ws, '방을 찾을 수 없습니다.');
                return;
            }
            if (room.state !== RoomState.WAITING && room.state !== RoomState.INITIALIZING) {
                sendError(ws, '참가할 수 없는 방 상태입니다.');
                return;
            }
            responseData = { ok: true, roomId: targetRoomId, roomName: room.roomName, ip: room.ip, port: room.port };
        } else {
            targetRoomId = pickRandomWaitingRoom();
            if (targetRoomId) {
                room = getRoom(targetRoomId);
                responseData = { ok: true, roomId: targetRoomId, roomName: room.roomName, ip: room.ip, port: room.port };
            } else {
                room = await createRoom(); // 랜덤 이름 생성
                targetRoomId = room.roomId;
                responseData = { ok: true, roomId: targetRoomId, roomName: room.roomName, ip: room.ip, port: room.port };
            }
        }

        if (room.state === RoomState.WAITING) {
            logger.info(`[Room][Room-${targetRoomId.substring(0, 6)}] 즉시 입장 가능`);
            sendMessage(ws, 'join_room_response', responseData);
        } else {
            logger.info(`[Room][Room-${targetRoomId.substring(0, 6)}] 준비 대기 중`);
            addPendingRequest(targetRoomId, ws, 'join_room_response', responseData);
        }

    } catch (error) {
        logger.error(`[Room][${session.nickname}] 방 참가 실패:`, error);
        sendError(ws, '방 참가에 실패했습니다.');
    }
}

async function handleUpdateRoomName(ws, data) {
    const { roomName } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, '세션을 찾을 수 없습니다.');
        return;
    }

    if (!roomName) {
        sendError(ws, '방 제목이 필요합니다.');
        return;
    }
    if (roomName.trim().length < 1 || roomName.trim().length > 20) {
        sendError(ws, '방 제목은 1-20자여야 합니다.');
        return;
    }

    const currentRoomId = getUserCurrentRoom(providerId);
    if (!currentRoomId) {
        sendError(ws, '현재 방에 참가하지 않았습니다.');
        return;
    }

    logger.info(`[Room][${session.nickname}] 방 제목 변경 요청: "${roomName}" (Room-${currentRoomId.substring(0, 6)})`);

    try {
        const room = updateRoomName(currentRoomId, roomName.trim());
        if (!room) {
            sendError(ws, '방을 찾을 수 없습니다.');
            return;
        }

        sendMessage(ws, 'update_room_name_response', {
            roomId: currentRoomId,
            roomName: room.roomName
        });

        logger.info(`[Room][Room-${currentRoomId.substring(0, 6)}] 제목 변경 완료: "${room.roomName}"`);

    } catch (error) {
        logger.error(`[Room][${session.nickname}] 방 제목 변경 실패:`, error);
        sendError(ws, '방 제목 변경에 실패했습니다.');
    }
}

/**
 * 연결 해제 처리
 */
export async function handleDisconnection(ws, reason, err = null) {
    if (ws.authenticated && ws.providerId) {
        const session = getActiveSession(ws.providerId);

        // WebSocket 객체가 동일한 경우만 세션 제거 (중복 로그인 시 레이스 컨디션 방지)
        if (session && session.ws === ws) {
            logger.info(`[USER-OUT][${session.nickname || 'Unknown'}] 연결 해제: ${reason}`);

            // 마지막 로그아웃 시간 업데이트
            if (session.userId) {
                await updateLastLogout(session.userId);
            }

            removeActiveSession(ws.providerId);
        } else {
            logger.info(`[USER-OUT][${session?.nickname || ws.providerId}] 연결 해제 무시: 이미 새 세션으로 대체됨`);
        }
    } else {
        logger.info(`[WS][Guest] 연결 해제: ${reason}`);
    }
}

function sendMessage(ws, type, data) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type, ...data }));
    }
}

function sendError(ws, message) {
    sendMessage(ws, 'error', { message });
}

/**
 * 코스튬 뽑기 처리
 * - constants.js는 합계 1.0/빈 티어를 강제하지 않아도 됩니다.
 * - 서버에서 정규화/빈 티어 제거/경계 보정으로 안전하게 처리합니다.
 * - 기존 UI(룰렛 8칸 중 1칸) 유지.
 */
async function handleShopCostumeBox1(ws, data) {
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, '세션을 찾을 수 없습니다.');
        return;
    }

    if (!session.userId) {
        sendError(ws, '로그인이 필요합니다.');
        return;
    }

    // 골드 확인 (세션의 최신 골드 값 사용)
    if ((session.gold || 0) < ShopBox1.BOX1_COST) {
        sendMessage(ws, 'shop_costume_box1_response', {
            success: false,
            error: '골드가 부족합니다.'
        });
        return;
    }

    try {
        // 정규화/빈티어 필터 결과 확인 (없으면 구매 불가 처리)
        const normalized = getNormalizedNonEmptyTiers(ShopBox1);
        if (normalized.length === 0) {
            sendMessage(ws, 'shop_costume_box1_response', {
                success: false,
                error: '현재 뽑기 가능한 코스튬이 없습니다.'
            });
            return;
        }

        // 1단계: 8번 반복해서 확률적으로 코스튬 선택하여 룰렛 풀 생성
        const roulettePool = [];
        for (let i = 0; i < ShopBox1.ROULETTE_POOL_SIZE; i++) {
            let c = null;
            // 재시도 루프: 극소수 예외(모든 티어 비정상 등)를 방어
            for (let retry = 0; retry < 5; retry++) {
                try {
                    c = drawCostumeOnce(ShopBox1);
                    break;
                } catch (e) {
                    logger.warn('[Shop][System] 코스튬 추첨 재시도:', e.message);
                }
            }
            if (c == null) {
                // 최종 안전장치(운영에서 관측되면 설정 문제)
                logger.error('[Shop][System] 코스튬 추첨 실패: 기본 코스튬으로 대체');
                c = 1;
            }
            roulettePool.push(c);
        }

        // 2단계: 생성된 8개 룰렛 풀에서 랜덤 선택
        const wonCostume = roulettePool[Math.floor(Math.random() * roulettePool.length)];

        // 중복 확인
        const ownedCostumes = await getUserInventory(session.userId);
        const isDuplicate = ownedCostumes.includes(wonCostume);

        // 골드 차감 및 보상 처리
        const newGold = isDuplicate
            ? session.gold - ShopBox1.BOX1_COST + (ShopBox1.BOX1_COST / 2)
            : session.gold - ShopBox1.BOX1_COST;

        if (!isDuplicate) {
            await addCostumeToInventory(session.userId, wonCostume);
        }
        await updateUserGold(session.userId, newGold);

        // 세션 골드 업데이트 (자동으로 update_gold 전송)
        updateSessionGold(providerId, newGold);

        // 업데이트된 인벤토리 조회
        const updatedOwnedCostumes = await getUserInventory(session.userId);

        // 응답 전송
        sendMessage(ws, 'shop_costume_box1_response', {
            success: true,
            roulettePool,
            wonCostume
        });

        // 인벤토리 업데이트 전송
        sendMessage(ws, 'update_inventory', { ownedCostumes: updatedOwnedCostumes });

        logger.info(
            `[Shop][${session.nickname}] 코스튬 뽑기: 코스튬 ${wonCostume} 획득` +
            (isDuplicate ? ` (중복, ${ShopBox1.BOX1_COST / 2}골드 환불)` : '')
        );

    } catch (error) {
        logger.error(`[Shop][${session?.nickname || 'Unknown'}] 코스튬 뽑기 오류:`, error);
        sendMessage(ws, 'shop_costume_box1_response', {
            success: false,
            error: '코스튬 뽑기에 실패했습니다.'
        });
    }
}

/**
 * 코스튬 확률 정보 조회
 * - 서버 기준 정규화된 가중치 사용
 * - 빈 티어는 제외, 각 코스튬 확률은 (정규화된 티어 가중치) / (그 티어의 코스튬 수)
 */
async function handleGetCostumeRates(ws, data) {
    const normalized = getNormalizedNonEmptyTiers(ShopBox1);

    if (normalized.length === 0) {
        sendMessage(ws, 'costume_rates_response', { rates: [] });
        return;
    }

    const rates = [];
    for (const x of normalized) {
        const per = x.weight / x.list.length;
        for (const costumeId of x.list) {
            rates.push({
                costumeId,
                rate: per
            });
        }
    }

    sendMessage(ws, 'costume_rates_response', { rates });
}

async function handleChangeCostume(ws, data) {
    const { costumeId } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, '세션을 찾을 수 없습니다.');
        return;
    }

    if (!session.userId) {
        sendError(ws, '로그인이 필요합니다.');
        return;
    }

    const ownedCostumes = await getUserInventory(session.userId);
    if (!ownedCostumes.includes(costumeId)) {
        sendError(ws, '보유하지 않은 코스튬입니다.');
        return;
    }

    try {
        await updateUserCostume(session.userId, costumeId);
        updateSessionCostume(providerId, costumeId); // 자동으로 update_costume 전송

        logger.info(`[Shop][${session.nickname}] 코스튬 변경: 코스튬 ${costumeId}`);

    } catch (error) {
        logger.error(`[Shop][${session?.nickname || 'Unknown'}] 코스튬 변경 오류:`, error);
        sendError(ws, '코스튬 변경에 실패했습니다.');
    }
}
