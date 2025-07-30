using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    public class GameStateManager : MonoBehaviour, IChannelListener
    {
        public static GameState GameState { get; private set; }

        public void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(OnGamePlaying);
            PacketChannel.On<WaitingStartFromServer>(OnGameWaiting);
            PacketChannel.On<AccuracySelectionStartFromServer>(OnAccuracySelection);
            ;
        }

        private static void OnGamePlaying(RoundStartFromServer e)
        {
            GameState = GameState.Round;
        }

        private static void OnGameWaiting(WaitingStartFromServer e)
        {
            GameState = GameState.Waiting;
        }

        private static void OnAccuracySelection(AccuracySelectionStartFromServer e)
        {
            GameState = GameState.AccuracySelection;
        }
    }
}