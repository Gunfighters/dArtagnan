using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    public class StateManager : MonoBehaviour
    {
        public GameState GameState { get; private set; }
        private void OnEnable()
        {
            PacketChannel.On<GameInPlayingFromServer>(OnGamePlaying);
            PacketChannel.On<GameInWaitingFromServer>(OnGameWaiting);
            PacketChannel.On<YourAccuracyAndPool>(OnYourAccuracyAndPool);;
        }

        private void OnGamePlaying(GameInPlayingFromServer e)
        {
            GameState = GameState.Playing;
        }

        private void OnGameWaiting(GameInWaitingFromServer e)
        {
            GameState = GameState.Waiting;
        }

        private void OnYourAccuracyAndPool(YourAccuracyAndPool e)
        {
            GameState = GameState.RouletteSpinning;
        }
    }
}