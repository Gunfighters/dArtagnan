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
                HUD.gameObject.SetActive(true);
                break;
            case GameScreen.Roulette:
                Roulette.gameObject.SetActive(true);
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
            default:
                throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
        }
    }
}