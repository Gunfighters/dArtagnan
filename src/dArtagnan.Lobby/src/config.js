/**
 * 로비 서버 설정 - 모든 설정값 중앙화
 */

export const config = {
    // 서버 설정
    port: 3002,
    publicDomain: process.env.PUBLIC_DOMAIN || '127.0.0.1',

    // 세션 관리
    session: {
        oauthTimeout: 5 * 60 * 1000,    // 5분
        cleanupInterval: 60 * 1000       // 1분마다 체크
    },

    // Docker 설정
    docker: {
        image: 'dartagnan-gameserver:v2',
        internalPort: 7777,
        // 플랫폼별 로비 URL
        getLobbyUrl() {
            const platform = process.platform;
            if (platform === 'win32' || platform === 'darwin') {
                return `http://host.docker.internal:${config.port}`;
            }
            return `http://172.17.0.1:${config.port}`;
        }
    },

    // [REMOVED FOR PORTFOLIO]

    // [REMOVED FOR PORTFOLIO]

    // 방 상태 상수
    roomState: {
        INITIALIZING: -1,
        WAITING: 0,
        ROUND: 1
    }
};