#pragma warning disable CS8618

using System;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 로비 서버와의 통신 프로토콜
    /// 
    /// </summary>

    /// 공용 구조체(직접 패킷 X)
    
    [Serializable] public class RoomInfo { public string roomId; public string roomName; public int playerCount; public int maxPlayers; public bool joinable; public string ip; public int port; }
    
    // HTTP 메시지
    [Serializable] public class LoginRequest { public string providerId; }
    [Serializable] public class LoginResponse { public string sessionId; public string nickname; }
    [Serializable] public class ErrorResponse { public string message; }

    // WebSocket 메시지
    [Serializable] public class CreateRoomMessage { public string type = "create_room"; public string roomName; }
    [Serializable] public class CreateRoomResponseMessage { public string type = "create_room_response"; public bool ok; public string roomId; public string roomName; public string ip; public int port; }
    [Serializable] public class JoinRoomMessage { public string type = "join_room"; public string roomId; }
    [Serializable] public class JoinRoomResponseMessage { public string type = "join_room_response"; public bool ok; public string roomId; public string roomName; public string ip; public int port; }
    [Serializable] public class UpdateRoomNameMessage { public string type = "update_room_name"; public string roomName; }
    [Serializable] public class UpdateRoomNameResponseMessage { public string type = "update_room_name_response"; public string roomId; public string roomName; }
    [Serializable] public class ErrorMessage { public string type = "error"; public string message; }
    [Serializable] public class MessageType { public string type; }
    [Serializable] public class NicknameSubmission { public string type = "set_nickname"; public string nickname; }
    [Serializable] public class NicknameSetResponse { public string type = "nickname_set"; public bool success; public string nickname; public string error; }

    // 방 목록 관련 메시지
    [Serializable] public class RoomsUpdateMessage { public string type = "rooms_update"; public RoomInfo[] rooms; }

    // 개별 업데이트 메시지들
    [Serializable] public class UpdateNicknameMessage { public string type = "update_nickname"; public string nickname; }
    [Serializable] public class UpdateGoldMessage { public string type = "update_gold"; public int gold; }
    [Serializable] public class UpdateCostumeMessage { public string type = "update_costume"; public int currentCostume; }
    [Serializable] public class UpdateInventoryMessage { public string type = "update_inventory"; public int[] ownedCostumes; }

    // 상점 관련 메시지들
    [Serializable] public class ShopCostumeBox1Message { public string type = "shop_costume_box1"; }
    [Serializable] public class ShopCostumeBox1Response { public string type = "shop_costume_box1_response"; public bool success; public int[] roulettePool; public int wonCostume; public string error; }
    [Serializable] public class GetCostumeRatesMessage { public string type = "get_costume_rates"; }
    [Serializable] public class CostumeRatesResponse { public string type = "costume_rates_response"; public CostumeRate[] rates; }
    [Serializable] public class ChangeCostumeMessage { public string type = "change_costume"; public int costumeId; }

    // 코스튬 확률 정보
    [Serializable] public class CostumeRate { public int costumeId; public float rate; }
}

#pragma warning restore CS8618