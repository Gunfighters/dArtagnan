using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD.Playing
{
    public class AccuracyStateWheelModel
    {
        public readonly ReactiveProperty<int> State = new();

        public AccuracyStateWheelModel()
        {
            GameService.State.Subscribe(state =>
            {
                if (state == GameState.Round)
                {
                    State.Value = GameService.LocalPlayer.AccuracyState.Value;
                }
            });
            GameService.LocalPlayerSet.Subscribe(player => player.AccuracyState.Subscribe(v => State.Value = v));
        }
    }
}