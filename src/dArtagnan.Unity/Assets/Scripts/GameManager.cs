using System.Collections.Generic;
using Assets.HeroEditor4D.Common.Scripts.Common;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using dArtagnan.Shared;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Camera mainCamera;
    public LocalPlayerController localPlayer;
    public GameObject playerPrefab;
    [SerializeField] private int localPlayerId;
    public readonly Dictionary<int, RemotePlayerController> remotePlayers = new();
    public float Ping { get; private set; }
    public static GameManager Instance { get; private set; }
    public Canvas WorldCanvas;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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
        var serverPosition = new Vector2(info.x, info.y);
        var directionVec = (Vector2) DirectionHelperClient.IntToDirection(info.direction);
        var estimatedPosition = serverPosition + directionVec * Ping / 2;
        var estimatedRemainingReloadTime = info.remainingReloadTime - Ping / 2;
        Debug.Log($"Add Player #{info.playerId}");
        if (remotePlayers.ContainsKey(info.playerId))
        {
            Debug.LogWarning($"Trying to add player #{info.playerId} that already exists");
            return;
        }

        GameObject created;
        if (info.playerId == localPlayerId)
        {
            created = localPlayer.gameObject;
            localPlayer.rb.MovePosition(estimatedPosition);
            mainCamera.transform.SetParent(localPlayer.transform);
            localPlayer = localPlayer.GetComponent<LocalPlayerController>();
            localPlayer.SetAccuracy(info.accuracy);
            localPlayer.range = info.range;
            localPlayer.speed = info.speed;
            localPlayer.cooldown = estimatedRemainingReloadTime;
            localPlayer.cooldownDuration = info.totalReloadTime;
            localPlayer.dead = !info.alive;
            if (localPlayer.dead)
            {
                localPlayer.Die();
            }
        }
        else
        {
            created = Instantiate(playerPrefab, estimatedPosition, Quaternion.identity, WorldCanvas.transform);
            var player = created.GetComponent<RemotePlayerController>();
            player.id = info.playerId;
            player.SetAccuracy(info.accuracy);
            player.SetMovementInformation(directionVec, estimatedPosition, info.speed);
            player.range = info.range;
            player.cooldown = estimatedRemainingReloadTime;
            player.cooldownDuration = info.totalReloadTime;
            player.dead = !info.alive;
            if (player.dead)
            {
                player.Die();
            }
            remotePlayers[info.playerId] = player;
        }
        created.SetActive(true);
    }

    public void OnYouAre(YouAre payload)
    {
        localPlayerId = payload.playerId;
    }

    public void OnInformationOfPlayers(InformationOfPlayers informationOfPlayers)
    {
        foreach (var info in informationOfPlayers.info)
        {
            AddPlayer(info);
        }
    }

    public void OnPlayerJoinBroadcast(PlayerJoinBroadcast payload)
    {
        var info = payload.playerInfo;
        if (info.playerId != localPlayerId)
        {
            AddPlayer(info);
        }
    }

    public void OnPlayerDirectionBroadcast(PlayerDirectionBroadcast payload)
    {
        if (payload.playerId == localPlayerId) return;
        var targetPlayer = remotePlayers[payload.playerId];
        var direction = DirectionHelperClient.IntToDirection(payload.direction);
        var position = RemotePlayerController.EstimatePositionByPing(new Vector2(payload.currentX, payload.currentY), direction, payload.speed);
        targetPlayer.SetMovementInformation(direction, position, payload.speed);
    }

    public void OnUpdatePlayerSpeedBroadcast(UpdatePlayerSpeedBroadcast update)
    {
        // players[update.playerId].SetSpeed(update.speed, + Ping);
    }

    public void OnPlayerShootingBroadcast(PlayerShootingBroadcast shooting)
    {
        Player shooter = shooting.shooterId == localPlayerId ? localPlayer : remotePlayers[shooting.shooterId];
        shooter.Fire();
        shooter.cooldown = shooter.cooldownDuration - Ping / 2;
        shooter.ShowHitOrMiss(shooting.hit);
        // TODO: show hit or miss text
    }

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        Player aliveOrDead = updatePlayerAlive.playerId == localPlayerId ? localPlayer : remotePlayers[updatePlayerAlive.playerId];
        if (!updatePlayerAlive.alive)
        {
            aliveOrDead.Die();
        }
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        remotePlayers[leave.playerId].gameObject.SetActive(false);
    }

    public void SetPing(Ping p)
    {
        Ping = p.time / 1000f;
    }
}