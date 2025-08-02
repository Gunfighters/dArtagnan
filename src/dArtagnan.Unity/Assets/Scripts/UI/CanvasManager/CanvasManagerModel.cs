using dArtagnan.Shared;
using Game;
using R3;
using UnityEditor;
using UnityEngine;

namespace UI.CanvasManager
{
    public static class CanvasManagerModel
    {
        public static readonly ReactiveProperty<GameScreen> Screen = new();

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            LocalEventChannel.OnConnectionFailure += () => Screen.Value = GameScreen.NetworkFailure;
            LocalEventChannel.BackToConnection += () => Screen.Value = GameScreen.Connection;
            PacketChannel.On<WaitingStartFromServer>(_ => Screen.Value = GameScreen.HUD);
            PacketChannel.On<RoundStartFromServer>(_ => Screen.Value = GameScreen.HUD);
            PacketChannel.On<YourAccuracyAndPool>(_ => Screen.Value = GameScreen.Roulette);
            PacketChannel.On<AugmentStartFromServer>(_ => Screen.Value = GameScreen.AugmentationSelection);
            Screen.Value = GameScreen.Connection;
        }
    }
}