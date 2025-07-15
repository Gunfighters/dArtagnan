using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using JetBrains.Annotations;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int playerObjectPoolSize;
    public List<GameObject> playerObjectPool = new();
    public GameObject playerPrefab;
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

    private void Awake()
    {
        Instance = this;
        for (var i = 0; i < playerObjectPoolSize; i++)
        {
            var obj = Instantiate(playerPrefab);
            obj.SetActive(false);
            playerObjectPool.Add(obj);
        }
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
        Debug.Log($"Add Player #{info.PlayerId} at {info.MovementData.Position} with direction {info.MovementData.Direction} and speed {info.MovementData.Speed}");
        if (players.ContainsKey(info.PlayerId))
        {
            throw new Exception($"Trying to add player #{info.PlayerId} that already exists");
        }

        var obj = playerObjectPool.FirstOrDefault();
        if (obj is null)
        {
            Debug.LogWarning($"No more object in the pool. Instantiating...");
            obj = Instantiate(playerPrefab, Field.transform);
        }
        playerObjectPool.Remove(obj);
        var player = obj.GetComponent<Player>();
        var directionVec = DirectionHelper.IntToDirection(info.MovementData.Direction);
        var estimatedPosition = info.MovementData.Position;
        var estimatedRemainingReloadTime = info.RemainingReloadTime;
        Debug.Log($"Estimated Position: {estimatedPosition}");
        info.MovementData.Position = estimatedPosition;
        info.RemainingReloadTime = estimatedRemainingReloadTime;
        player.Initialize(info);
        players[info.PlayerId] = player;
        Debug.Log($"Player #{info.PlayerId} added at {player.Position} (Object: {players[info.PlayerId]})");
        
        if (player == LocalPlayer)
        {
            CanvasManager.Instance.Show(GameScreen.HUD);
            SetCameraFollow(player);
            HUDManager.Instance.OnLocalPlayerActivation(player);
        }
        player.ToggleUIInGame(inGame);
        player.gameObject.layer = LayerMask.NameToLayer(player == LocalPlayer ? "LocalPlayer" : "RemotePlayer");
        player.gameObject.SetActive(true);
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

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        var player = players[updatePlayerAlive.PlayerId];
        player.SetAlive(updatePlayerAlive.Alive);
        if (!player.Alive)
        {
            ScheduleDeactivation(player);
            if (player.transform == mainCamera.target)
            {
                var anotherPlayer = players.Values.FirstOrDefault(p => p.Alive);
                SetCameraFollow(anotherPlayer);
            }
        }
    }

    private void ScheduleDeactivation(Player p)
    {
        StartCoroutine(DelayedDeactivate(p));
    }

    private IEnumerator DelayedDeactivate(Player p)
    {
        yield return new WaitForSeconds(1.5f);
        p.gameObject.SetActive(false);
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        if (players.TryGetValue(leave.PlayerId, out var leavingPlayer))
        {
            ReleasePlayerObject(leavingPlayer);
            players.Remove(leave.PlayerId);
            Debug.Log($"Player #{leave.PlayerId} was successfully removed.");
        }
        else
        {
            Debug.LogWarning($"Tried to remove player #{leave.PlayerId}, but they were not found in the dictionary. They might have been cleared already.");
        }
    }

    private void ReleasePlayerObject(Player p)
    {
        Debug.Log($"Releasing: {p.ID}");
        p.gameObject.SetActive(false);
        playerObjectPool.Add(p.gameObject);
    }

    public void OnGamePlaying(GameInPlayingFromServer gamePlaying)
    {
        CanvasManager.Instance.Hide(GameScreen.Roulette);
        CanvasManager.Instance.Show(GameScreen.HUD);
        gameState = GameState.Playing;
        HUDManager.Instance.SetupForGameState(gamePlaying);
        AudioManager.PlayForState(GameState.Playing);
        foreach (var info in gamePlaying.PlayersInfo)
        {
            var p = players[info.PlayerId];
            p.Initialize(info);
            p.gameObject.SetActive(true);
            p.ToggleUIInGame(true);
        }
        SetCameraFollow(LocalPlayer);
    }

    public void OnGameWaiting(GameInWaitingFromServer gameWaiting)
    {
        StopAllCoroutines();
        gameState = GameState.Waiting;
        RemovePlayerAll();
        foreach (var info in gameWaiting.PlayersInfo)
        {
            AddPlayer(info, false);
        }
        HUDManager.Instance.SetupForGameState(gameWaiting);
        AudioManager.PlayForState(GameState.Waiting);
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
        RouletteManager.Instance.SetAccuracyPool(yourAccuracyAndPool.AccuracyPool);
        RouletteManager.Instance.SetTarget(yourAccuracyAndPool.YourAccuracy);
        CanvasManager.Instance.Show(GameScreen.Roulette);
        CanvasManager.Instance.Hide(GameScreen.HUD);
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
            ReleasePlayerObject(p);
        }
        players.Clear();
    }
}