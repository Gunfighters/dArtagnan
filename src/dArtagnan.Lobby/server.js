import express from 'express';
import Docker from 'dockerode';
import net from 'node:net';
import http from 'http';
import { Server as SocketIOServer } from 'socket.io';

const app = express();
const httpServer = http.createServer(app);
const io = new SocketIOServer(httpServer, { cors: { origin: '*' } });
app.use(express.json());

// Docker 연결 (플랫폼 자동 감지)
const docker = process.platform === 'win32' 
  ? new Docker() 
  : new Docker({ socketPath: '/var/run/docker.sock' });

// 상태 저장소
const rooms = new Map(); // roomId -> { containerId, ip, port, state }
const users = new Map(); // sessionId -> { nickname }

// 설정
const IMAGE = 'dartagnan-gameserver:latest';
const INTERNAL_PORT = 7777;
const HOST_IP = '127.0.0.1';

async function createRoom(roomId) {
  // Return existing
  if (rooms.has(roomId)) return rooms.get(roomId);

  const lobbyUrl = `http://host.docker.internal:${process.env.PORT || 3000}`;

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

  // 포트가 준비될 때까지 대기
  try {
    await waitForPort(HOST_IP, Number(hostPort));
    await new Promise(resolve => setTimeout(resolve, 200)); // 짧은 대기
  } catch (e) {
    try { await container.stop({ t: 1 }); } catch {}
    throw e;
  }

  const room = { containerId: container.id, ip: HOST_IP, port: Number(hostPort), state: 0 };
  rooms.set(roomId, room);
  console.log(`Room created: ${roomId} -> ${HOST_IP}:${hostPort}`);

  // 컨테이너 종료 시 방 삭제
  container.wait().then(() => rooms.delete(roomId)).catch(() => {});
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
  const nickname = (req.body?.nickname || '').trim();
  if (!nickname || nickname.length < 1 || nickname.length > 16) {
    return res.status(400).json({ error: 'invalid_nickname' });
  }
  // 중복 체크
  for (const user of users.values()) {
    if (user.nickname === nickname) return res.status(409).json({ error: 'duplicate_nickname' });
  }
  const sessionId = Math.random().toString(36).slice(2);
  users.set(sessionId, { nickname });
  res.json({ sessionId, nickname });
});

// 게임서버 상태 업데이트
app.post('/internal/rooms/:roomId/state', (req, res) => {
  const room = rooms.get(req.params.roomId);
  if (!room) return res.status(404).json({ error: 'room_not_found' });
  room.state = req.body.state;
  res.json({ ok: true });
});

// 소켓 인증
io.use((socket, next) => {
  const sessionId = socket.handshake.auth?.sessionId;
  if (!sessionId || !users.has(sessionId)) return next(new Error('unauthorized'));
  socket.data.sessionId = sessionId;
  next();
});

io.on('connection', (socket) => {
  // 방 생성
  socket.on('create_room', async (payload, ack) => {
    try {
      const roomId = payload?.roomId || generateRoomId();
      const room = await createRoom(roomId);
      ack?.({ ok: true, roomId, ip: room.ip, port: room.port });
    } catch {
      ack?.({ ok: false, code: 'room_create_failed' });
    }
  });

  // 방 참가
  socket.on('join_room', async (payload, ack) => {
    try {
      let roomId = payload?.roomId;
      
      if (roomId) {
        // 특정 방 참가
        const room = rooms.get(roomId);
        if (!room) return ack?.({ ok: false, code: 'room_not_found' });
        if (room.state !== 0) return ack?.({ ok: false, code: 'room_not_joinable' });
        return ack?.({ ok: true, ip: room.ip, port: room.port });
      }
      
      // 랜덤 매칭 또는 새 방 생성
      roomId = pickRandomWaitingRoom() || generateRoomId();
      const room = roomId ? rooms.get(roomId) : null;
      
      if (room) {
        ack?.({ ok: true, ip: room.ip, port: room.port, roomId });
      } else {
        const newRoom = await createRoom(roomId);
        ack?.({ ok: true, ip: newRoom.ip, port: newRoom.port, roomId });
      }
    } catch {
      ack?.({ ok: false, code: 'room_not_available' });
    }
  });
});

const PORT = process.env.PORT || 3000;
httpServer.listen(PORT, () => console.log(`Lobby server running on port ${PORT}`));


