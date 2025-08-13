using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameState GameState { get; private set; }

        private void Awake()
        {
            PacketChannel.On<RoundStartFromServer>(OnGamePlaying);
            PacketChannel.On<WaitingStartFromServer>(OnGameWaiting);
            PacketChannel.On<RouletteStartFromServer>(OnYourAccuracyAndPool);
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

        private static void OnYourAccuracyAndPool(RouletteStartFromServer e)
        {
            GameState = GameState.Roulette;
        }
    }
}