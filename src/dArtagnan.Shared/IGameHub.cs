using System.Threading.Tasks;
using MagicOnion;
using UnityEngine;

namespace dArtagnan.Shared
{
    public interface IGameHub : IStreamingHub<IGameHub, IGameHubReceiver>
    {
        ValueTask JoinAsync(int roomId);
        ValueTask MoveAsync(Vector2 position);
        ValueTask ShootAsync(int target);
    }

    public interface IGameHubReceiver
    {
        void OnJoin(int accuracy);
        void OnMove(int index, Vector2 position);
        void OnShoot(int target);
        void OnDeath(int index);
    }
}