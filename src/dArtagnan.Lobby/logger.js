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