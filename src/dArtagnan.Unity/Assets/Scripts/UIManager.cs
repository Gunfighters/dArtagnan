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
    private MovementJoystick _movementJoystick;
    private ShootJoystickController _shootJoystickController;
    private Vector2 _lastDirection;
    private bool _lastRunning;

    private void Awake()
    {
        Instance = this;
        _movementJoystick = GetComponentInChildren<MovementJoystick>();
        _shootJoystickController = GetComponentInChildren<ShootJoystickController>();
        _movementJoystick.gameObject.SetActive(false);
        _shootJoystickController.gameObject.SetActive(false);
        gameStartButton.gameObject.SetActive(false);
        winnerAnnouncement.gameObject.SetActive(false);
    }

    private void Start()
    {
        SetupForGameState(GameState.Waiting);
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
        var dir = _movementJoystick.IsMoving ? _movementJoystick.InputVectorSnapped : GetKeyboardVector();
        return dir;
    }

    private bool GetInputRunning()
    {
        return _movementJoystick.IsMoving || Input.GetKey(KeyCode.Space);
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
        _movementJoystick.gameObject.SetActive(true);
        _shootJoystickController.LocalPlayer = localPlayer;
        _shootJoystickController.gameObject.SetActive(true);
    }
    
    public void OnNewHost(bool youAreHost)
    {
        gameStartButton.gameObject.SetActive(youAreHost);
    }

    public void AnnounceWinner(Player winner)
    {
        winnerAnnouncement.text = $"{winner.Nickname} HAS WON!";
        winnerAnnouncement.gameObject.SetActive(true);
    }

    public void SetupForGameState(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Waiting:
                gameStartButton.gameObject.SetActive(GameManager.Instance.LocalPlayer == GameManager.Instance.Host);
                winnerAnnouncement.gameObject.SetActive(false);
                break;
            case GameState.Playing:
                gameStartButton.gameObject.SetActive(false);
                break;
        }
    }

    public Vector2 ShootJoystickVector()
    {
        return _shootJoystickController.Direction;
    }
}