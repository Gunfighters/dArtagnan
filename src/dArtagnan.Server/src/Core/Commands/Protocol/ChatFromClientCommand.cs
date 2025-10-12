using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어가 증강을 선택했을 때 처리하는 명령
/// </summary>
public class ChatFromClientCommand : IGameCommand
{
    required public int PlayerId;
    required public string Message;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        await gameManager.BroadcastToAll(new ChatBroadcast
        {
            PlayerId = PlayerId,
            Message = Message
        });
    }
}