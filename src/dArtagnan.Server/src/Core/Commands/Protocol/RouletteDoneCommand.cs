using System.Collections.Immutable;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 룰렛 완료 명령 - 플레이어가 정확도 룰렛을 완료했을 때 처리합니다
/// 모든 플레이어 완료시 첫 라운드를 즉시 시작합니다
/// </summary>
public class RouletteDoneCommand : IGameCommand
{
    required public int PlayerId;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;
        if (player.Bankrupt) return;

        // 이미 완료한 플레이어인지 확인
        if (gameManager.rouletteDonePlayers.Contains(player)) return;
        gameManager.rouletteDonePlayers.Add(player);
        var requiredPlayers = gameManager.Players.Values.Where(p => !p.Bankrupt).ToList();

        Console.WriteLine(
            $"[룰렛] 플레이어 {PlayerId} 룰렛 완료 ({gameManager.rouletteDonePlayers.Count}/{requiredPlayers.Count})");

        // 모든 플레이어가 룰렛을 완료했는지 확인
        if (requiredPlayers.TrueForAll(gameManager.rouletteDonePlayers.Contains))
        {
            Console.WriteLine($"[룰렛] 모든 플레이어 룰렛 완료 - 라운드 {gameManager.Round + 1} 시작!");
            await gameManager.StartRoundStateAsync(gameManager.Round + 1);
        }
    }
}