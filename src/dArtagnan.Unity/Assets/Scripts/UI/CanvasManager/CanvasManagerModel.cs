using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.CanvasManager
{
    [CreateAssetMenu(fileName = "CanvasManagerModel", menuName = "d'Artagnan/Canvas Manager Model ", order = 0)]
    public class CanvasManagerModel : ScriptableObject
    {
        public SerializableReactiveProperty<GameScreen> screen;

        private void OnEnable()
        {
            PacketChannel.On<WaitingStartFromServer>(_ => screen.Value = GameScreen.HUD);
            PacketChannel.On<RoundStartFromServer>(_ => screen.Value = GameScreen.HUD);
            PacketChannel.On<YourAccuracyAndPool>(_ => screen.Value = GameScreen.Roulette);
            PacketChannel.On<AugmentStartFromServer>(_ => screen.Value = GameScreen.AugmentationSelection);
            screen.Value = GameScreen.Connection;
        }
    }
}