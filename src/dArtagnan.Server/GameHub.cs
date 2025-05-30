using dArtagnan.Shared;
using MagicOnion.Server.Hubs;
using UnityEngine;

namespace dArtagnan.Server;

public class GameHub : StreamingHubBase<IGameHub, IGameHubReceiver>, IGameHub
{
    public async ValueTask JoinAsync(int roomId)
    {
        throw new NotImplementedException();
    }

    public async ValueTask MoveAsync(Vector2 position)
    {
        throw new NotImplementedException();
    }

    public async ValueTask ShootAsync(int target)
    {
        throw new NotImplementedException();
    }
}