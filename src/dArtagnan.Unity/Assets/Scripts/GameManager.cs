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
    public int playerObjectPoolSize;
    private Camera mainCamera;
    public readonly Dictionary<int, Player> players = new();
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
    public GameObject Ground;
    private int hostId;
    [CanBeNull] public Player Host => players[hostId];
    public GameState GameState { get; private set; } = GameState.Waiting;
    public List<GameObject> playerObjectPool = new();
    public AudioManager AudioManager;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        // ToggleUIOverHeadEveryone(false);
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
        AudioManager.PlayForState(GameState);
    }

    private void AddPlayer(PlayerInformation info, bool InGame)
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
            mainCamera.transform.SetParent(player.transform, false);
            UIManager.Instance.OnLocalPlayerActivation(player);
        }
        player.ToggleUIOverHead(true);
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
        foreach (var player in players.Values)
        {
            Destroy(player.gameObject);
        }
        players.Clear();
        foreach (var info in gameStarted.Players)
        {
            AddPlayer(info, true);
        }
        ToggleUIOverHeadEveryone(true);
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
        UIManager.Instance.SetupForGameState(GameState);
        AudioManager.PlayForState(GameState);
        switch (GameState)
        {
            case GameState.Waiting:
                mainCamera.transform.SetParent(Ground.transform);
                foreach (var player in players.Values)
                {
                    player.Reset();
                }
                break;
            case GameState.Playing:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
}