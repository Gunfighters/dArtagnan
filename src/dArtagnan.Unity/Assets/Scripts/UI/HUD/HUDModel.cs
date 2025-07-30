using dArtagnan.Shared;
using R3;
using UnityEditor;

namespace UI.HUD
{
    [InitializeOnLoad]
    public static class HUDModel
    {
        public static readonly ReactiveProperty<bool> Controlling = new();
        public static readonly ReactiveProperty<bool> Spectating = new();
        public static readonly ReactiveProperty<bool> Waiting = new();
        public static readonly ReactiveProperty<bool> Playing = new();
        public static readonly ReactiveProperty<bool> InRound = new();
        public static readonly ReactiveProperty<bool> IsHost = new();

        static HUDModel()
        {
            PacketChannel.On<RoundStartFromServer>(_ =>
            {
                InRound.Value = true;
                Waiting.Value = false;
                Playing.Value = true;
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
            LocalEventChannel.OnNewHost += (_, isLocalPlayerHost) =>
            {
                IsHost.Value = isLocalPlayerHost;
            };
        }
    }
}