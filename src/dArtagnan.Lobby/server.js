import express from 'express';
import Docker from 'dockerode';
import pino from 'pino';
import net from 'node:net';
import http from 'http';
import { Server as SocketIOServer } from 'socket.io';

const app = express();
const httpServer = http.createServer(app);
const io = new SocketIOServer(httpServer, { cors: { origin: '*' } });
app.use(express.json());
const log = pino({ level: process.env.LOG_LEVEL || 'info' });
const docker = new Docker();

// In-memory state
const rooms = new Map(); // roomId -> { containerId, ip, port, state }
const users = new Map(); // sessionId -> { nickname }

const IMAGE = process.env.GAME_IMAGE || 'dartagnan-server:latest';
const INTERNAL_PORT = Number(process.env.GAME_INTERNAL_PORT || 7777);
const HOST_IP = process.env.PUBLIC_HOST || '127.0.0.1';
const READINESS_TIMEOUT_MS = 5000;
const READINESS_RETRY_MS = 150;

async function createRoom(roomId) {
  // Return existing
  if (rooms.has(roomId)) return rooms.get(roomId);

  const lobbyUrl = process.env.LOBBY_INTERNAL_URL || `http://host.docker.internal:${process.env.PORT || 3000}`;

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

  await container.start();

  // Wait until port is bound
  const info = await container.inspect();
  const bindings = info.NetworkSettings.Ports[`${INTERNAL_PORT}/tcp`];
  const hostPort = bindings && bindings[0] && bindings[0].HostPort;
  if (!hostPort) {
    throw new Error('Failed to get host port binding');
  }

  // Ready check: wait until TCP port accepts connections
  try {
    await waitForPort(HOST_IP, Number(hostPort), READINESS_TIMEOUT_MS, READINESS_RETRY_MS);
  } catch (e) {
    // Cleanup container and rethrow
    try {
      await container.stop({ t: 1 });
    } catch {}
    throw e;
  }

  const room = { containerId: container.id, ip: HOST_IP, port: Number(hostPort), state: 0 };
  rooms.set(roomId, room);
  log.info({ roomId, room }, 'Room created');

  // 컨테이너 종료 감지 후 방 정리
  (async () => {
    try {
      await container.wait();
      rooms.delete(roomId);
      log.info({ roomId }, 'Container exited, room cleaned');
    } catch (e) {
      log.warn({ roomId, err: e }, 'Error while waiting for container exit');
    }
  })();
  return room;
}

function waitForPort(host, port, timeoutMs, retryMs) {
  const started = Date.now();
  return new Promise((resolve, reject) => {
    (function attempt() {
      const socket = net.createConnection({ host, port }, () => {
        socket.end();
        resolve(true);
      });
      socket.once('error', () => {
        socket.destroy();
        if (Date.now() - started >= timeoutMs) return reject(new Error('port_wait_timeout'));
        setTimeout(attempt, retryMs);
      });
    })();
  });
}

function pickRandomWaitingRoomId() {
  const waiting = Array.from(rooms.entries())
    .filter(([, r]) => (r.state ?? 0) === 0)
    .map(([id]) => id);
  if (waiting.length === 0) return null;
  const idx = Math.floor(Math.random() * waiting.length);
  return waiting[idx];
}

function generateRoomId() {
  return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}

// 로그인(HTTP) - 닉네임 중복 체크만 수행
app.post('/login', (req, res) => {
  const nickname = (req.body?.nickname || '').trim();
  if (!nickname || nickname.length < 1 || nickname.length > 16) {
    return res.status(400).json({ error: 'invalid_nickname' });
  }
  for (const { nickname: n } of users.values()) {
    if (n === nickname) return res.status(409).json({ error: 'duplicate_nickname' });
  }
  const sessionId = Math.random().toString(36).slice(2) + Math.random().toString(36).slice(2);
  users.set(sessionId, { nickname });
  return res.json({ sessionId, nickname });
});

// 게임서버가 상태 보고
app.post('/internal/rooms/:roomId/state', (req, res) => {
  const { roomId } = req.params;
  const { state } = req.body || {};
  const room = rooms.get(roomId);
  if (!room) return res.status(404).json({ error: 'room_not_found' });
  if (typeof state !== 'number') return res.status(400).json({ error: 'invalid_state' });
  room.state = state;
  return res.json({ ok: true });
});

// 소켓 인증 미들웨어(세션 필수)
io.use((socket, next) => {
  const sessionId = socket.handshake.auth?.sessionId;
  if (!sessionId || !users.has(sessionId)) return next(new Error('unauthorized'));
  socket.data.sessionId = sessionId;
  next();
});

io.on('connection', (socket) => {
  // 이벤트: 방 생성
  socket.on('create_room', async (payload, ack) => {
    try {
      const providedId = (payload?.roomId || '').trim();
      const roomId = providedId.length === 0 ? generateRoomId() : providedId;
      const room = await createRoom(roomId);
      ack?.({ ok: true, roomId, ip: room.ip, port: room.port });
    } catch (e) {
      ack?.({ ok: false, code: 'room_create_failed' });
    }
  });

  // 이벤트: 방 참가(옵션 roomId 없으면 Waiting 중 랜덤 배정, 없으면 새로 생성)
  socket.on('join_room', async (payload, ack) => {
    try {
      let roomId = (payload?.roomId || '').trim();
      if (roomId.length > 0) {
        const room = rooms.get(roomId);
        if (!room) return ack?.({ ok: false, code: 'room_not_found' });
        if ((room.state ?? 0) !== 0) return ack?.({ ok: false, code: 'room_not_joinable' });
        return ack?.({ ok: true, ip: room.ip, port: room.port });
      }

      // 랜덤 배정
      const picked = pickRandomWaitingRoomId();
      if (picked) {
        const room = rooms.get(picked);
        return ack?.({ ok: true, ip: room.ip, port: room.port, roomId: picked });
      }

      // 없으면 새로 생성
      roomId = generateRoomId();
      const roomNew = await createRoom(roomId);
      return ack?.({ ok: true, ip: roomNew.ip, port: roomNew.port, roomId });
    } catch (e) {
      ack?.({ ok: false, code: 'room_not_available' });
    }
  });
});

const PORT = Number(process.env.PORT || 3000);
httpServer.listen(PORT, () => log.info(`Lobby listening on ${PORT}`));


