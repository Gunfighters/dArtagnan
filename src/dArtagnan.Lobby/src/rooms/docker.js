import Docker from 'dockerode';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { config } from '../config.js';
import { logger } from '../logger.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Docker 연결
const docker = process.platform === 'win32'
    ? new Docker()
    : new Docker({ socketPath: '/var/run/docker.sock' });

// 게임 로그 디렉토리 생성
const GAME_LOG_DIR = path.join(__dirname, '..', '..', '..', '..', 'logs', 'game');
if (!fs.existsSync(GAME_LOG_DIR)) {
    fs.mkdirSync(GAME_LOG_DIR, { recursive: true });
}

/**
 * 게임서버 컨테이너 생성
 */
export async function createGameServerContainer(roomId, roomName) {
    try {
        logger.info(`[Docker][Room-${roomId.substring(0, 6)}] 컨테이너 생성 시작`);

        const lobbyUrl = config.docker.getLobbyUrl();

        const container = await docker.createContainer({
            Image: config.docker.image,
            Env: [
                `PORT=${config.docker.internalPort}`,
                `ROOM_ID=${roomId}`,
                `ROOM_NAME=${roomName}`,
                `LOBBY_URL=${lobbyUrl}`
            ],
            ExposedPorts: { [`${config.docker.internalPort}/tcp`]: {} },
            HostConfig: {
                PortBindings: { [`${config.docker.internalPort}/tcp`]: [{ HostPort: '0' }] },
                AutoRemove: true,
                Binds: [`${GAME_LOG_DIR}:/app/logs`]
            }
        });

        logger.info(`[Docker][Room-${roomId.substring(0, 6)}] 컨테이너 생성 완료: containerId=${container.id.substring(0, 12)}`);

        // 컨테이너 시작
        await container.start();
        logger.info(`[Docker][Room-${roomId.substring(0, 6)}] 컨테이너 시작 완료`);

        // 포트 바인딩 조회
        const info = await container.inspect();
        const bindings = info.NetworkSettings.Ports[`${config.docker.internalPort}/tcp`];
        const hostPort = bindings?.[0]?.HostPort;

        if (!hostPort) {
            logger.error(`[Docker][Room-${roomId.substring(0, 6)}] 포트 바인딩 조회 실패`);
            throw new Error('Failed to get host port binding');
        }

        logger.info(`[Docker][Room-${roomId.substring(0, 6)}] 포트 바인딩 완료: ${config.publicDomain}:${hostPort}`);

        return {
            containerId: container.id,
            ip: config.publicDomain,
            port: Number(hostPort),
            container: container
        };

    } catch (error) {
        logger.error(`[Docker][Room-${roomId.substring(0, 6)}] 컨테이너 생성 실패:`, error);
        throw error;
    }
}

/**
 * 컨테이너 정리 시 콜백 등록
 */
export function onContainerStop(container, callback) {
    container.wait().then(() => {
        logger.info(`[Docker][Container-${container.id.substring(0, 6)}] 컨테이너 정지 감지`);
        callback();
    }).catch(() => {});
}