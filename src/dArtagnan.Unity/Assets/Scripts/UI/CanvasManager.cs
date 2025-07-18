using System;
using dArtagnan.Shared;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }
    [SerializeField] private Canvas HUD;
    [SerializeField] private Canvas Roulette;
    [SerializeField] private Canvas Connection;

    private void Awake()
    {
        Instance = this;
        Init(GameScreen.HUD);
        Init(GameScreen.Roulette);
        Show(GameScreen.Connection, true);
    }

    private void OnEnable()
    {
        PacketChannel.On<GameInWaitingFromServer>(e => Show(GameScreen.HUD, true));
        PacketChannel.On<GameInPlayingFromServer>(e => Show(GameScreen.HUD, true));
        PacketChannel.On<YourAccuracyAndPool>(e => Show(GameScreen.Roulette, true));
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

    public void Hide(GameScreen screen)
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

    private void Init(GameScreen screen)
    {
        switch (screen)
        {
            case GameScreen.HUD:
                HUD.enabled = false;
                HUD.gameObject.SetActive(true);
                HUD.gameObject.SetActive(false);
                HUD.enabled = true;
                break;
            case GameScreen.Roulette:
                Roulette.enabled = false;
                Roulette.gameObject.SetActive(true);
                Roulette.gameObject.SetActive(false);
                Roulette.enabled = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
        }
    }

    public void HideAll()
    {
        foreach (var c in Enum.GetValues(typeof(GameScreen)))
        {
            Hide((GameScreen) c);
        }
    }
}