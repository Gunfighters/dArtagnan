using dArtagnan.Shared;
using Game;
using R3;

namespace UI.CanvasManager
{
    public class CanvasManagerModel
    {
        public readonly ReactiveProperty<GameScreen> Screen = new();

        public CanvasManagerModel()
        {
            LocalEventChannel.OnConnectionFailure += () => Screen.Value = GameScreen.NetworkFailure;
            LocalEventChannel.BackToConnection += () => Screen.Value = GameScreen.Connection;
            PacketChannel.On<WaitingStartFromServer>(_ => Screen.Value = GameScreen.HUD);
            PacketChannel.On<RoundStartFromServer>(_ => Screen.Value = GameScreen.HUD);
            PacketChannel.On<ShowdownStartFromServer>(_ => Screen.Value = GameScreen.ShowdownLoading);
            PacketChannel.On<AugmentStartFromServer>(_ => Screen.Value = GameScreen.AugmentationSelection);
        }
    }
}