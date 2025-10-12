import mysql from 'mysql2/promise';
import { config } from './config.js';
import { logger } from './logger.js';

// DB 연결
let connection;

// DB 연결 테스트 및 데이터베이스 자동 생성
async function testConnection() {
    try {
        // 1. 데이터베이스 없이 MySQL 연결
        const rootConnection = await mysql.createConnection({
            host: config.database.host,
            user: config.database.user,
            password: config.database.password,
        });

        // 2. 데이터베이스 존재 확인 및 생성
        await rootConnection.execute(`
            CREATE DATABASE IF NOT EXISTS ${config.database.name}
            CHARACTER SET utf8mb4
            COLLATE utf8mb4_unicode_ci
        `);
        logger.info(`[DB] 데이터베이스 확인/생성 완료: ${config.database.name}`);

        await rootConnection.end();

        // 3. 실제 데이터베이스에 연결
        connection = await mysql.createConnection({
            host: config.database.host,
            user: config.database.user,
            password: config.database.password,
            database: config.database.name
        });

        await connection.execute('SELECT 1');
        logger.info(`[DB] MySQL 연결 성공`);
    } catch (error) {
        logger.error(`[DB] MySQL 연결 실패: ${error.message}`);
        process.exit(1);
    }
}

// 유저 테이블 생성
async function createTables() {
    const createUserTable = `
        CREATE TABLE IF NOT EXISTS users (
            id INT PRIMARY KEY AUTO_INCREMENT,
            provider VARCHAR(10) NOT NULL,
            provider_id VARCHAR(255) NOT NULL,
            nickname VARCHAR(50),
            gold INT DEFAULT 0,
            current_costume INT DEFAULT 1,
            needs_nickname TINYINT(1) DEFAULT 1,
            win_count INT DEFAULT 0,
            last_login_at TIMESTAMP NULL,
            last_logout_at TIMESTAMP NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

            UNIQUE KEY unique_provider (provider, provider_id),
            INDEX idx_nickname (nickname)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `;

    const createInventoryTable = `
        CREATE TABLE IF NOT EXISTS inventory (
            user_id INT,
            costume_id INT,
            acquired_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (user_id, costume_id),
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `;

    try {
        await connection.execute(createUserTable);
        logger.info('[DB] 유저 테이블 생성/확인 완료');

        await connection.execute(createInventoryTable);
        logger.info('[DB] 인벤토리 테이블 생성/확인 완료');
    } catch (error) {
        logger.error(`[DB] 테이블 생성 실패: ${error.message}`);
    }
}

/**
 * 사용자 조회
 */
export async function findUserByProvider(providerId) {
    try {
        const [rows] = await connection.execute(
            'SELECT * FROM users WHERE provider_id = ?',
            [providerId]
        );
        return rows[0] || null;
    } catch (error) {
        logger.error('[DB] 사용자 조회 실패:', error);
        return null;
    }
}

/**
 * 사용자 생성 (기본 코스튬 자동 지급)
 */
export async function createUser(provider, providerId, nickname) {
    try {
        // 트랜잭션 시작
        await connection.beginTransaction();

        // 사용자 생성
        const [result] = await connection.execute(
            'INSERT INTO users (provider, provider_id, nickname) VALUES (?, ?, ?)',
            [provider, providerId, nickname]
        );
        const userId = result.insertId;

        // 기본 코스튬 1번 지급
        await connection.execute(
            'INSERT INTO inventory (user_id, costume_id) VALUES (?, ?)',
            [userId, 1]
        );
        await Promise.all([addCostumeToInventory(userId, 1), addCostumeToInventory(userId, 2), addCostumeToInventory(userId, 3)]);

        // 트랜잭션 커밋
        await connection.commit();
        return userId;
    } catch (error) {
        // 트랜잭션 롤백
        await connection.rollback();
        logger.error('[DB] 사용자 생성 실패:', error);
        return null;
    }
}

/**
 * 닉네임 중복 체크
 */
export async function checkNicknameDuplicate(nickname) {
    try {
        const [rows] = await connection.execute(
            'SELECT id FROM users WHERE nickname = ?',
            [nickname]
        );
        return rows.length > 0;
    } catch (error) {
        logger.error('[DB] 닉네임 중복 체크 실패:', error);
        return true; // 에러시 중복으로 처리
    }
}

/**
 * 닉네임 설정
 */
export async function setUserNickname(provider, providerId, nickname) {
    try {
        await connection.execute(
            'UPDATE users SET nickname = ?, needs_nickname = 0 WHERE provider = ? AND provider_id = ?',
            [nickname, provider, providerId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 닉네임 설정 실패:', error);
        return false;
    }
}

/**
 * 임시 닉네임 생성
 */
export function generateTempNickname() {
    const timestamp = Date.now().toString(36);
    return `User${timestamp}`;
}

/**
 * 골드 업데이트
 */
export async function updateUserGold(userId, gold) {
    try {
        await connection.execute(
            'UPDATE users SET gold = ? WHERE id = ?',
            [gold, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 골드 업데이트 실패:', error);
        return false;
    }
}

/**
 * 코스튬 변경
 */
export async function updateUserCostume(userId, costumeId) {
    try {
        await connection.execute(
            'UPDATE users SET current_costume = ? WHERE id = ?',
            [costumeId, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 코스튬 변경 실패:', error);
        return false;
    }
}

/**
 * 인벤토리에 코스튬 추가
 */
export async function addCostumeToInventory(userId, costumeId) {
    try {
        await connection.execute(
            'INSERT IGNORE INTO inventory (user_id, costume_id) VALUES (?, ?)',
            [userId, costumeId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 코스튬 추가 실패:', error);
        return false;
    }
}

/**
 * 사용자 인벤토리 조회
 */
export async function getUserInventory(userId) {
    try {
        const [rows] = await connection.execute(
            'SELECT costume_id FROM inventory WHERE user_id = ? ORDER BY costume_id',
            [userId]
        );
        return rows.map(row => row.costume_id);
    } catch (error) {
        logger.error('[DB] 인벤토리 조회 실패:', error);
        return [];
    }
}

/**
 * 마지막 로그인 시간 업데이트
 */
export async function updateLastLogin(userId) {
    try {
        await connection.execute(
            'UPDATE users SET last_login_at = NOW() WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 마지막 로그인 업데이트 실패:', error);
        return false;
    }
}

/**
 * 승리 횟수 증가
 */
export async function incrementWinCount(userId) {
    try {
        await connection.execute(
            'UPDATE users SET win_count = win_count + 1 WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 승리 횟수 증가 실패:', error);
        return false;
    }
}

/**
 * 마지막 로그아웃 시간 업데이트
 */
export async function updateLastLogout(userId) {
    try {
        await connection.execute(
            'UPDATE users SET last_logout_at = NOW() WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 마지막 로그아웃 업데이트 실패:', error);
        return false;
    }
}

/**
 * DB 초기화
 */
export async function initDatabase() {
    await testConnection();
    await createTables();
}