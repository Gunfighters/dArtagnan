using dArtagnan.Shared;
using Game;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip BGMInGame;
    public AudioClip BGMWaiting;
    public AudioSource BGMPlayer;

    public void Awake()
    {
        PacketChannel.On<GameInPlayingFromServer>(e => PlayForState(GameState.Playing));
        PacketChannel.On<GameInWaitingFromServer>(e => PlayForState(GameState.Waiting));
    }

    private void PlayForState(GameState state)
    {
        BGMPlayer.clip = state switch
        {
            GameState.Playing => BGMInGame,
            GameState.Waiting => BGMWaiting,
            _ => BGMPlayer.clip
        };
        BGMPlayer.Play();
    }
}