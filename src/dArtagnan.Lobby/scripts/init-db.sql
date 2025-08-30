-- dArtagnan 데이터베이스 초기화 스크립트
-- 실행 방법: mysql -u root -p < init-db.sql

-- 1. 기존 데이터베이스 삭제 (주의: 데이터 손실!)
-- DROP DATABASE IF EXISTS dartagnan;

-- 2. 데이터베이스 생성 (UTF-8 완전 지원)
CREATE DATABASE IF NOT EXISTS dartagnan 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

-- 3. 사용자 권한 설정 (필요시)
-- GRANT ALL PRIVILEGES ON dartagnan.* TO 'root'@'localhost';
-- FLUSH PRIVILEGES;

-- 4. 데이터베이스 선택
USE dartagnan;

-- 5. 사용자 테이블 생성 (Node.js에서 자동 생성되지만 백업용)
CREATE TABLE IF NOT EXISTS users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    provider VARCHAR(10) NOT NULL,
    provider_id VARCHAR(255) NOT NULL,
    nickname VARCHAR(50) UNIQUE,
    is_guest BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE KEY unique_provider (provider, provider_id),
    INDEX idx_nickname (nickname)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. 초기 데이터 (필요시)
-- INSERT INTO users (provider, provider_id, nickname) VALUES 
-- ('system', '0', 'admin');

-- 완료 메시지
SELECT '✅ dArtagnan 데이터베이스 초기화 완료!' as message;