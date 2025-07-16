using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 상태 업데이트 명령 - 주기적으로 게임 상태를 업데이트합니다 (위치, 재장전, 정확도 등)
/// </summary>
public class UpdateGameStateCommand : IGameCommand
{
    public required float DeltaTime { get; init; }
    
    public Task ExecuteAsync(GameManager gameManager)
    {
        // 플레이어 상태 업데이트
        foreach (var player in gameManager.Players.Values)
        {
            if (!player.Alive) continue;
            
            // 위치 업데이트
            var newPosition = CalculateNewPosition(player.MovementData, DeltaTime);
            if (Vector2.Distance(newPosition, player.MovementData.Position) > 0.01f)
            {
                player.UpdatePosition(newPosition);
            }
            
            // 재장전 시간 업데이트
            if (player.RemainingReloadTime > 0)
            {
                player.UpdateReloadTime(Math.Max(0, player.RemainingReloadTime - DeltaTime));
            }
            
            // 정확도 업데이트
            player.UpdateAccuracy(DeltaTime);
        }
        
        return Task.CompletedTask;
    }
    
    private static Vector2 CalculateNewPosition(MovementData movementData, float deltaTime)
    {
        var vector = DirectionHelper.IntToDirection(movementData.Direction);
        if (vector == Vector2.Zero) return movementData.Position;
        
        return movementData.Position + vector * movementData.Speed * deltaTime;
    }
} 