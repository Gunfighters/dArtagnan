using System.Collections.Generic;
using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
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

    void Awake()
    {
        Instance = this;
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
        var directionVec = (Vector2) DirectionHelperClient.IntToDirection(info.MovementData.Direction);
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
            var obj = Instantiate(playerPrefab, estimatedPosition, Quaternion.identity, WorldCanvas.transform);
            var remotePlayer = obj.GetComponent<RemotePlayerController>();
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

    public void SetPing(Ping p)
    {
        Ping = p.time / 1000f;
    }

    void StartGame()
    {
        NetworkManager.Instance.SendStartGame();
    }
}