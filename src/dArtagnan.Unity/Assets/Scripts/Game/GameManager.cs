using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using dArtagnan.Shared;
using JetBrains.Annotations;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public PlayerManager playerManager;
    private readonly Dictionary<int, Player> players = new();
    public List<Player> Survivors => players.Values.Where(p => p.Alive).ToList();
    private int localPlayerId;
    [CanBeNull] public Player LocalPlayer => players.GetValueOrDefault(localPlayerId, null);
    public CameraController mainCamera;
    public GameObject Field;
    public AudioManager AudioManager;
    private int hostId;
    [CanBeNull] public Player Host => players.GetValueOrDefault(hostId, null);
    private GameState gameState;
    private float lastMovementDataUpdateTimestmap;
    private CancellationTokenSource _deactivationTaskCancellationTokenSource = new();
    private float ping;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Instance = this;
    }

    private void Update()
    {
        if (Time.time - lastMovementDataUpdateTimestmap >= 1f && LocalPlayer?.Alive is true)
        {
            NetworkManager.Instance.SendPlayerMovementData(LocalPlayer.Position, LocalPlayer.CurrentDirection, LocalPlayer.Running, LocalPlayer.Speed);
            lastMovementDataUpdateTimestmap = Time.time;
        }
    }

    private void AddPlayer(PlayerInformation info, bool inGame)
    {
        var p = playerManager.CreatePlayer(info);
        p.Initialize(info);
        players[info.PlayerId] = p;
        
        if (p == LocalPlayer)
        {
            CanvasManager.Instance.Show(GameScreen.HUD);
            SetCameraFollow(p);
            HUDManager.Instance.OnLocalPlayerActivation(p);
        }
        p.ToggleUIInGame(inGame);
        p.gameObject.layer = LayerMask.NameToLayer(p == LocalPlayer ? "LocalPlayer" : "RemotePlayer");
    }

    public void OnYouAre(YouAre payload)
    {
        localPlayerId = payload.PlayerId;
        HUDManager.Instance.OnNewHost(localPlayerId == hostId);
    }

    public void OnNewHost(NewHostBroadcast payload)
    {
        hostId = payload.HostId;
        HUDManager.Instance.OnNewHost(localPlayerId == hostId);
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
        targetPlayer.UpdateMovementDataForReckoning(direction, serverPosition, payload.MovementData.Speed);
        targetPlayer.SetRunning(payload.Running);
    }

    public void OnPlayerShootingBroadcast(PlayerShootingBroadcast shooting)
    {
        var shooter = players[shooting.ShooterId];
        var target = players[shooting.TargetId];
        shooter.Fire(target);
        shooter.UpdateRemainingReloadTime(shooter.TotalReloadTime);
        if (gameState == GameState.Playing)
        {
            shooter.ShowHitOrMiss(shooting.Hit);
        }
    }

    public void UpdatePing(float ping)
    {
        this.ping = ping;
    }

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        var player = players[updatePlayerAlive.PlayerId];
        player.SetAlive(updatePlayerAlive.Alive);
        if (!player.Alive)
        {
            ScheduleDeactivation(player).Forget();
            if (player.transform == mainCamera.target)
            {
                var anotherPlayer = players.Values.FirstOrDefault(p => p.Alive);
                SetCameraFollow(anotherPlayer);
            }
        }
    }

    private async UniTask ScheduleDeactivation(Player p)
    {
        try
        {
            await UniTask.WaitForSeconds(1.5f, cancellationToken: _deactivationTaskCancellationTokenSource.Token);
            p.gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Deactivation of player cancelled.");
        }
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        if (players.TryGetValue(leave.PlayerId, out var leavingPlayer))
        {
            playerManager.RemovePlayer(leave.PlayerId);
            players.Remove(leave.PlayerId);
            Debug.Log($"Player #{leave.PlayerId} was successfully removed.");
        }
        else
        {
            Debug.LogWarning($"Tried to remove player #{leave.PlayerId}, but they were not found in the dictionary. They might have been cleared already.");
        }
    }

    public void OnGamePlaying(GameInPlayingFromServer gamePlaying)
    {
        CancelAllTasks();
        CanvasManager.Instance.Hide(GameScreen.Roulette);
        CanvasManager.Instance.Show(GameScreen.HUD);
        gameState = GameState.Playing;
        HUDManager.Instance.SetupForGameState(gamePlaying);
        AudioManager.PlayForState(GameState.Playing);
        RemovePlayerAll();
        foreach (var info in gamePlaying.PlayersInfo)
        {
            AddPlayer(info, true);
        }
        
        HUDManager.Instance.accuracyStateTabMenuController.SwitchUIOnly(LocalPlayer.AccuracyState);
    }

    public void OnGameWaiting(GameInWaitingFromServer gameWaiting)
    {
        CancelAllTasks();
        gameState = GameState.Waiting;
        RemovePlayerAll();
        foreach (var info in gameWaiting.PlayersInfo)
        {
            AddPlayer(info, false);
        }
        HUDManager.Instance.SetupForGameState(gameWaiting);
        AudioManager.PlayForState(GameState.Waiting);
    }

    private void CancelAllTasks()
    {
        _deactivationTaskCancellationTokenSource.Cancel();
        _deactivationTaskCancellationTokenSource.Dispose();
        _deactivationTaskCancellationTokenSource = new CancellationTokenSource();
    }
    public void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
    {
        var aiming = players[playerIsTargeting.ShooterId];
        if (aiming == LocalPlayer) return;
        players.TryGetValue(playerIsTargeting.TargetId, out var target);
        aiming.Aim(target);
    }

    public void OnWinner(WinnerBroadcast winner)
    {
        HUDManager.Instance.AnnounceWinner(players[winner.PlayerId]);
    }

    public void StartGame()
    {
        NetworkManager.Instance.SendStartGame();
    }

    public void OnPlayerBalanceUpdate(PlayerBalanceUpdateBroadcast playerBalanceUpdate)
    {
        var updated = players[playerBalanceUpdate.PlayerId];
        updated.SetBalance(playerBalanceUpdate.Balance);
    }

    public void OnPlayerAccuracyStateBroadcast(PlayerAccuracyStateBroadcast accuracyStateBroadcast)
    {
        var player = players.GetValueOrDefault(accuracyStateBroadcast.PlayerId);
        if (player)
        {
            player.SetAccuracyState(accuracyStateBroadcast.AccuracyState);
        }
    }

    public void OnYourAccuracyAndPool(YourAccuracyAndPool yourAccuracyAndPool)
    {
        CanvasManager.Instance.HideAll();
        CanvasManager.Instance.Show(GameScreen.Roulette);
        RouletteManager.Instance.SetAccuracyPool(yourAccuracyAndPool.AccuracyPool);
        RouletteManager.Instance.SetTarget(yourAccuracyAndPool.YourAccuracy);
    }

    public void UpdateVelocity(Vector2 newDirection, bool running, float speed)
    {
        if (!LocalPlayer.Alive) return;
        LocalPlayer.SetDirection(newDirection);
        LocalPlayer.SetRunning(running);
        LocalPlayer.SetSpeed(speed);
        NetworkManager.Instance.SendPlayerMovementData(LocalPlayer.Position, LocalPlayer.CurrentDirection, running, LocalPlayer.Speed);
        lastMovementDataUpdateTimestmap = Time.time;
    }

    public void ShootTarget()
    {
        if (!LocalPlayer?.TargetPlayer) return;
        NetworkManager.Instance.SendPlayerShooting(LocalPlayer!.TargetPlayer!.ID);
    }

    private void SetCameraFollow([CanBeNull] Player p)
    {
        if (p is null)
        {
            Debug.LogError($"Can't follow null player.");
            return;
        }
        mainCamera.Follow(p.transform);
        HUDManager.Instance.ToggleSpectate(p != LocalPlayer);
    }

    private void RemovePlayerAll()
    {
        foreach (var p in players.Values)
        {
            playerManager.RemovePlayer(p.ID);
        }
        players.Clear();
    }

    private void OnDestroy()
    {
        _deactivationTaskCancellationTokenSource.Cancel();
        _deactivationTaskCancellationTokenSource.Dispose();
    }
}