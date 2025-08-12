// 로비 서버 에러 코드 상수
// C# LobbyProtocol.cs와 동일한 값들을 유지

export const ErrorCodes = {
  // HTTP Login 에러
  INVALID_NICKNAME: 'invalid_nickname',
  DUPLICATE_NICKNAME: 'duplicate_nickname', 
  NULL_NICKNAME: 'null_nickname',

  // WebSocket 인증 에러
  UNAUTHORIZED: 'unauthorized',
  NOT_AUTHENTICATED: 'not_authenticated',

  // 방 생성/참가 에러
  ROOM_CREATE_FAILED: 'room_create_failed',
  ROOM_NOT_FOUND: 'room_not_found',
  ROOM_NOT_JOINABLE: 'room_not_joinable',
  ROOM_NOT_AVAILABLE: 'room_not_available',

  // 일반 에러
  INVALID_MESSAGE: 'invalid_message'
};

// 방 상태 상수
export const RoomState = {
  WAITING: 0,
  ROUND: 1,
  ROULETTE: 2,
  AUGMENT: 3
};