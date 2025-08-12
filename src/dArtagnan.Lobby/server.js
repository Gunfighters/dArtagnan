import express from 'express';
import Docker from 'dockerode';
import net from 'node:net';
import http from 'http';
import { WebSocketServer } from 'ws';
import crypto from 'crypto';

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
console.log(`[INFO] Game server host: ${HOST_IP}`);

async function createRoom(roomId) {
  console.log(`[DEBUG] createRoom called with roomId: ${roomId}`);
  
  // Return existing
  if (rooms.has(roomId)) {
    console.log(`[DEBUG] Room ${roomId} already exists, returning existing room`);
    return rooms.get(roomId);
  }

  // 컨테이너에서 로비 서버로 접근할 URL 결정
  let lobbyUrl;
  if (process.env.LOBBY_HOST_URL) {
    // 환경변수가 설정된 경우 (수동 설정)
    lobbyUrl = process.env.LOBBY_HOST_URL;
  } else if (process.platform === 'win32') {
    // Windows/Mac Docker Desktop
    lobbyUrl = `http://host.docker.internal:${process.env.PORT || 3000}`;
  } else {
    // Linux (AWS 등) - Docker 기본 브리지 게이트웨이
    lobbyUrl = `http://172.17.0.1:${process.env.PORT || 3000}`;
  }
  console.log(`[DEBUG] Creating container for room ${roomId} with lobby URL: ${lobbyUrl}`);

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
  console.log(`[DEBUG] Container created for room ${roomId}: ${container.id}`);

  await container.start();
  console.log(`[DEBUG] Container started for room ${roomId}`);

  // Wait until port is bound
  const info = await container.inspect();
  const bindings = info.NetworkSettings.Ports[`${INTERNAL_PORT}/tcp`];
  const hostPort = bindings && bindings[0] && bindings[0].HostPort;
  if (!hostPort) {
    console.error(`[DEBUG] Failed to get host port binding for room ${roomId}`);
    throw new Error('Failed to get host port binding');
  }
  console.log(`[DEBUG] Port binding successful for room ${roomId}: ${HOST_IP}:${hostPort}`);

  // *** 중요: 게임 서버가 상태를 보고하기 전에 미리 방 정보를 저장 ***
  const room = { containerId: container.id, ip: HOST_IP, port: Number(hostPort), state: -1 };
  rooms.set(roomId, room);
  console.log(`[DEBUG] Room pre-stored: ${roomId} -> ${HOST_IP}:${hostPort} (state: -1)`);

  // 포트가 준비될 때까지 대기
  console.log(`[DEBUG] Waiting for port ${hostPort} to be ready for room ${roomId}`);
  await waitForPort(HOST_IP, Number(hostPort));
  await new Promise(resolve => setTimeout(resolve, 200)); // 짧은 대기
  console.log(`[DEBUG] Port ${hostPort} is ready for room ${roomId}`);

  // 컨테이너 종료 시 방 삭제
  container.wait().then(() => {
    console.log(`[DEBUG] Container for room ${roomId} has stopped, cleaning up`);
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
  const nickname = (req.body?.nickname || '').trim();
  if (!nickname) {
    return res.status(400).json({
      code: 'null_nickname',
      message: '올바른 닉네임을 입력해주세요 (1-16자)'
    });
  }
  if (!nickname || nickname.length < 1 || nickname.length > 16) {
    return res.status(400).json({ 
      code: 'invalid_nickname', 
      message: '올바른 닉네임을 입력해주세요 (1-16자)' 
    });
  }
  // 중복 체크
  for (const user of users.values()) {
    if (user.nickname === nickname) {
      return res.status(409).json({ 
        code: 'duplicate_nickname', 
        message: '이미 사용 중인 닉네임입니다' 
      });
    }
  }
  const sessionId = Math.random().toString(36).slice(2);
  users.set(sessionId, { nickname });
  res.json({ sessionId, nickname });
});

// 게임서버 상태 업데이트
app.post('/internal/rooms/:roomId/state', (req, res) => {
  const roomId = req.params.roomId;
  const newState = req.body.state;
  console.log(`[DEBUG] Received state update for room ${roomId}: state = ${newState}`);
  
  // 현재 저장된 방 목록 출력
  const currentRooms = Array.from(rooms.keys());
  console.log(`[DEBUG] Current rooms in memory: [${currentRooms.join(', ')}] (total: ${currentRooms.length})`);
  
  const room = rooms.get(roomId);
  if (!room) {
    console.error(`[DEBUG] Room ${roomId} not found for state update`);
    console.error(`[DEBUG] Available rooms: ${currentRooms.length > 0 ? currentRooms.join(', ') : 'NONE'}`);
    return res.status(404).json({ 
      code: 'room_not_found', 
      message: '방을 찾을 수 없습니다' 
    });
  }
  
  console.log(`[DEBUG] Room ${roomId} state changed from ${room.state} to ${newState}`);
  room.state = newState;
  
  // state가 0(준비완료)이면 대기 중인 요청들에 응답 전송
  if (newState === 0 && pendingRequests.has(roomId)) {
    const requests = pendingRequests.get(roomId);
    console.log(`[DEBUG] Found ${requests.length} pending requests for room ${roomId}, sending responses`);
    
    requests.forEach(({ ws, type, responseData }) => {
      console.log(`[DEBUG] Sending response: type=${type}`);
      sendMessage(ws, type, responseData);
    });
    pendingRequests.delete(roomId);
    console.log(`[DEBUG] Sent ${requests.length} pending responses for room ${roomId} and cleared queue`);
  } else if (newState === 0) {
    console.log(`[DEBUG] No pending requests found for room ${roomId}`);
  } else {
    console.log(`[DEBUG] State is not 0 (${newState}), not processing pending requests`);
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
  console.log(`[DEBUG] Added pending request for room ${roomId}. Queue size: ${pendingRequests.get(roomId).length}`);
}

function cleanupPendingRequests(ws) {
  let cleaned = 0;
  for (const [roomId, requests] of pendingRequests.entries()) {
    const originalLength = requests.length;
    const filteredRequests = requests.filter(req => req.ws !== ws);
    
    if (filteredRequests.length !== originalLength) {
      cleaned += originalLength - filteredRequests.length;
      if (filteredRequests.length === 0) {
        pendingRequests.delete(roomId);
      } else {
        pendingRequests.set(roomId, filteredRequests);
      }
    }
  }
  
  if (cleaned > 0) {
    console.log(`[DEBUG] Cleaned up ${cleaned} pending requests for disconnected user`);
  }
}

wss.on('connection', (ws, req) => {
  let authenticated = false;
  let sessionId = null;
  console.log('[DEBUG] New WebSocket connection established');
  
  ws.on('message', async (message) => {
    try {
      const data = JSON.parse(message.toString());
      console.log(`[DEBUG] Received WebSocket message: ${JSON.stringify(data)}`);
      
      // 인증 처리
      if (data.type === 'auth') {
        sessionId = data.sessionId;
        console.log(`[DEBUG] Auth request with sessionId: ${sessionId}`);
        if (!sessionId || !users.has(sessionId)) {
          console.error(`[DEBUG] Auth failed for sessionId: ${sessionId}`);
          sendError(ws, 'unauthorized');
          ws.close();
          return;
        }
        authenticated = true;
        connections.set(ws, { sessionId, nickname: users.get(sessionId).nickname });
        sendMessage(ws, 'auth_success', { ok: true });
        console.log(`[DEBUG] Auth successful for sessionId: ${sessionId}`);
        return;
      }
      
      // 인증되지 않은 요청 거부
      if (!authenticated) {
        console.error(`[DEBUG] Unauthenticated request: ${data.type}`);
        sendError(ws, 'not_authenticated');
        return;
      }
      
      // 방 생성
      if (data.type === 'create_room') {
        console.log(`[DEBUG] Received create_room request from ${sessionId}`);
        try {
          const roomId = data.roomId || generateRoomId();
          console.log(`[DEBUG] Generated/using roomId: ${roomId}`);
          
          const room = await createRoom(roomId);
          console.log(`[DEBUG] Room created successfully: ${roomId}`);
          
          const responseData = { ok: true, roomId, ip: room.ip, port: room.port };
          
          // 게임 서버가 이미 준비되었으면 즉시 응답, 아니면 대기 큐에 추가
          if (room.state === 0) {
            console.log(`[DEBUG] Room ${roomId} is already ready, sending immediate response`);
            sendMessage(ws, 'create_room_response', responseData);
          } else {
            console.log(`[DEBUG] Room ${roomId} not ready yet, adding to pending queue`);
            addPendingRequest(roomId, ws, 'create_room_response', responseData);
          }
        } catch (e) {
          console.error(`[DEBUG] create_room failed: ${e.message}`);
          sendError(ws, 'room_create_failed');
        }
      }
      
      // 방 참가
      else if (data.type === 'join_room') {
        console.log(`[DEBUG] Received join_room request from ${sessionId}`);
        try {
          let roomId = data.roomId;
          let room = null;
          let responseData = null;
          
          if (roomId) {
            // 특정 방 참가
            console.log(`[DEBUG] Joining specific room: ${roomId}`);
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
          } else {
            // 랜덤 매칭 또는 새 방 생성
            roomId = pickRandomWaitingRoom() || generateRoomId();
            console.log(`[DEBUG] Random matching/creating room: ${roomId}`);
            room = rooms.get(roomId);
            
            if (room) {
              responseData = { ok: true, ip: room.ip, port: room.port, roomId };
            } else {
              room = await createRoom(roomId);
              responseData = { ok: true, ip: room.ip, port: room.port, roomId };
            }
          }
          
          // 게임 서버가 이미 준비되었으면 즉시 응답, 아니면 대기 큐에 추가
          if (room.state === 0) {
            console.log(`[DEBUG] Room ${roomId} is already ready, sending immediate response`);
            sendMessage(ws, 'join_room_response', responseData);
          } else {
            console.log(`[DEBUG] Room ${roomId} not ready yet, adding to pending queue`);
            addPendingRequest(roomId, ws, 'join_room_response', responseData);
          }
        } catch (e) {
          console.error(`[DEBUG] join_room failed: ${e.message}`);
          sendError(ws, 'room_not_available');
        }
      }
    } catch (e) {
      console.error(`[DEBUG] WebSocket message parsing error: ${e.message}`);
      sendError(ws, 'invalid_message');
    }
  });
  
  ws.on('close', () => {
    handleDisconnection(ws, 'close');
  });
  
  ws.on('error', (err) => {
    console.error('WebSocket error:', err);
    handleDisconnection(ws, 'error');
  });
  
  // WebSocket 연결 해제 시 로그아웃 처리
  function handleDisconnection(ws, reason) {
    const connection = connections.get(ws);
    if (connection) {
      const { sessionId, nickname } = connection;
      console.log(`[DEBUG] User ${nickname} (${sessionId}) disconnected (${reason})`);
      
      // 사용자 세션 정리
      users.delete(sessionId);
      connections.delete(ws);
      
      // 해당 사용자의 대기 중인 요청도 정리
      cleanupPendingRequests(ws);
      
      console.log(`[DEBUG] Logout processed for ${nickname}`);
    } else {
      console.log(`[DEBUG] Unauthenticated connection disconnected (${reason})`);
      connections.delete(ws);
    }
  }
});

const PORT = process.env.PORT || 3000;
httpServer.listen(PORT, () => console.log(`Lobby server running on port ${PORT}`));