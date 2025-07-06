using dArtagnan.Shared;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip BGMInGame;
    public AudioClip BGMWaiting;
    public AudioSource BGMPlayer;

    private void Awake()
    {
        BGMPlayer.loop = true;
    }

    public void PlayForState(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                BGMPlayer.clip = BGMInGame;
                break;
            case GameState.Waiting:
                BGMPlayer.clip = BGMWaiting;
                break;
        }
        BGMPlayer.Play();
    }
}