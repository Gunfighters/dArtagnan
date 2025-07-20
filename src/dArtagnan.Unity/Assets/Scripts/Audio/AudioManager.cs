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
        PacketChannel.On<RoundStartFromServer>(e => PlayForState(GameState.Round));
        PacketChannel.On<WaitingStartFromServer>(e => PlayForState(GameState.Waiting));
    }

    private void PlayForState(GameState state)
    {
        BGMPlayer.clip = state switch
        {
            GameState.Round => BGMInGame,
            GameState.Waiting => BGMWaiting,
            _ => BGMPlayer.clip
        };
        BGMPlayer.Play();
    }
}