import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// 로그 디렉토리 생성
const LOG_DIR = path.join(__dirname, '..', '..', '..', 'logs', 'lobby');
if (!fs.existsSync(LOG_DIR)) {
    fs.mkdirSync(LOG_DIR, { recursive: true });
}

/**
 * 현재 날짜로 로그 파일 경로 생성
 */
function getLogFilePath() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const filename = `${year}-${month}-${day}.log`;
    return path.join(LOG_DIR, filename);
}

/**
 * 통합 로깅 유틸리티
 */
export const logger = {
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

        let logLine;
        if (level === 'INFO') {
            logLine = `${timestamp} ${message}`;
            stream(logLine);
        } else if (level === 'WARN') {
            logLine = `${timestamp} ⚠️ ${message}`;
            stream(logLine);
        } else if (level === 'ERROR') {
            logLine = `${timestamp} ❌ ${message}`;
            stream(logLine);
        }

        // 파일에 저장
        try {
            const logFilePath = getLogFilePath();
            fs.appendFileSync(logFilePath, logLine + '\n', 'utf8');
        } catch (err) {
            console.error('Failed to write log to file:', err);
        }
    },
    info(...args) { this._log('INFO', ...args); },
    warn(...args) { this._log('WARN', ...args); },
    error(...args) { this._log('ERROR', ...args); }
};