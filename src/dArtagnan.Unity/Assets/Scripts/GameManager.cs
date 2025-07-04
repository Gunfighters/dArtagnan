using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public const int RemotePlayerPoolSize = 7;
    public Camera mainCamera;
    public LocalPlayerController localPlayer;
    public GameObject playerPrefab;
    private int localPlayerId;
    public readonly Dictionary<int, RemotePlayerController> remotePlayers = new();
    public float Ping { get; private set; }
    public static GameManager Instance { get; private set; }
    public Canvas WorldCanvas;
    public Button GameStartButton;
    private int hostId;
    public TextMeshProUGUI winnerAnnouncement;
    public GameState GameState { get; private set; }
    public List<RemotePlayerController> RemotePlayerObjectPool;

    void Awake()
    {
        Instance = this;
        for (var i = 0; i < RemotePlayerPoolSize; i++)
        {
            var created = Instantiate(playerPrefab, WorldCanvas.transform);
            created.SetActive(false);
            RemotePlayerObjectPool.Add(created.GetComponent<RemotePlayerController>());
        }
        ToggleUIOverHeadEveryone(false);
    }

    void Start()
    {
        GameStartButton.onClick.AddListener(StartGame);
        NetworkManager.Instance.SendJoinRequest();
    }

    void Update()
    {
        UpdateCooldown();
    }

    void UpdateCooldown()
    {
        foreach (var player in remotePlayers.Values)
        {
            player.cooldown = Mathf.Max(0, player.cooldown - Time.deltaTime);
        }
        localPlayer.cooldown = Mathf.Max(0, localPlayer.cooldown - Time.deltaTime);
    }

    void AddPlayer(PlayerInformation info)
    {
        var serverPosition = new Vector2(info.MovementData.Position.X, info.MovementData.Position.Y);
        var directionVec = DirectionHelperClient.IntToDirection(info.MovementData.Direction);
        var estimatedPosition = serverPosition + directionVec * Ping / 2;
        var estimatedRemainingReloadTime = info.RemainingReloadTime - Ping / 2;
        Debug.Log($"Add Player #{info.PlayerId}");
        if (remotePlayers.ContainsKey(info.PlayerId))
        {
            Debug.LogWarning($"Trying to add player #{info.PlayerId} that already exists");
            return;
        }

        Player player;
        if (info.PlayerId == localPlayerId)
        {
            player = localPlayer;
            mainCamera.transform.SetParent(player.transform);
        }
        else
        {
            var remotePlayer = RemotePlayerObjectPool.FirstOrDefault(o => !o.gameObject.activeInHierarchy);
            if (remotePlayer is null)
            {
                Debug.Log("No remote player available in the pool. Instantiating...");
                remotePlayer = Instantiate(playerPrefab, WorldCanvas.transform).GetComponent<RemotePlayerController>();
                RemotePlayerObjectPool.Add(remotePlayer);
            }
            remotePlayer.SetMovementData(directionVec, estimatedPosition, info.MovementData.Speed);
            remotePlayers[info.PlayerId] = remotePlayer;
            player = remotePlayer;
        }
        player.id = info.PlayerId;
        player.SetNickname($"#{player.id}");
        player.SetAccuracy(info.Accuracy);
        player.ImmediatelyMoveTo(estimatedPosition);
        player.range = info.Range;
        player.cooldown = estimatedRemainingReloadTime;
        player.cooldownDuration = info.TotalReloadTime;
        player.dead = !info.Alive;
        if (player.dead)
        {
            player.Die();
        }
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
            AddPlayer(info);
        }
    }

    public void OnPlayerJoinBroadcast(PlayerJoinBroadcast payload)
    {
        var info = payload.PlayerInfo;
        if (info.PlayerId != localPlayerId)
        {
            AddPlayer(info);
        }
    }

    public void OnPlayerMovementData(PlayerMovementDataBroadcast payload)
    {
        if (payload.PlayerId == localPlayerId) return;
        var targetPlayer = remotePlayers[payload.PlayerId];
        var direction = DirectionHelperClient.IntToDirection(payload.MovementData.Direction);
        var serverPosition = new Vector2(payload.MovementData.Position.X, payload.MovementData.Position.Y);
        var position = RemotePlayerController.EstimatePositionByPing(serverPosition, direction, payload.MovementData.Speed);
        targetPlayer.SetMovementData(direction, position, payload.MovementData.Speed);
    }

    public void OnPlayerShootingBroadcast(PlayerShootingBroadcast shooting)
    {
        Player shooter = shooting.ShooterId == localPlayerId ? localPlayer : remotePlayers[shooting.ShooterId];
        Player target = shooting.TargetId == localPlayerId ? localPlayer : remotePlayers[shooting.TargetId];
        shooter.Fire(target);
        shooter.cooldown = shooter.cooldownDuration - Ping / 2;
        shooter.ShowHitOrMiss(shooting.Hit);
        // TODO: show hit or miss text
    }

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        Player aliveOrDead = updatePlayerAlive.PlayerId == localPlayerId ? localPlayer : remotePlayers[updatePlayerAlive.PlayerId];
        if (!updatePlayerAlive.Alive)
        {
            aliveOrDead.Die();
        }
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        remotePlayers[leave.PlayerId].gameObject.SetActive(false);
        remotePlayers.Remove(leave.PlayerId);
    }

    public void OnGameStarted(GameStarted gameStarted)
    {
        foreach (var info in gameStarted.Players)
        {
            Player player = info.PlayerId == localPlayerId ? localPlayer : remotePlayers[info.PlayerId];
            player.SetAsInfo(info);
            player.gameObject.SetActive(true);
        }
    }

    public void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
    {
        if (playerIsTargeting.ShooterId == localPlayerId) return;
        var shooter = remotePlayers[playerIsTargeting.ShooterId];
        shooter.modelManager.HideTrajectory();
        if (playerIsTargeting.TargetId == -1) return;
        Player target = playerIsTargeting.TargetId == localPlayerId ? localPlayer : remotePlayers[playerIsTargeting.TargetId];
        shooter.modelManager.ShowTrajectory(target.transform.position, true);
    }

    public void OnWinner(Winner winner)
    {
        Player winnerPlayer = winner.PlayerId == localPlayerId ? localPlayer : remotePlayers[winner.PlayerId];
        winnerAnnouncement.text = $"{winnerPlayer.nickname} HAS WON!";
        winnerAnnouncement.gameObject.SetActive(true);
    }

    void ToggleUIOverHeadEveryone(bool toggle)
    {
        localPlayer.ToggleUIOverHead(toggle);
        foreach (var remotePlayerController in RemotePlayerObjectPool)
        {
            remotePlayerController.ToggleUIOverHead(toggle);
        }
    }

    public void OnNewGameState(NewGameState newGameState)
    {
        GameState = newGameState.GameState;
        switch (GameState)
        {
            case GameState.Waiting:
                ToggleUIOverHeadEveryone(false);
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

    void StartGame()
    {
        NetworkManager.Instance.SendStartGame();
    }
}