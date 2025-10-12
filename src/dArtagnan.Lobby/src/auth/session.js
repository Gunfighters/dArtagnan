import { config } from '../config.js';
import { logger } from '../logger.js';

// OAuth 임시 세션 (HTTP → WebSocket 연결용)
const oauthSessions = new Map(); // providerId -> { sessionId, user, createdAt }

// 활성 WebSocket 세션 (중복 로그인 방지용)
const activeSessions = new Map(); // providerId -> { ws, userId, nickname, gold, currentCostume, currentRoomId, ... }

/**
 * OAuth 세션 생성
 */
export function createOAuthSession(providerId, user) {
    const sessionId = generateSessionId();
    oauthSessions.set(providerId, {
        sessionId,
        user,
        createdAt: Date.now()
    });
    return sessionId;
}

/**
 * OAuth 세션 검증
 */
export function verifyOAuthSession(sessionId) {
    for (const [providerId, session] of oauthSessions) {
        if (session.sessionId === sessionId) {
            // 세션 만료 체크
            if (Date.now() - session.createdAt > config.session.oauthTimeout) {
                oauthSessions.delete(providerId);
                return null;
            }
            return { providerId, user: session.user };
        }
    }
    return null;
}

/**
 * OAuth 세션 삭제
 */
export function removeOAuthSession(providerId) {
    oauthSessions.delete(providerId);
}

/**
 * 활성 세션 조회
 */
export function getActiveSession(providerId) {
    return activeSessions.get(providerId);
}

/**
 * 모든 활성 세션 조회 (접속자 목록)
 */
export function getAllActiveSessions() {
    const sessions = [];
    for (const [providerId, session] of activeSessions) {
        sessions.push({
            providerId,
            nickname: session.nickname,
            gold: session.gold,
            currentCostume: session.currentCostume,
            currentRoomId: session.currentRoomId,
            provider: session.provider,
            loginAt: session.loginAt
        });
    }
    return sessions;
}

/**
 * 활성 세션 설정
 */
export function setActiveSession(providerId, ws, user, currentRoomId = null) {
    activeSessions.set(providerId, {
        ws,
        userId: user.id,
        nickname: user.nickname,
        gold: user.gold,
        currentCostume: user.current_costume,
        providerId: user.providerId,
        provider: user.provider,
        isNewUser: user.isNewUser || false,
        needSetNickname: user.needSetNickname || false,
        currentRoomId,
        loginAt: new Date()
    });
    logger.info(`[USER-IN][${user.nickname}] 활성 세션 등록: 골드=${user.gold}`);
}

/**
 * 세션 골드 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionGold(providerId, newGold) {
    const session = activeSessions.get(providerId);
    if (session) {
        const oldGold = session.gold;
        session.gold = newGold;
        logger.info(`[Auth][${session.nickname}] 골드 업데이트: ${oldGold} → ${newGold}`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_gold',
                gold: newGold
            }));
        }
    }
}

/**
 * 세션 닉네임 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionNickname(providerId, newNickname) {
    const session = activeSessions.get(providerId);
    if (session) {
        const oldNickname = session.nickname;
        session.nickname = newNickname;
        session.needSetNickname = false;
        logger.info(`[Auth][${newNickname}] 닉네임 업데이트: "${oldNickname}" → "${newNickname}"`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_nickname',
                nickname: newNickname
            }));
        }
    }
}

/**
 * 세션 코스튬 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionCostume(providerId, costumeId) {
    const session = activeSessions.get(providerId);
    if (session) {
        session.currentCostume = costumeId;
        logger.info(`[Auth][${session.nickname}] 코스튬 업데이트: 코스튬 ${costumeId}`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_costume',
                currentCostume: costumeId
            }));
        }
    }
}

/**
 * 세션 사용자 ID 업데이트 (신규 사용자 생성 시)
 * - 클라이언트에 전송할 필요 없음 (내부 데이터)
 */
export function updateSessionUserId(providerId, userId) {
    const session = activeSessions.get(providerId);
    if (session) {
        session.userId = userId;
        logger.info(`[Auth][${session.nickname}] 사용자 ID 업데이트: userId=${userId}`);
    }
}

/**
 * 세션에서 user 객체 형태로 조회 (호환성용)
 */
export function getSessionUser(providerId) {
    const session = activeSessions.get(providerId);
    if (!session) return null;

    return {
        id: session.userId,
        nickname: session.nickname,
        gold: session.gold,
        current_costume: session.currentCostume,
        providerId: session.providerId,
        provider: session.provider,
        isNewUser: session.isNewUser,
        needSetNickname: session.needSetNickname
    };
}

/**
 * 사용자의 현재 방 설정
 */
export function setUserCurrentRoom(providerId, roomId) {
    const session = activeSessions.get(providerId);
    if (session) {
        session.currentRoomId = roomId;
    }
}

/**
 * 사용자의 현재 방 조회
 */
export function getUserCurrentRoom(providerId) {
    const session = activeSessions.get(providerId);
    return session?.currentRoomId || null;
}

/**
 * 활성 세션 제거
 */
export function removeActiveSession(providerId) {
    const session = activeSessions.get(providerId);
    if (session) {
        logger.info(`[USER-OUT][${session.nickname}] 활성 세션 제거`);
    }
    activeSessions.delete(providerId);
}

/**
 * 모든 클라이언트에게 메시지 브로드캐스트
 */
export function broadcastToAll(message) {
    const messageStr = JSON.stringify(message);
    let sentCount = 0;

    for (const [providerId, { ws }] of activeSessions) {
        if (ws.readyState === ws.OPEN) {
            ws.send(messageStr);
            sentCount++;
        }
    }

    logger.info(`[WS][All] 브로드캐스트: ${message.type} (${sentCount}명)`);
}

/**
 * 세션 ID 생성
 */
function generateSessionId() {
    return Math.random().toString(36).slice(2) + Date.now().toString(36);
}

/**
 * 주기적 세션 정리
 */
function cleanupSessions() {
    const now = Date.now();
    let oauthCleaned = 0;

    // OAuth 세션 정리
    for (const [providerId, session] of oauthSessions) {
        if (now - session.createdAt > config.session.oauthTimeout) {
            oauthSessions.delete(providerId);
            oauthCleaned++;
        }
    }

    if (oauthCleaned > 0) {
        logger.info(`[Auth][System] 세션 정리: OAuth 세션 ${oauthCleaned}개 정리 완료`);
    }
}

// 세션 정기 정리 시작
setInterval(cleanupSessions, config.session.cleanupInterval);
logger.info(`[Auth][System] 자동 세션 정리 시작: OAuth TTL=${config.session.oauthTimeout / 60000}분`);