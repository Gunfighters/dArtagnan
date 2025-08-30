import { OAuth2Client } from 'google-auth-library';
import { findUserByProvider, createUser, generateTempNickname, checkNicknameDuplicate } from './db.js';

// Google OAuth 클라이언트 설정
const googleClient = new OAuth2Client(
    process.env.GOOGLE_CLIENT_ID,
    process.env.GOOGLE_CLIENT_SECRET
);

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
 * 개발용 로그인 처리 (OAuth와 동일한 구조 사용)
 */
export async function processDevLogin(providerId, oauthSessions) {
    try {
        if (!providerId || providerId.trim().length === 0) {
            throw new Error('ProviderId is required.');
        }

        const trimmedProviderId = providerId.trim();
        
        if (trimmedProviderId.length < 1 || trimmedProviderId.length > 16) {
            throw new Error('ProviderId must be 1-16 characters.');
        }

        logger.info(`[Dev Login] Login request: ${trimmedProviderId}`);

        // 개발용 provider 정보
        const provider = 'dev';
        const cleanProviderId = trimmedProviderId;
        
        // 기존 사용자 찾기 (OAuth와 동일한 패턴)
        let user = await findUserByProvider(provider, cleanProviderId);
        
        if (!user) {
            // 신규 사용자 생성 (providerId를 닉네임으로도 사용)
            const userId = await createUser(provider, cleanProviderId, cleanProviderId, true);
            user = { id: userId, nickname: cleanProviderId };
            logger.info(`[Dev Login] New guest user created: ${cleanProviderId}`);
        } else {
            logger.info(`[Dev Login] Existing guest user login: ${cleanProviderId}`);
        }

        // OAuth 세션 생성 (HTTP → WebSocket 연결용)
        const sessionId = Math.random().toString(36).slice(2);
        oauthSessions.set(sessionId, {
            user: {
                id: user.id,
                nickname: user.nickname,
                needSetNickname: false, // 개발용은 providerId를 직접 입력하므로 false
                provider: provider,
                providerId: cleanProviderId,
                displayName: user.nickname,
                is_guest: true
            },
            createdAt: Date.now()
        });

        logger.info(`[Dev Login] Login complete: ${user.nickname} (${sessionId})`);
        
        return {
            success: true,
            sessionId,
            nickname: user.nickname,
            needSetNickname: false
        };

    } catch (error) {
        logger.error(`[Dev Login] Failed:`, error);
        throw error;
    }
}

/**
 * Unity에서 받은 Authorization Code로 OAuth 로그인 처리
 */
export async function processUnityOAuth(authCode, oauthSessions) {
    try {
        if (!authCode) {
            throw new Error('Authorization Code is required.');
        }

        logger.info(`[Unity OAuth] Received Auth Code: ${authCode.substring(0, 10)}...`);

        // 1. Authorization Code를 Access Token으로 교환
        const tokens = await exchangeAuthCodeForToken(authCode);
        
        // 2. Access Token으로 Google Play Games 플레이어 정보 가져오기
        const playerInfo = await getPlayerInfo(tokens.access_token);
        
        // 3. DB에서 사용자 찾기/생성
        const user = await findOrCreateUser(playerInfo);
        
        // 4. OAuth 세션 생성 (HTTP → WebSocket 연결용)
        const sessionId = Math.random().toString(36).slice(2);
        oauthSessions.set(sessionId, {
            user: {
                id: user.id,
                nickname: user.nickname,
                needSetNickname: user.needSetNickname,
                provider: 'google',
                providerId: playerInfo.playerId,
                displayName: playerInfo.displayName,
                is_guest: false
            },
            createdAt: Date.now()
        });

        logger.info(`[Unity OAuth] Login processing complete: ${user.nickname} (${sessionId})`);
        
        return {
            success: true,
            sessionId,
            nickname: user.nickname,
            needSetNickname: user.needSetNickname
        };

    } catch (error) {
        logger.error(`[Unity OAuth] Token verification failed:`, error);
        throw error;
    }
}

/**
 * Authorization Code를 Access Token으로 교환
 */
async function exchangeAuthCodeForToken(authCode) {
    logger.info(`[Unity OAuth] Starting token exchange with Google`);
    
    const tokenRequestBody = {
        code: authCode,
        client_id: process.env.GOOGLE_CLIENT_ID,
        client_secret: process.env.GOOGLE_CLIENT_SECRET,
        grant_type: 'authorization_code'
    };

    const tokenResponse = await fetch('https://oauth2.googleapis.com/token', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: new URLSearchParams(tokenRequestBody)
    });

    logger.info(`[Unity OAuth] Token response status: ${tokenResponse.status}`);
    
    const tokens = await tokenResponse.json();
    
    if (!tokenResponse.ok) {
        throw new Error(`Google token exchange failed: ${JSON.stringify(tokens)}`);
    }
    
    if (!tokens.access_token) {
        throw new Error('No access token received from Google');
    }

    logger.info(`[Unity OAuth] Token exchange successful - Access token received`);
    return tokens;
}

/**
 * Access Token으로 Google Play Games 플레이어 정보 가져오기
 */
async function getPlayerInfo(accessToken) {
    logger.info(`[Unity OAuth] Fetching player info with Google Play Games API`);
    
    const playerInfoResponse = await fetch('https://www.googleapis.com/games/v1/players/me', {
        headers: {
            'Authorization': `Bearer ${accessToken}`
        }
    });
    
    logger.info(`[Unity OAuth] Player info response status: ${playerInfoResponse.status}`);
    
    const playerInfoText = await playerInfoResponse.text();
    
    if (!playerInfoResponse.ok) {
        throw new Error(`Failed to fetch player info: ${playerInfoResponse.status} - ${playerInfoText}`);
    }
    
    const playerInfo = JSON.parse(playerInfoText);
    
    if (!playerInfo.playerId) {
        throw new Error(`No player ID in Play Games response: ${playerInfoText}`);
    }
    
    logger.info(`[Unity OAuth] Google Play Games player info retrieved - ID: ${playerInfo.playerId}, DisplayName: ${playerInfo.displayName || 'N/A'}`);
    
    return playerInfo;
}

/**
 * DB에서 사용자 찾기/생성
 */
async function findOrCreateUser(playerInfo) {
    const { playerId, displayName } = playerInfo;
    
    let user = await findUserByProvider('google', playerId);
    let needSetNickName = false;

    if (!user) {
        // 신규 사용자 → 즉시 임시 닉네임으로 회원가입
        const tempNickname = generateTempNickname();
        const userId = await createUser('google', playerId, tempNickname, false);
        
        user = { id: userId, nickname: tempNickname };
        needSetNickName = true;
        logger.info(`[Unity OAuth] New user auto registration: ${displayName} → ${tempNickname}`);
    } else {
        // 기존 사용자 - 임시 닉네임인지 확인
        needSetNickName = user.nickname.startsWith('User') && /^User[a-z0-9]+$/.test(user.nickname);
        logger.info(`[Unity OAuth] Existing user login: ${displayName} → ${user.nickname}`);
    }

    return { ...user, needSetNickname: needSetNickName };
}