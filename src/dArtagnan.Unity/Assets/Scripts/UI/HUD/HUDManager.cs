using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    public Button gameStartButton;
    public TextMeshProUGUI winnerAnnouncement;
    public TextMeshProUGUI gameStartSplash;
    public TextMeshProUGUI roundBoard;
    public TextMeshProUGUI roundSplash;
    public AccuracyStateTabMenuController accuracyStateTabMenuController;
    public GameObject spectatingText;
    public MovementJoystick movementJoystick;
    public ShootJoystickController shootJoystickController;
    public Canvas onlyWhenLocalPlayerAlive;
    private Vector2 _lastDirection;
    private bool _lastRunning;
    public float gameStartSplashDuration;
    public float winnerAnnouncementDuration;
    private float Speed = 2;

    private void Awake()
    {
        Instance = this;
        PacketChannel.On<GameInPlayingFromServer>(SetupForGameState);
        PacketChannel.On<GameInWaitingFromServer>(SetupForGameState);
        PacketChannel.On<WinnerBroadcast>(e => AnnounceWinner(PlayerGeneralManager.GetPlayer(e.PlayerId)));
        LocalEventChannel.OnLocalPlayerAlive += alive => ToggleSpectate(!alive);
    }

    private void Update()
    {
        var newDirection = GetInputDirection();
        var newRunning = GetInputRunning();
        if (_lastDirection == newDirection && _lastRunning == newRunning) return;
        _lastDirection = newDirection;
        _lastRunning = newRunning;
        UpdateVelocity(newDirection, newRunning, Speed);
    }

    public void UpdateSpeed(float speed)
    {
        Speed = speed;
        UpdateVelocity(GetInputDirection(), GetInputRunning(), speed);
    }
    
    public void UpdateVelocity(Vector2 newDirection, bool running, float speed)
    {
        var localPlayer = PlayerGeneralManager.LocalPlayer;
        if (!localPlayer.Alive) return;
        localPlayer.SetDirection(newDirection);
        localPlayer.SetRunning(running);
        localPlayer.SetSpeed(speed);
        SendLocalPlayerMovementData();
    }
    
    public void SendLocalPlayerMovementData()
    {
        var localPlayer = PlayerGeneralManager.LocalPlayer;
        PacketChannel.Raise(new PlayerMovementDataFromClient
        {
            Direction = DirectionHelperClient.DirectionToInt(localPlayer.CurrentDirection),
            MovementData = new MovementData
            {
                Direction = DirectionHelperClient.DirectionToInt(localPlayer.CurrentDirection),
                Position = VecConverter.ToSystemVec(localPlayer.Position),
                Speed = localPlayer.Speed
            }
        });
    }
    
    public void UpdateRange(float range)
    {
        PlayerGeneralManager.LocalPlayer.SetRange(range);
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

    private void AnnounceWinner(Player winner)
    {
        Debug.Log($"Announcing winner: {winner.Nickname}");
        winnerAnnouncement.text = $"{winner.Nickname} HAS WON!";
        winnerAnnouncement.transform.parent.gameObject.SetActive(true);
        DelayDeactivation(winnerAnnouncement.transform.parent.gameObject, winnerAnnouncementDuration).Forget();
    }

    private void SetupForGameState(GameInWaitingFromServer waiting)
    {
        gameStartButton.gameObject.SetActive(
            PlayerGeneralManager.LocalPlayer is not null
            && PlayerGeneralManager.LocalPlayer == PlayerGeneralManager.HostPlayer);
        roundBoard.gameObject.SetActive(false);
        roundSplash.gameObject.SetActive(false);
        accuracyStateTabMenuController.gameObject.SetActive(false);
        onlyWhenLocalPlayerAlive.enabled = true;
    }

    private void SetupForGameState(GameInPlayingFromServer playing)
    {
        gameStartButton.gameObject.SetActive(false);
        accuracyStateTabMenuController.gameObject.SetActive(true);
        roundBoard.text = $"Round #{playing.Round}";
        if (playing.Round == 1)
        {
            gameStartSplash.gameObject.SetActive(true);
            DelayDeactivation(gameStartSplash.gameObject, gameStartSplashDuration).Forget();
        }
        else
        {
            roundSplash.text = $"Round #{playing.Round}";
            roundSplash.gameObject.SetActive(true);
            DelayDeactivation(roundSplash.gameObject, 1.5f).Forget();
        }
    }

    public Vector2 ShootJoystickVector()
    {
        return shootJoystickController.Direction;
    }

    private async UniTask DelayDeactivation(GameObject obj, float delay)
    {
        await UniTask.WaitForSeconds(delay);
        obj.SetActive(false);
    }

    private void ToggleSpectate(bool toggle)
    {
        spectatingText.gameObject.SetActive(toggle);
        onlyWhenLocalPlayerAlive.enabled = !toggle;
    }
}