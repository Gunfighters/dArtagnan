using dArtagnan.Shared;
using Game;
using R3;
using UnityEditor;
using UnityEngine;

namespace UI.HUD
{
    public static class HUDModel
    {
        public static readonly ReactiveProperty<bool> Controlling = new();
        public static readonly ReactiveProperty<bool> Spectating = new();
        public static readonly ReactiveProperty<bool> Waiting = new();
        public static readonly ReactiveProperty<bool> Playing = new();
        public static readonly ReactiveProperty<bool> InRound = new();
        public static readonly ReactiveProperty<bool> IsHost = new();

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(_ =>
            {
                InRound.Value = true;
                Waiting.Value = false;
                Playing.Value = PlayerGeneralManager.LocalPlayerCore.Health.Alive;
            });
            PacketChannel.On<WaitingStartFromServer>(_ =>
            {
                InRound.Value = false;
                Waiting.Value = true;
                Playing.Value = false;
            });
            LocalEventChannel.OnLocalPlayerAlive += alive =>
            {
                Controlling.Value = alive;
                Spectating.Value = !alive;
                Playing.Value = alive && InRound.Value;
            };
            LocalEventChannel.OnNewHost += (_, isLocalPlayerHost) => { IsHost.Value = isLocalPlayerHost; };
        }
    }
}