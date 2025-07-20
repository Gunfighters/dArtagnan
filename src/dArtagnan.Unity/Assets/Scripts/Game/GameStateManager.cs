using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameState GameState { get; private set; }
        public void Awake()
        {
            PacketChannel.On<RoundStartFromServer>(OnGamePlaying);
            PacketChannel.On<WaitingStartFromServer>(OnGameWaiting);
            PacketChannel.On<YourAccuracyAndPool>(OnYourAccuracyAndPool);;
        }

        private static void OnGamePlaying(RoundStartFromServer e)
        {
            GameState = GameState.Round;
        }

        private static void OnGameWaiting(WaitingStartFromServer e)
        {
            GameState = GameState.Waiting;
        }

        private static void OnYourAccuracyAndPool(YourAccuracyAndPool e)
        {
            GameState = GameState.Roulette;
        }
    }
}