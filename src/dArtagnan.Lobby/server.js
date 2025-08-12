import express from 'express';
import Docker from 'dockerode';
import net from 'node:net';
import http from 'http';
import { WebSocketServer } from 'ws';
import crypto from 'crypto';

// --- 로깅 유틸리티 (최종 수정) ---
const logger = {
  _log(level, ...args) {
    const now = new Date();
    const h = String(now.getHours()).padStart(2, '0');
    const m = String(now.getMinutes()).padStart(2, '0');
    const s = String(now.getSeconds()).padStart(2, '0');
    const ms = String(now.getMilliseconds()).padStart(3, '0');
    const timestamp = `[${h}:${m}:${s}.${ms}]`;

    // 모든 인자를 하나의 문자열로 변환하고 합칩니다.
    const message = args.map(arg => {
      if (typeof arg === 'object' && arg !== null) {
        return JSON.stringify(arg, null, 2); // 객체는 보기 좋게 변환
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
// --------------------

const app = express();
const httpServer = http.createServer(app);
const wss = new WebSocketServer({ server: httpServer });
app.use(express.json());

// WebSocket 연결 관리
const connections = new Map(); // ws -> { sessionId, nickname }

// Docker 연결 (플랫폼 자동 감지)
const docker = process.platform === 'win32'
    ? new Docker()
    : new Docker({ socketPath: '/var/run/docker.sock' });

// 상태 저장소
const rooms = new Map(); // roomId -> { containerId, ip, port, state }
const users = new Map(); // sessionId -> { nickname }
const pendingRequests = new Map(); // roomId -> [{ ws, type, responseData }]

// 설정
const IMAGE = 'dartagnan-gameserver:latest';
const INTERNAL_PORT = 7777;

// 게임 서버 접속 주소 (도메인 사용 시 도메인, 아니면 localhost)
const HOST_IP = process.env.GAME_SERVER_HOST || '127.0.0.1';
logger.info(`게임 서버 호스트 주소: ${HOST_IP}`);

async function createRoom(roomId) {
  logger.info(`[방 ${roomId}] 생성 요청 접수`);

  if (rooms.has(roomId)) {
    logger.info(`[방 ${roomId}] 기존에 생성된 방이 존재하여 반환합니다.`);
    return rooms.get(roomId);
  }

  let lobbyUrl;
  if (process.env.LOBBY_HOST_URL) {
    lobbyUrl = process.env.LOBBY_HOST_URL;
  } else if (process.platform === 'win32') {
    lobbyUrl = `http://host.docker.internal:${process.env.PORT || 3000}`;
  } else {
    lobbyUrl = `http://172.17.0.1:${process.env.PORT || 3000}`;
  }
  logger.info(`[방 ${roomId}] 컨테이너 생성을 시작합니다. (로비 URL: ${lobbyUrl})`);

  const container = await docker.createContainer({
    Image: IMAGE,
    Env: [
      `PORT=${INTERNAL_PORT}`,
      `ROOM_ID=${roomId}`,
      `LOBBY_URL=${lobbyUrl}`
    ],
    ExposedPorts: { [`${INTERNAL_PORT}/tcp`]: {} },
    HostConfig: {
      PortBindings: { [`${INTERNAL_PORT}/tcp`]: [{ HostPort: '0' }] },
      AutoRemove: true
    }
  });
  logger.info(`[방 ${roomId}] 컨테이너 생성 완료: ${container.id.substring(0, 12)}`);

  await container.start();
  logger.info(`[방 ${roomId}] 컨테이너 시작 완료.`);

  const info = await container.inspect();
  const bindings = info.NetworkSettings.Ports[`${INTERNAL_PORT}/tcp`];
  const hostPort = bindings?.[0]?.HostPort;
  if (!hostPort) {
    logger.error(`[방 ${roomId}] 호스트 포트 바인딩 정보를 가져오는 데 실패했습니다.`);
    throw new Error('Failed to get host port binding');
  }
  logger.info(`[방 ${roomId}] 포트 바인딩 확인: ${HOST_IP}:${hostPort}`);

  const room = { containerId: container.id, ip: HOST_IP, port: Number(hostPort), state: -1 };
  rooms.set(roomId, room);
  logger.info(`[방 ${roomId}] 방 정보 사전 저장 완료 (상태: 대기중)`);

  logger.info(`[방 ${roomId}] 포트(${hostPort}) 활성화 대기를 시작합니다.`);
  await waitForPort(HOST_IP, Number(hostPort));
  await new Promise(resolve => setTimeout(resolve, 200));
  logger.info(`[방 ${roomId}] 포트(${hostPort})가 활성화되었습니다.`);

  container.wait().then(() => {
    logger.info(`[방 ${roomId}] 컨테이너가 정지되어 관련 리소스를 정리합니다.`);
    rooms.delete(roomId);
    pendingRequests.delete(roomId);
  }).catch(() => {});

  return room;
}

function waitForPort(host, port, timeoutMs = 5000, retryMs = 150) {
  const started = Date.now();
  return new Promise((resolve, reject) => {
    (function attempt() {
      const socket = net.createConnection({ host, port }, () => {
        socket.end();
        resolve();
      });
      socket.once('error', () => {
        socket.destroy();
        if (Date.now() - started >= timeoutMs) return reject(new Error('Timeout'));
        setTimeout(attempt, retryMs);
      });
    })();
  });
}

function pickRandomWaitingRoom() {
  const waiting = Array.from(rooms.entries()).filter(([, r]) => r.state === 0);
  return waiting.length > 0 ? waiting[Math.floor(Math.random() * waiting.length)][0] : null;
}

function generateRoomId() {
  return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}

// 로그인
app.post('/login', (req, res) => {
  logger.info(`[로그인] 요청 수신:`, req.body);
  const nickname = (req.body?.nickname || '').trim();
  if (!nickname) {
    return res.status(400).json({ code: 'null_nickname', message: '닉네임을 입력해주세요.' });
  }
  if (nickname.length < 1 || nickname.length > 16) {
    return res.status(400).json({ code: 'invalid_nickname', message: '닉네임은 1자 이상 16자 이하로 입력해주세요.' });
  }
  for (const user of users.values()) {
    if (user.nickname === nickname) {
      logger.warn(`[로그인] 닉네임 중복 시도: ${nickname}`);
      return res.status(409).json({ code: 'duplicate_nickname', message: '이미 사용 중인 닉네임입니다.' });
    }
  }
  const sessionId = Math.random().toString(36).slice(2);
  users.set(sessionId, { nickname });
  logger.info(`[로그인] 성공: ${nickname} (세션ID: ${sessionId})`);
  res.json({ sessionId, nickname });
});

// 게임서버 상태 업데이트
app.post('/internal/rooms/:roomId/state', (req, res) => {
  const { roomId } = req.params;
  const { state: newState } = req.body;
  logger.info(`[상태 업데이트] [방 ${roomId}] 상태 변경 요청: ${newState}`);

  const room = rooms.get(roomId);
  if (!room) {
    const currentRooms = Array.from(rooms.keys());
    logger.error(`[상태 업데이트] [방 ${roomId}] 존재하지 않는 방에 대한 요청입니다. (현재 방: ${currentRooms.join(', ') || '없음'})`);
    return res.status(404).json({ code: 'room_not_found', message: '방을 찾을 수 없습니다.' });
  }

  logger.info(`[방 ${roomId}] 상태 변경: ${room.state} -> ${newState}`);
  room.state = newState;

  if (newState === 0 && pendingRequests.has(roomId)) {
    const requests = pendingRequests.get(roomId);
    logger.info(`[방 ${roomId}] 준비 완료. 대기 중인 요청 ${requests.length}건에 대해 응답을 전송합니다.`);

    requests.forEach(({ ws, type, responseData }) => {
      logger.info(`[방 ${roomId}] 대기열 응답 전송: ${type}`);
      sendMessage(ws, type, responseData);
    });
    pendingRequests.delete(roomId);
  }

  res.json({ ok: true });
});

// WebSocket 메시지 처리
function sendMessage(ws, type, data) {
  if (ws.readyState === ws.OPEN) {
    ws.send(JSON.stringify({ type, ...data }));
  }
}

function sendError(ws, code) {
  sendMessage(ws, 'error', { code });
}

function addPendingRequest(roomId, ws, type, responseData) {
  if (!pendingRequests.has(roomId)) {
    pendingRequests.set(roomId, []);
  }
  pendingRequests.get(roomId).push({ ws, type, responseData });
  logger.info(`[방 ${roomId}] 요청이 대기열에 추가되었습니다. (현재 ${pendingRequests.get(roomId).length}개 대기)`);
}

function cleanupPendingRequests(ws) {
  let cleanedCount = 0;
  for (const [roomId, requests] of pendingRequests.entries()) {
    const originalLength = requests.length;
    const filteredRequests = requests.filter(req => req.ws !== ws);

    if (filteredRequests.length !== originalLength) {
      cleanedCount += originalLength - filteredRequests.length;
      if (filteredRequests.length === 0) {
        pendingRequests.delete(roomId);
      } else {
        pendingRequests.set(roomId, filteredRequests);
      }
    }
  }
  if (cleanedCount > 0) {
    logger.info(`[연결 해제] 사용자의 대기중인 요청 ${cleanedCount}개를 정리했습니다.`);
  }
}

wss.on('connection', (ws, req) => {
  const connectionId = crypto.randomBytes(4).toString('hex');
  logger.info(`[WebSocket] 새로운 연결 수립: ${connectionId}`);

  let authenticated = false;
  let sessionId = null;

  ws.on('message', async (message) => {
    try {
      const data = JSON.parse(message.toString());
      logger.info(`[WebSocket ${connectionId}] 메시지 수신:`, data);

      if (data.type === 'auth') {
        sessionId = data.sessionId;
        if (!sessionId || !users.has(sessionId)) {
          logger.error(`[WebSocket ${connectionId}] 인증 실패: 유효하지 않은 세션ID (${sessionId})`);
          sendError(ws, 'unauthorized');
          ws.close();
          return;
        }
        authenticated = true;
        connections.set(ws, { sessionId, nickname: users.get(sessionId).nickname });
        sendMessage(ws, 'auth_success', { ok: true });
        logger.info(`[WebSocket ${connectionId}] 인증 성공: ${users.get(sessionId).nickname} (${sessionId})`);
        return;
      }

      if (!authenticated) {
        logger.warn(`[WebSocket ${connectionId}] 인증되지 않은 요청 시도: ${data.type}`);
        sendError(ws, 'not_authenticated');
        return;
      }

      const { nickname } = connections.get(ws);

      if (data.type === 'create_room') {
        logger.info(`[${nickname}] 방 생성 요청`);
        try {
          const roomId = data.roomId || generateRoomId();
          const room = await createRoom(roomId);
          const responseData = { ok: true, roomId, ip: room.ip, port: room.port };

          if (room.state === 0) {
            logger.info(`[방 ${roomId}] 즉시 응답 가능. 응답을 전송합니다.`);
            sendMessage(ws, 'create_room_response', responseData);
          } else {
            logger.info(`[방 ${roomId}] 게임 서버 준비 대기. 요청을 큐에 추가합니다.`);
            addPendingRequest(roomId, ws, 'create_room_response', responseData);
          }
        } catch (e) {
          logger.error(`[${nickname}] 방 생성 처리 중 오류 발생: ${e.message}`);
          sendError(ws, 'room_create_failed');
        }
      }

      else if (data.type === 'join_room') {
        logger.info(`[${nickname}] 방 참가 요청`, data.roomId ? { roomId: data.roomId } : { random: true });
        try {
          let roomId = data.roomId;
          let room = null;
          let responseData = null;

          if (roomId) { // 특정 방 참가
            room = rooms.get(roomId);
            if (!room) {
              sendError(ws, 'room_not_found');
              return;
            }
            if (room.state !== 0 && room.state !== -1) {
              sendError(ws, 'room_not_joinable');
              return;
            }
            responseData = { ok: true, ip: room.ip, port: room.port };
          } else { // 랜덤 매칭
            roomId = pickRandomWaitingRoom() || generateRoomId();
            logger.info(`[${nickname}] 참가할 방 결정: ${roomId}`);
            room = await createRoom(roomId); // 없으면 생성, 있으면 정보 가져오기
            responseData = { ok: true, ip: room.ip, port: room.port, roomId };
          }

          if (room.state === 0) {
            logger.info(`[방 ${roomId}] 즉시 응답 가능. 응답을 전송합니다.`);
            sendMessage(ws, 'join_room_response', responseData);
          } else {
            logger.info(`[방 ${roomId}] 게임 서버 준비 대기. 요청을 큐에 추가합니다.`);
            addPendingRequest(roomId, ws, 'join_room_response', responseData);
          }
        } catch (e) {
          logger.error(`[${nickname}] 방 참가 처리 중 오류 발생: ${e.message}`);
          sendError(ws, 'room_not_available');
        }
      }
    } catch (e) {
      logger.error(`[WebSocket ${connectionId}] 메시지 처리 오류: ${e.message}`);
      sendError(ws, 'invalid_message');
    }
  });

  ws.on('close', () => handleDisconnection(ws, 'close'));
  ws.on('error', (err) => handleDisconnection(ws, 'error', err));

  function handleDisconnection(ws, reason, err = null) {
    const connection = connections.get(ws);
    if (connection) {
      const { sessionId, nickname } = connection;
      logger.info(`[WebSocket] 연결 해제: ${nickname} (${sessionId}), 사유: ${reason}${err ? `, 오류: ${err.message}` : ''}`);
      users.delete(sessionId);
      connections.delete(ws);
      cleanupPendingRequests(ws);
    } else {
      logger.info(`[WebSocket ${connectionId}] 인증되지 않은 연결 해제, 사유: ${reason}`);
      connections.delete(ws);
    }
  }
});

const PORT = process.env.PORT || 3000;
httpServer.listen(PORT, () => logger.info(`로비 서버가 포트 ${PORT}에서 실행됩니다.`));