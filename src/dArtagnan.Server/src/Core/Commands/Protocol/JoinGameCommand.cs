using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 참가 명령 - 새로운 플레이어가 게임에 참가할 때 처리합니다
/// </summary>
public class PlayerJoinCommand : IGameCommand
{
    required public int ClientId;
    required public string Nickname;
    required public ClientConnection Client;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        // [REMOVED FOR PORTFOLIO]
    }
} 