using System;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 로비 서버와의 HTTP/WebSocket 통신을 위한 프로토콜 정의
    /// 
    /// == 로비 서버 API 명세 ==
    /// 
    /// ### HTTP API
    /// - POST /login: 닉네임으로 로그인하여 sessionId 발급
    /// - POST /internal/rooms/:roomId/state: 게임 서버가 방 상태를 업데이트 (내부 API)
    /// 
    /// ### WebSocket API 플로우
    /// 1. HTTP 로그인으로 sessionId 획득
    /// 2. WebSocket 연결 후 auth 메시지로 인증
    /// 3. 인증 성공 후 create_room 또는 join_room 요청
    /// 4. 성공 시 게임 서버 IP:Port 정보 수신
    /// 5. 해당 정보로 TCP 연결하여 게임 플레이
    /// 
    /// ### 에러 처리
    /// 모든 에러는 { "type": "error", "code": "error_code" } 형식으로 통일
    /// 
    /// ### 방 상태 (Room State)
    /// - 0: Waiting (대기 중, 참가 가능)
    /// - 1: Round (라운드 진행 중)
    /// - 2: Roulette (룰렛 진행 중)  
    /// - 3: Augment (증강 선택 중)
    /// </summary>
    public static class LobbyProtocol
    {
        // ===========================
        // HTTP API 메시지
        // ===========================

        /// <summary>
        /// POST /login 요청
        /// 닉네임을 서버에 전송하여 sessionId를 발급받음
        /// </summary>
        [Serializable]
        public class LoginRequest
        {
            public string nickname;
        }

        /// <summary>
        /// POST /login 성공 응답
        /// sessionId와 함께 닉네임을 다시 반환
        /// </summary>
        [Serializable]
        public class LoginResponse
        {
            public string sessionId;
            public string nickname;
        }

        /// <summary>
        /// HTTP API 에러 응답 (400, 409 등)
        /// Unity에서는 JsonUtility.FromJson&lt;ErrorResponse&gt;로 파싱
        /// </summary>
        [Serializable]
        public class ErrorResponse
        {
            public string code;    // 에러 코드 (예: invalid_nickname, duplicate_nickname)
            public string message; // 사용자에게 표시할 메시지 (옵션)
        }

        /// <summary>
        /// POST /internal/rooms/:roomId/state 요청 (게임서버 -> 로비서버)
        /// 게임 서버가 자신의 상태를 로비 서버에 보고할 때 사용
        /// </summary>
        [Serializable]
        public class UpdateRoomStateRequest
        {
            public int state; // 0: Waiting, 1: Round, 2: Roulette, 3: Augment
        }

        // ===========================
        // WebSocket 메시지
        // ===========================

        /// <summary>
        /// 클라이언트 -> 서버: WebSocket 연결 후 첫 번째 메시지
        /// HTTP 로그인으로 받은 sessionId를 전송하여 인증
        /// </summary>
        [Serializable]
        public class AuthMessage
        {
            public string type = "auth";
            public string sessionId;
        }

        /// <summary>
        /// 서버 -> 클라이언트: 인증 성공 응답
        /// 이 메시지를 받으면 방 생성/참가 요청을 보낼 수 있음
        /// </summary>
        [Serializable]
        public class AuthSuccessMessage
        {
            public string type = "auth_success";
            public bool ok = true;
        }

        /// <summary>
        /// 클라이언트 -> 서버: 새 방 생성 요청
        /// roomId를 지정하지 않으면 서버가 자동 생성
        /// </summary>
        [Serializable]
        public class CreateRoomMessage
        {
            public string type = "create_room";
            public string roomId; // Optional: null이면 서버가 자동 생성
        }

        /// <summary>
        /// 서버 -> 클라이언트: 방 생성 성공 응답
        /// 게임 서버의 TCP 연결 정보를 포함
        /// </summary>
        [Serializable]
        public class CreateRoomResponseMessage
        {
            public string type = "create_room_response";
            public bool ok;
            public string roomId;  // 생성된 방 ID
            public string ip;      // 게임 서버 IP
            public int port;       // 게임 서버 포트
        }

        /// <summary>
        /// 클라이언트 -> 서버: 방 참가 요청
        /// roomId를 지정하지 않으면 랜덤 매칭 또는 새 방 생성
        /// </summary>
        [Serializable]
        public class JoinRoomMessage
        {
            public string type = "join_room";
            public string roomId; // Optional: null이면 랜덤 매칭
        }

        /// <summary>
        /// 서버 -> 클라이언트: 방 참가 성공 응답
        /// 랜덤 매칭의 경우 할당된 방 ID도 함께 반환
        /// </summary>
        [Serializable]
        public class JoinRoomResponseMessage
        {
            public string type = "join_room_response";
            public bool ok;
            public string roomId;  // 참가한 방 ID (랜덤 매칭 시 할당된 ID)
            public string ip;      // 게임 서버 IP
            public int port;       // 게임 서버 포트
        }

        /// <summary>
        /// 서버 -> 클라이언트: 통합 에러 메시지
        /// 모든 WebSocket 에러는 이 형식으로 전송됨
        /// </summary>
        [Serializable]
        public class ErrorMessage
        {
            public string type = "error";
            public string code;    // 에러 코드 (LobbyErrorCode 상수 참고)
        }

        // ===========================
        // 에러 코드 상수
        // ===========================

        /// <summary>
        /// 로비 서버 에러 코드 정의
        /// 클라이언트에서 에러 코드에 따른 분기 처리 시 사용
        /// </summary>
        public static class ErrorCode
        {
            // HTTP Login 에러
            public const string InvalidNickname = "invalid_nickname";     // 닉네임 형식 오류 (1-16자)
            public const string DuplicateNickname = "duplicate_nickname"; // 이미 사용 중인 닉네임
            public const string NullNickname = "null_nickname";           // 닉네임이 전송되지 않음

            // WebSocket 인증 에러
            public const string Unauthorized = "unauthorized";            // 유효하지 않은 sessionId
            public const string NotAuthenticated = "not_authenticated";   // 인증되지 않은 상태에서 요청

            // 방 생성/참가 에러
            public const string RoomCreateFailed = "room_create_failed";  // 방 생성 실패 (Docker 오류 등)
            public const string RoomNotFound = "room_not_found";          // 존재하지 않는 방
            public const string RoomNotJoinable = "room_not_joinable";    // 게임 진행 중인 방
            public const string RoomNotAvailable = "room_not_available";  // 방 참가 불가

            // 일반 에러
            public const string InvalidMessage = "invalid_message";       // 잘못된 메시지 형식
        }

        // ===========================
        // 방 상태 열거형
        // ===========================

        /// <summary>
        /// 게임 방의 현재 상태
        /// 로비 서버는 이 상태를 기반으로 방 참가 가능 여부를 판단
        /// </summary>
        public enum RoomState
        {
            Waiting = 0,    // 대기 중 (참가 가능)
            Round = 1,      // 라운드 진행 중 (참가 불가)
            Roulette = 2,   // 룰렛 진행 중 (참가 불가)
            Augment = 3     // 증강 선택 중 (참가 불가)
        }

        // ===========================
        // Unity 편의 클래스
        // ===========================

        /// <summary>
        /// Unity에서 방 생성 결과를 처리하기 위한 클래스
        /// LobbyManager의 이벤트 콜백에서 사용
        /// </summary>
        [Serializable]
        public class CreateRoomResult
        {
            public bool success;
            public string roomId;
            public string ip;
            public int port;
            public string errorCode; // 실패 시 에러 코드
        }

        /// <summary>
        /// Unity에서 방 참가 결과를 처리하기 위한 클래스
        /// LobbyManager의 이벤트 콜백에서 사용
        /// </summary>
        [Serializable]
        public class JoinRoomResult
        {
            public bool success;
            public string roomId;
            public string ip;
            public int port;
            public string errorCode; // 실패 시 에러 코드
        }
    }

    /// <summary>
    /// Unity JsonUtility 호환성을 위한 간소화된 메시지 클래스들
    /// JsonUtility는 중첩 클래스를 잘 처리하지 못하므로 최상위 레벨에 정의
    /// </summary>

    // HTTP 메시지
    [Serializable] public class LoginRequest { public string nickname; }
    [Serializable] public class LoginResponse { public string sessionId; public string nickname; }
    [Serializable] public class ErrorResponse { public string code; public string message; }

    // WebSocket 메시지
    [Serializable] public class AuthMessage { public string type = "auth"; public string sessionId; }
    [Serializable] public class AuthSuccessMessage { public string type = "auth_success"; public bool ok = true; }
    [Serializable] public class CreateRoomMessage { public string type = "create_room"; public string roomId; }
    [Serializable] public class CreateRoomResponseMessage { public string type = "create_room_response"; public bool ok; public string roomId; public string ip; public int port; }
    [Serializable] public class JoinRoomMessage { public string type = "join_room"; public string roomId; }
    [Serializable] public class JoinRoomResponseMessage { public string type = "join_room_response"; public bool ok; public string roomId; public string ip; public int port; }
    [Serializable] public class ErrorMessage { public string type = "error"; public string code; }
    [Serializable] public class MessageType { public string type; }

    // Unity 편의 클래스
    [Serializable] public class CreateRoomResult { public bool success; public string roomId; public string ip; public int port; public string errorCode; }
    [Serializable] public class JoinRoomResult { public bool success; public string roomId; public string ip; public int port; public string errorCode; }
}