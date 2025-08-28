import mysql from 'mysql2/promise';

// DB 연결 설정
let connection;

// DB 연결 테스트
async function testConnection() {
    try {
        connection = await mysql.createConnection({
            host: process.env.DB_HOST || 'localhost',
            user: process.env.DB_USER || 'root', 
            password: process.env.DB_PASSWORD || '',
            database: process.env.DB_NAME || 'dartagnan'
        });
        
        await connection.execute('SELECT 1');
        console.log('✅ MySQL 연결 성공');
    } catch (error) {
        console.error('❌ MySQL 연결 실패:', error.message);
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
            nickname VARCHAR(50) UNIQUE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            
            UNIQUE KEY unique_provider (provider, provider_id),
            INDEX idx_nickname (nickname)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `;
    
    try {
        await connection.execute(createUserTable);
        console.log('✅ 유저 테이블 생성/확인 완료');
    } catch (error) {
        console.error('❌ 테이블 생성 실패:', error.message);
    }
}

// 사용자 DB 함수들
export async function findUserByProvider(provider, providerId) {
    try {
        const [rows] = await connection.execute(
            'SELECT * FROM users WHERE provider = ? AND provider_id = ?',
            [provider, providerId]
        );
        return rows[0] || null;
    } catch (error) {
        console.error('사용자 조회 실패:', error);
        return null;
    }
}

export async function createUser(provider, providerId, nickname) {
    try {
        const [result] = await connection.execute(
            'INSERT INTO users (provider, provider_id, nickname) VALUES (?, ?, ?)',
            [provider, providerId, nickname]
        );
        return result.insertId;
    } catch (error) {
        console.error('사용자 생성 실패:', error);
        return null;
    }
}

export async function checkNicknameDuplicate(nickname) {
    try {
        const [rows] = await connection.execute(
            'SELECT id FROM users WHERE nickname = ?',
            [nickname]
        );
        return rows.length > 0; // true면 중복
    } catch (error) {
        console.error('닉네임 중복 체크 실패:', error);
        return true; // 에러시 중복으로 처리
    }
}

export async function setUserNickname(provider, providerId, nickname) {
    try {
        await connection.execute(
            'UPDATE users SET nickname = ? WHERE provider = ? AND provider_id = ?',
            [nickname, provider, providerId]
        );
        return true;
    } catch (error) {
        console.error('닉네임 설정 실패:', error);
        return false;
    }
}

// 임시 닉네임 생성
export function generateTempNickname() {
    const timestamp = Date.now().toString(36);
    return `User${timestamp}`;
}

// DB 초기화
export async function initDB() {
    await testConnection();
    await createTables();
}

export default connection;