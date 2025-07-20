using System;
using dArtagnan.Shared;
using Game;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private Canvas HUD;
    [SerializeField] private Canvas Roulette;
    [SerializeField] private Canvas Connection;

    public void Awake()
    {
        PacketChannel.On<WaitingStartFromServer>(e => Show(GameScreen.HUD, true));
        PacketChannel.On<RoundStartFromServer>(e => Show(GameScreen.HUD, true));
        PacketChannel.On<YourAccuracyAndPool>(e => Show(GameScreen.Roulette, true));
        LocalEventChannel.OnEndpointSelected += (_, _) => Hide(GameScreen.Connection);
    }

    private void Show(GameScreen screen, bool hideAll)
    {
        if (hideAll)
            HideAll();
        switch (screen)
        {
            case GameScreen.HUD:
                HUD.gameObject.SetActive(true);
                break;
            case GameScreen.Roulette:
                Roulette.gameObject.SetActive(true);
                break;
            case GameScreen.Connection:
                Connection.gameObject.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
        }
    }

    private void Hide(GameScreen screen)
    {
        switch (screen)
        {
            case GameScreen.HUD:
                HUD.gameObject.SetActive(false);
                break;
            case GameScreen.Roulette:
                Roulette.gameObject.SetActive(false);
                break;
            case GameScreen.Connection:
                Connection.gameObject.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
        }
    }

    private void HideAll()
    {
        foreach (var c in Enum.GetValues(typeof(GameScreen)))
        {
            Hide((GameScreen) c);
        }
    }
}