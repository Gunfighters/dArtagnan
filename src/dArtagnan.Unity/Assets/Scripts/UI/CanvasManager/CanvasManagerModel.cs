using dArtagnan.Shared;
using Game;
using R3;
using UnityEditor;

namespace UI.CanvasManager
{
    [InitializeOnLoad]
    public static class CanvasManagerModel
    {
        public static readonly ReactiveProperty<GameScreen> Screen = new();

        static CanvasManagerModel()
        {
            PacketChannel.On<WaitingStartFromServer>(_ => Screen.Value = GameScreen.HUD);
            PacketChannel.On<RoundStartFromServer>(_ => Screen.Value = GameScreen.HUD);
            PacketChannel.On<YourAccuracyAndPool>(_ => Screen.Value = GameScreen.Roulette);
            PacketChannel.On<AugmentStartFromServer>(_ => Screen.Value = GameScreen.AugmentationSelection);
            Screen.Value = GameScreen.Connection;
        }
    }
}