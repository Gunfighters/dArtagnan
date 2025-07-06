using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using JetBrains.Annotations;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int playerObjectPoolSize;
    public List<GameObject> playerObjectPool = new();
    public GameObject playerPrefab;
    private readonly Dictionary<int, Player> players = new();
    public List<Player> Survivors => players.Values.Where(p => p.Alive).ToList();
    public float Ping { get; private set; }
    private int localPlayerId;
    [CanBeNull] public Player LocalPlayer => players.GetValueOrDefault(localPlayerId, null);
    private Camera mainCamera;
    public GameObject Ground;
    public AudioManager AudioManager;
    private int hostId;
    [CanBeNull] public Player Host => players.GetValueOrDefault(hostId, null);
    private GameState gameState;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        for (var i = 0; i < playerObjectPoolSize; i++)
        {
            var obj = Instantiate(playerPrefab, Ground.transform);
            obj.SetActive(false);
            playerObjectPool.Add(obj);
        }
    }

    private void Start()
    {
        NetworkManager.Instance.SendJoinRequest();
    }

    private void AddPlayer(PlayerInformation info, bool inGame)
    {
        Debug.Log($"Add Player #{info.PlayerId}");
        if (players.ContainsKey(info.PlayerId))
        {
            throw new Exception($"Trying to add player #{info.PlayerId} that already exists");
        }

        var obj = playerObjectPool.FirstOrDefault();
        if (obj is null)
        {
            Debug.LogWarning($"No more object in the pool. Instantiating...");
            obj = Instantiate(playerPrefab, Ground.transform);
        }
        playerObjectPool.Remove(obj);
        var player = obj.GetComponent<Player>();
        var directionVec = DirectionHelper.IntToDirection(info.MovementData.Direction);
        var estimatedPosition = info.MovementData.Position + Ping / 2 * directionVec;
        var estimatedRemainingReloadTime = info.RemainingReloadTime - Ping / 2;
        info.MovementData.Position = estimatedPosition;
        info.RemainingReloadTime = estimatedRemainingReloadTime;
        player.Initialize(info);
        players[info.PlayerId] = player;
        
        if (player == LocalPlayer)
        {
            SetCameraFollow(LocalPlayer);
            UIManager.Instance.OnLocalPlayerActivation(player);
        }
        player.ToggleUIOverHead(inGame);
        player.gameObject.layer = LayerMask.NameToLayer(player == LocalPlayer ? "LocalPlayer" : "RemotePlayer");
        player.gameObject.SetActive(true);
    }

    public void OnYouAre(YouAre payload)
    {
        localPlayerId = payload.PlayerId;
        UIManager.Instance.OnNewHost(localPlayerId == hostId);
    }

    public void OnNewHost(NewHost payload)
    {
        hostId = payload.HostId;
        UIManager.Instance.OnNewHost(localPlayerId == hostId);
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
        if (gameState == GameState.Playing)
        {
            shooter.ShowHitOrMiss(shooting.Hit);
        }
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
        ReleasePlayerObject(leaving.gameObject);
        players.Remove(leave.PlayerId);
    }

    private void ReleasePlayerObject(GameObject obj)
    {
        obj.SetActive(false);
        playerObjectPool.Add(obj);
    }

    public void OnGamePlaying(GamePlaying gamePlaying)
    {
        gameState = GameState.Playing;
        UIManager.Instance.SetupForGameState(GameState.Playing);
        AudioManager.PlayForState(GameState.Playing);
        foreach (var info in gamePlaying.PlayersInfo)
        {
            var p = players[info.PlayerId];
            p.Initialize(info);
            p.ToggleUIOverHead(true);
        }
    }

    public void OnGameWaiting(GameWaiting gameWaiting)
    {
        gameState = GameState.Waiting;
        RemovePlayerAll();
        foreach (var info in gameWaiting.PlayersInfo)
        {
            AddPlayer(info, false);
        }
        UIManager.Instance.SetupForGameState(GameState.Waiting);
        AudioManager.PlayForState(GameState.Waiting);
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
        UIManager.Instance.AnnounceWinner(players[winner.PlayerId]);
    }

    public void SetPing(Ping p)
    {
        Ping = p.time / 1000f;
    }

    public void StartGame()
    {
        NetworkManager.Instance.SendStartGame();
    }

    public void UpdateVelocity(Vector2 newDirection, bool running)
    {
        LocalPlayer.SetDirection(newDirection);
        LocalPlayer.SetRunning(running);
        NetworkManager.Instance.SendPlayerMovementData(LocalPlayer.Position, LocalPlayer.CurrentDirection, running);
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

    private void SetCameraFollow(Player p)
    {
        mainCamera.transform.SetParent(p.transform);
        mainCamera.transform.localPosition = new Vector3(0, 0, mainCamera.transform.localPosition.z);
    }

    private void RemovePlayerAll()
    {
        foreach (var p in players.Values)
        {
            ReleasePlayerObject(p.gameObject);
        }
        players.Clear();
    }
}