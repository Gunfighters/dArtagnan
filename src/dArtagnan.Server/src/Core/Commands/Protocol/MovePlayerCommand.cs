using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 이동 명령 - 플레이어의 이동 데이터를 업데이트하고 브로드캐스트합니다
/// </summary>
public class PlayerMovementCommand : IGameCommand
{
    public required int PlayerId;
    public required MovementData MovementData;
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null) return;

        //todo: 채굴 중에는 이동할 수 없음
        //그러나 지금 위치동기화가 클라권위구조라 일단 이동 수용하고 있음
        //클라에서 채굴 중에는 이동패킷을 보낼 수 없도록 꼭 로직이 있어야 함 (ex: Bot.cs 에선 isMiningForClient 변수를 관리하고 있음)
        if (player.IsMining) {
            Logger.log($"[⚠️⚠️⚠️⚠️ ] 플레이어 {PlayerId}는 채굴 중에 이동할 수 없습니다 (클라 로직 확인)");
            //return;
        }
        
        await player.UpdateMovementData(MovementData);
    }
} 