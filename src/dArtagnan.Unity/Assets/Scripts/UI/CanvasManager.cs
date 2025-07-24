using System;
using dArtagnan.Shared;
using Game;
using UnityEngine;

public class CanvasManager : MonoBehaviour, IChannelListener
{
    [SerializeField] private Canvas HUD;
    [SerializeField] private Canvas Roulette;
    [SerializeField] private Canvas Connection;
    [SerializeField] private Canvas AugmentationSelection;

    public void Initialize()
    {
        PacketChannel.On<WaitingStartFromServer>(_ => Show(GameScreen.HUD, true));
        PacketChannel.On<RoundStartFromServer>(_ => Show(GameScreen.HUD, true));
        PacketChannel.On<YourAccuracyAndPool>(_ => Show(GameScreen.Roulette, true));
        PacketChannel.On<AugmentStartFromServer>(_ => Show(GameScreen.AugmentationSelection, true));
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
            case GameScreen.AugmentationSelection:
                AugmentationSelection.gameObject.SetActive(true);
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
            case GameScreen.AugmentationSelection:
                AugmentationSelection.gameObject.SetActive(false);
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