using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD
{
    public class HUDModel
    {
        public readonly ReactiveProperty<bool> Controlling = new();
        public readonly ReactiveProperty<bool> InRound = new();
        public readonly ReactiveProperty<bool> IsHost = new();
        public readonly ReactiveProperty<bool> Playing = new();
        public readonly ReactiveProperty<bool> Spectating = new();
        public readonly ReactiveProperty<bool> Waiting = new();

        public HUDModel()
        {
            PacketChannel.On<RoundStartFromServer>(_ =>
            {
                InRound.Value = true;
                Waiting.Value = false;
                Playing.Value = GameService.LocalPlayer.Health.Alive.CurrentValue;
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