using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 관리자 플레이어 강제 사망 명령
/// </summary>
public class AdminKillCommand : IGameCommand
{
    public required int PlayerId;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);

        if (player == null)
        {
            Logger.log($"[관리자] 플레이어 ID {PlayerId}를 찾을 수 없습니다");
            return;
        }

        if (!player.Alive)
        {
            Logger.log($"[관리자] 플레이어 {PlayerId}({player.Nickname})는 이미 사망한 상태입니다");
            return;
        }

        await player.Die();

        Logger.log($"[관리자] 플레이어 {PlayerId}({player.Nickname})를 강제로 사망시켰습니다");

        await gameManager.BroadcastToAll(new ChatBroadcast
        {
            PlayerId = -1,
            Message = $"관리자에 의해 {player.Nickname}님이 제거되었습니다"
        });

        await gameManager.CheckAndHandleGameEndAsync();
    }
} 