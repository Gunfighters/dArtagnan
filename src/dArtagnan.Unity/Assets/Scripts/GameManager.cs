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

    void AddPlayer(int id, Vector2 estimatedPosition, Vector3 direction, int accuracy, float speed, float remainingReloadTime, float totalReloadTime, bool alive)
    {
        Debug.Log($"Add Player #{id} at {estimatedPosition} with accuracy {accuracy}%");
        if (remotePlayers.ContainsKey(id))
        {
            Debug.LogWarning($"Trying to add player #{id} that already exists");
            return;
        }

        GameObject created;
        if (id == localPlayerId)
        {
            created = localPlayer.gameObject;
            localPlayer.rb.MovePosition(estimatedPosition);
            mainCamera.transform.SetParent(localPlayer.transform);
            localPlayer = localPlayer.GetComponent<LocalPlayerController>();
            localPlayer.SetAccuracy(accuracy);
            localPlayer.speed = speed;
            localPlayer.cooldown = remainingReloadTime;
            localPlayer.cooldownDuration = totalReloadTime;
            localPlayer.dead = !alive;
            if (localPlayer.dead)
            {
                localPlayer.Die();
            }
        }
        else
        {
            created = Instantiate(playerPrefab, estimatedPosition, Quaternion.identity);
            var player = created.GetComponent<RemotePlayerController>();
            player.id = id;
            player.SetAccuracy(accuracy);
            player.SetMovementInformation(direction, estimatedPosition, speed);
            player.cooldown = remainingReloadTime;
            player.cooldownDuration = totalReloadTime;
            player.dead = !alive;
            if (player.dead)
            {
                player.Die();
            }
            remotePlayers[id] = player;
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
            var directionVec = DirectionHelperClient.IntToDirection(info.direction);
            var estimated = RemotePlayerController.EstimatePositionByPing(new Vector3(info.x, info.y), directionVec, info.speed);
            AddPlayer(info.playerId, estimated, directionVec, info.accuracy, info.speed, info.remainingReloadTime - Ping / 2, info.totalReloadTime, info.alive);
        }
    }

    public void OnPlayerJoinBroadcast(PlayerJoinBroadcast payload)
    {
        if (payload.playerId != localPlayerId)
        {
            AddPlayer(payload.playerId, new Vector2(payload.initX, payload.initY), Vector3.zero, payload.accuracy, 40f,
                7.5f - Ping / 2, 15, true);
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
        remotePlayers[updatePlayerAlive.playerId].Die();
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