using System.Collections;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public IMinimapManager MinimapManager;
    public Button gameStartButton;
    public TextMeshProUGUI winnerAnnouncement;
    public TextMeshProUGUI gameStartSplash;
    public TextMeshProUGUI roundBoard;
    public TextMeshProUGUI roundSplash;
    public GameObject spectatingText;
    public MovementJoystick movementJoystick;
    public ShootJoystickController ShootJoystickController;
    private Vector2 _lastDirection;
    private bool _lastRunning;
    public float gameStartSplashDuration;
    public float winnerAnnouncementDuration;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetupForGameState(new GameInWaitingFromServer());
    }

    private void Update()
    {
        var newDirection = GetInputDirection();
        var newRunning = GetInputRunning();
        if (_lastDirection == newDirection && _lastRunning == newRunning) return;
        _lastDirection = newDirection;
        _lastRunning = newRunning;
        GameManager.Instance.UpdateVelocity(newDirection, newRunning);
    }
    
    private Vector2 GetInputDirection()
    {
        var dir = movementJoystick.IsMoving ? movementJoystick.InputVectorSnapped : GetKeyboardVector();
        return dir;
    }

    private bool GetInputRunning()
    {
        return movementJoystick.IsMoving || Input.GetKey(KeyCode.Space);
    }

    private Vector2 GetKeyboardVector()
    {
        var direction = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector2.up;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector2.down;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector2.left;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector2.right;
        }

        return direction.normalized;
    }

    public void OnLocalPlayerActivation(Player localPlayer)
    {
        movementJoystick.gameObject.SetActive(true);
        ShootJoystickController.LocalPlayer = localPlayer;
        ShootJoystickController.gameObject.SetActive(true);
    }
    
    public void OnNewHost(bool youAreHost)
    {
        gameStartButton.gameObject.SetActive(youAreHost);
    }

    public void AnnounceWinner(Player winner)
    {
        winnerAnnouncement.text = $"{winner.Nickname} HAS WON!";
        winnerAnnouncement.gameObject.SetActive(true);
        ScheduleDisappear(winnerAnnouncement.gameObject, winnerAnnouncementDuration);
    }

    public void SetupForGameState(GameInWaitingFromServer waiting)
    {
        gameStartButton.gameObject.SetActive(GameManager.Instance.LocalPlayer == GameManager.Instance.Host);
        roundBoard.gameObject.SetActive(false);
        roundSplash.gameObject.SetActive(false);
    }

    public void SetupForGameState(GameInPlayingFromServer playing)
    {
        gameStartButton.gameObject.SetActive(false);
        roundBoard.text = $"Round {playing.Round} / 4";
        if (playing.Round == 1)
        {
            gameStartSplash.gameObject.SetActive(true);
            ScheduleDisappear(gameStartSplash.gameObject, gameStartSplashDuration);
        }
        else
        {
            roundSplash.text = $"Round #{playing.Round}";
            roundSplash.gameObject.SetActive(true);
            ScheduleDisappear(roundSplash.gameObject, 1.5f);
        }
    }

    public Vector2 ShootJoystickVector()
    {
        return ShootJoystickController.Direction;
    }

    private void ScheduleDisappear(GameObject obj, float delay)
    {
        StartCoroutine(Delay(obj, delay));
    }

    IEnumerator Delay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    public void ToggleSpectate(bool toggle)
    {
        spectatingText.gameObject.SetActive(toggle);
    }
}