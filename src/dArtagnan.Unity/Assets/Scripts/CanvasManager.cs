using System;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }
    [SerializeField] private Canvas HUD;
    [SerializeField] private Canvas Roulette;

    private void Awake()
    {
        Instance = this;
    }

    public void Show(GameScreen screen)
    {
        switch (screen)
        {
            case GameScreen.HUD:
                HUD.enabled = true;
                break;
            case GameScreen.Roulette:
                Roulette.enabled = true;
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
                HUD.enabled = false;
                break;
            case GameScreen.Roulette:
                Roulette.enabled = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
        }
    }
}