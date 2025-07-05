using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

public class GameManager : MonoBehaviour
{
    public Camera mainCamera;
    public AudioClip BGMInGame;
    public AudioClip BGMWaiting;
    public AudioSource BGMPlayer;
    readonly Dictionary<int, Player> players = new();
    private int localPlayerId;
    [CanBeNull] public Player LocalPlayer
    {
        get
        {
            players.TryGetValue(localPlayerId, out var player);
            return player;
        }
    }

    public GameObject playerPrefab;
    public float Ping { get; private set; }
    public static GameManager Instance { get; private set; }
    public Canvas WorldCanvas;
    public Button GameStartButton;
    private int hostId;
    [CanBeNull] public Player Host => players[hostId];
    public TextMeshProUGUI winnerAnnouncement;
    public GameState GameState { get; private set; }
    public VariableJoystick movementJoystick;
    public ShootJoystickController shootJoystickController;
    [CanBeNull] private Player LastSentTarget;

    private void Awake()
    {
        Instance = this;
        ToggleUIOverHeadEveryone(false);
        movementJoystick.gameObject.SetActive(false);
        shootJoystickController.gameObject.SetActive(false);
        GameStartButton.onClick.AddListener(StartGame);
    }

    private void Start()
    {
        NetworkManager.Instance.SendJoinRequest();
        BGMPlayer.PlayOneShot(BGMWaiting);
    }

    private void AddPlayer(PlayerInformation info, bool InGame)
    {
        Debug.Log($"Add Player #{info.PlayerId}");
        if (players.ContainsKey(info.PlayerId))
        {
            Debug.LogWarning($"Trying to add player #{info.PlayerId} that already exists");
            return;
        }
        var player = Instantiate(playerPrefab, WorldCanvas.transform).GetComponent<Player>();
        var directionVec = DirectionHelper.IntToDirection(info.MovementData.Direction);
        var estimatedPosition = info.MovementData.Position + Ping / 2 * directionVec;
        var estimatedRemainingReloadTime = info.RemainingReloadTime - Ping / 2;
        info.MovementData.Position = estimatedPosition;
        info.RemainingReloadTime = estimatedRemainingReloadTime;
        player.Initialize(info);
        players[info.PlayerId] = player;
        if (player == LocalPlayer)
        {
            mainCamera.transform.SetParent(player.transform);
            movementJoystick.gameObject.SetActive(true);
            shootJoystickController.gameObject.SetActive(true);
        }
        player.ToggleUIOverHead(InGame);
        player.gameObject.layer = LayerMask.NameToLayer(player == LocalPlayer ? "LocalPlayer" : "RemotePlayer");
        player.gameObject.SetActive(true);
    }

    public void OnYouAre(YouAre payload)
    {
        localPlayerId = payload.PlayerId;
        ToggleGameStartButton(localPlayerId == hostId);
    }

    public void OnNewHost(NewHost payload)
    {
        hostId = payload.HostId;
        Debug.Log($"New Host #{hostId}");
        ToggleGameStartButton(localPlayerId == hostId);
    }

    void ToggleGameStartButton(bool toggle)
    {
        GameStartButton.gameObject.SetActive(toggle);
    }

    public void OnInformationOfPlayers(InformationOfPlayers informationOfPlayers)
    {
        foreach (var info in informationOfPlayers.Info)
        {
            AddPlayer(info, informationOfPlayers.InGame);
        }
    }

    public void OnPlayerJoinBroadcast(PlayerJoinBroadcast payload)
    {
        var info = payload.PlayerInfo;
        if (info.PlayerId != localPlayerId)
        {
            AddPlayer(info, false);
        }
    }

    public void OnPlayerMovementData(PlayerMovementDataBroadcast payload)
    {
        var targetPlayer = players[payload.PlayerId];
        if (targetPlayer == LocalPlayer) return;
        var direction = DirectionHelperClient.IntToDirection(payload.MovementData.Direction);
        var serverPosition = VecConverter.ToUnityVec(payload.MovementData.Position);
        var position = EstimatePositionByPing(serverPosition, direction, payload.MovementData.Speed);
        targetPlayer.UpdateMovementDataForReckoning(direction, position, payload.MovementData.Speed);
    }

    public void OnPlayerShootingBroadcast(PlayerShootingBroadcast shooting)
    {
        var shooter = players[shooting.ShooterId];
        var target = players[shooting.TargetId];
        shooter.Fire(target);
        shooter.UpdateRemainingReloadTime(shooter.TotalReloadTime - Ping / 2);
        shooter.ShowHitOrMiss(shooting.Hit);
    }

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        var player = players[updatePlayerAlive.PlayerId];
        player.SetAlive(updatePlayerAlive.Alive);
        if (!player.Alive && player == LocalPlayer)
        {
            mainCamera.transform.SetParent(players.Values.FirstOrDefault(p => p.Alive).transform);
        }
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        var leaving = players[leave.PlayerId];
        leaving.gameObject.SetActive(false);
        leaving.Reset();
        players.Remove(leave.PlayerId);
    }

    public void OnGameStarted(GameStarted gameStarted)
    {
        BGMPlayer.Stop();
        BGMPlayer.PlayOneShot(BGMInGame);
        foreach (var info in gameStarted.Players)
        {
            var player = players[info.PlayerId];
            player.Initialize(info);
            player.gameObject.SetActive(true);
        }
    }

    public void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
    {
        var aiming = players[playerIsTargeting.ShooterId];
        if (aiming == LocalPlayer) return;
        players.TryGetValue(playerIsTargeting.TargetId, out var target);
        aiming.Aim(target);
    }

    public void OnWinner(Winner winner)
    {
        var winning = players[winner.PlayerId];
        winnerAnnouncement.text = $"{winning.Nickname} HAS WON!";
        winnerAnnouncement.gameObject.SetActive(true);
    }

    private void ToggleUIOverHeadEveryone(bool toggle)
    {
        foreach (var p in players.Values)
        {
            p.ToggleUIOverHead(toggle);
        }
    }

    public void OnNewGameState(NewGameState newGameState)
    {
        GameState = newGameState.GameState;
        switch (GameState)
        {
            case GameState.Waiting:
                winnerAnnouncement.gameObject.SetActive(false);
                ToggleUIOverHeadEveryone(false);
                foreach (var p in players.Values)
                {
                    p.gameObject.SetActive(true);
                    p.SetAlive(true);
                    if (p == LocalPlayer)
                    {
                        mainCamera.transform.SetParent(p.transform);
                    }
                }
                break;
            case GameState.Playing:
                ToggleUIOverHeadEveryone(true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetPing(Ping p)
    {
        Ping = p.time / 1000f;
    }

    private void StartGame()
    {
        NetworkManager.Instance.SendStartGame();
    }

    private void Update()
    {
        HandleMovementInputAndUpdateOnChange();
        UpdateLocalPlayerTarget();
    }

    private void HandleMovementInputAndUpdateOnChange()
    {
        if (!LocalPlayerActive()) return;
        var inputDirection = GetInputVector();
        var needToUpdate = LocalPlayer!.CurrentDirection != inputDirection;
        needToUpdate |= Input.GetKeyDown(KeyCode.Space) | Input.GetKeyUp(KeyCode.Space);
        var nowRunning = Input.GetKey(KeyCode.Space) || IsMovementJoystickMoving(); // always run if using joystick
        LocalPlayer.SetDirection(inputDirection);
        LocalPlayer.SetSpeed(nowRunning ? Constants.RUNNING_SPEED : Constants.WALKING_SPEED);

        if (needToUpdate)
        {
            NetworkManager.Instance.SendPlayerMovementData(LocalPlayer.Position, LocalPlayer.CurrentDirection, nowRunning);
        }
    }

    private Vector2 GetInputVector()
    {
        if (IsMovementJoystickMoving())
        {
            return DirectionHelperClient.IntToDirection(DirectionHelperClient.DirectionToInt(movementJoystick.Direction));
        }
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

    private bool LocalPlayerActive()
    {
        return LocalPlayer && LocalPlayer.gameObject.activeInHierarchy && LocalPlayer.Alive;
    }

    private void UpdateLocalPlayerTarget()
    {
        if (!LocalPlayerActive()) return;
        var newTarget = GetAutoTarget();
        var changed = LocalPlayer?.TargetPlayer != newTarget;
        if (changed)
        {
            LocalPlayer!.TargetPlayer?.HighlightAsTarget(false);
            LocalPlayer.TargetPlayer = newTarget;
            LocalPlayer.TargetPlayer?.HighlightAsTarget(true);
        }

        if (!IsShootJoystickMoving()) return;
        if ((LastSentTarget is null && newTarget is not null)
            || (newTarget is null && LastSentTarget is not null)
            || changed)
        {
            LocalPlayer.Aim(newTarget);
            NetworkManager.Instance.SendPlayerNewTarget(newTarget?.ID ?? -1);
            LastSentTarget = newTarget;
        }
    }

    private bool IsMovementJoystickMoving()
    {
        return movementJoystick.Direction != Vector2.zero;
    }

    private bool IsShootJoystickMoving()
    {
        return shootJoystickController.Direction != Vector2.zero;
    }
    
    private Player GetAutoTarget()
    {
        Player best = null;
        var targetPool =
            players.Values.Where(target =>
                target != LocalPlayer
                && target.Alive
                && LocalPlayer!.CanShoot(target));
        if (!IsShootJoystickMoving()) // 사거리 내 가장 가까운 적.
        {
            var minDistance = LocalPlayer.Range;
            foreach (var target in targetPool)
            {
                if (Vector2.Distance(target.Position, LocalPlayer.Position) < minDistance)
                {
                    minDistance = Vector2.Distance(target.Position, LocalPlayer.Position);
                    best = target;
                }
            }

            return best;
        }

        var minAngle = float.MaxValue;
        foreach (var target in targetPool)
        {
            if (Vector2.Angle(shootJoystickController.Direction, target.Position - LocalPlayer.Position) < minAngle
                && LocalPlayer.CanShoot(target)
               )
            {
                minAngle = Vector2.Angle(shootJoystickController.Direction, target.Position - LocalPlayer.Position);
                best = target;
            }
        }
        return best;
    }

    public void ShootTarget()
    {
        if (!LocalPlayer?.TargetPlayer) return;
        NetworkManager.Instance.SendPlayerShooting(LocalPlayer!.TargetPlayer!.ID);
    }
    
    private Vector2 EstimatePositionByPing(Vector2 position, Vector2 direction, float speed)
    {
        return position + Ping / 2 * direction * speed;
    }
}