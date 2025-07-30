using dArtagnan.Shared;

namespace dArtagnan.Server.Core.Commands.Protocol;

public struct SelectAccuracyCommand : IGameCommand
{
    public required int PlayerId;
    public required int AccuracyIndex;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        if (!gameManager.WaitingForAccuracySelection) return;
        if (PlayerId != gameManager.AccuracySelectionTurn) return;
        var selectedAccuracyItem = gameManager.AccuracyPool[AccuracyIndex];
        if (selectedAccuracyItem.Taken) return;
        gameManager.AccuracySelectionResult[PlayerId] = gameManager.AccuracyPool[AccuracyIndex].Accuracy;
        gameManager.AccuracyPool[AccuracyIndex].Taken = true;
        await gameManager.BroadcastToAll(new PlayerHasSelectedAccuracyFromServer
            { AccuracyIndex = AccuracyIndex, PlayerId = PlayerId });
        var next =
            gameManager.Players.Values.FirstOrDefault(p => !gameManager.AccuracySelectionResult.ContainsKey(p.Id));
        if (next is null)
        {
            gameManager.WaitingForAccuracySelection = false;
            await InitializeAllPlayers(gameManager);
            await gameManager.StartNextRoundAsync(1);
        }
        else
        {
            gameManager.AccuracySelectionTurn = next.Id;
            await gameManager.BroadcastToAll(new PlayerTurnToSelectAccuracy { PlayerId = next.Id });
        }
    }

    /// <summary>
    ///     모든 플레이어를 게임용으로 초기화합니다
    /// </summary>
    private static Task InitializeAllPlayers(GameManager gameManager)
    {
        // 각 플레이어 초기화 & 정확도 할당
        foreach (var player in gameManager.Players.Values)
        {
            player.ResetForInitialGame(gameManager.AccuracySelectionResult[player.Id]);
            Console.WriteLine($"[초기화] {player.Nickname}: {player.Accuracy}% (잔액: {player.Balance}달러)");
        }

        // 위치 재배치 (한 번만)
        gameManager.ResetRespawnAll(true);
        return Task.CompletedTask;
    }
}