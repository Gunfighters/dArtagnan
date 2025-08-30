using System;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 로비 서버와의 통신 프로토콜
    /// 
    /// HTTP API: POST /login
    /// WebSocket API: auth -> create_room/join_room -> 게임서버 TCP 연결
    /// 
    /// </summary>
    public static class LobbyProtocol
    {
        /// <summary>에러 코드 상수</summary>
        public static class ErrorCode
        {
            public const string InvalidNickname = "invalid_nickname";
            public const string DuplicateNickname = "duplicate_nickname";
            public const string NullNickname = "null_nickname";
            public const string Unauthorized = "unauthorized";
            public const string NotAuthenticated = "not_authenticated";
            public const string RoomCreateFailed = "room_create_failed";
            public const string RoomNotFound = "room_not_found";
            public const string RoomNotJoinable = "room_not_joinable";
            public const string RoomNotAvailable = "room_not_available";
            public const string InvalidMessage = "invalid_message";
        }

        /// <summary>방 상태</summary>
        public enum RoomState
        {
            Waiting = 0,
            Round = 1,
            Showdown = 2,
            Augment = 3
        }

        /// <summary>Unity 이벤트용 결과 클래스</summary>
        [Serializable]
        public class CreateRoomResult
        {
            public bool success;
            public string roomId;
            public string ip;
            public int port;
            public string errorCode;
        }

        [Serializable]
        public class JoinRoomResult
        {
            public bool success;
            public string roomId;
            public string ip;
            public int port;
            public string errorCode;
        }
    }

    // ===========================
    // 통신 메시지 클래스들
    // ===========================

    // HTTP 메시지
    [Serializable] public class LoginRequest { public string providerId; }
    [Serializable] public class LoginResponse { public string sessionId; public string nickname; }
    [Serializable] public class ErrorResponse { public string code; public string message; }

    // WebSocket 메시지  
    [Serializable] public class AuthMessage { public string type = "auth"; public string sessionId; }
    [Serializable] public class CreateRoomMessage { public string type = "create_room"; public string roomId; }
    [Serializable] public class CreateRoomResponseMessage { public string type = "create_room_response"; public bool ok; public string roomId; public string ip; public int port; }
    [Serializable] public class JoinRoomMessage { public string type = "join_room"; public string roomId; }
    [Serializable] public class JoinRoomResponseMessage { public string type = "join_room_response"; public bool ok; public string roomId; public string ip; public int port; }
    [Serializable] public class ErrorMessage { public string type = "error"; public string code; }
    [Serializable] public class MessageType { public string type; }
}