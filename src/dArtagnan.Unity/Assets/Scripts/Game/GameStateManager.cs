using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameState GameState { get; private set; }
        public void Awake()
        {
            PacketChannel.On<GameInPlayingFromServer>(OnGamePlaying);
            PacketChannel.On<GameInWaitingFromServer>(OnGameWaiting);
            PacketChannel.On<YourAccuracyAndPool>(OnYourAccuracyAndPool);;
        }

        private static void OnGamePlaying(GameInPlayingFromServer e)
        {
            GameState = GameState.Playing;
        }

        private static void OnGameWaiting(GameInWaitingFromServer e)
        {
            GameState = GameState.Waiting;
        }

        private static void OnYourAccuracyAndPool(YourAccuracyAndPool e)
        {
            GameState = GameState.RouletteSpinning;
        }
    }
}