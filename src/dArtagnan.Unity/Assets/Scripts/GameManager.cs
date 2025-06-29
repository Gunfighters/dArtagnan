using System.Collections;
using System.Collections.Generic;
using Assets.HeroEditor4D.Common.Scripts.Common;
using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
///     게임 내 플레이어 전원을 조종합니다. Input 처리도 겸합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject playerPrefab;
    private readonly Dictionary<int, PlayerController> players = new();
    [SerializeField] private int controlledPlayerIndex = -1;
    public float Ping;
    private Vector3 lastDirection = Vector3.zero;
    public static GameManager Instance { get; private set; }
    public PlayerController ControlledPlayer => players[controlledPlayerIndex];
    public VariableJoystick joystick;
    public FixedJoystick shootJoystick;
    public PlayerController targetPlayer;
    public readonly Dictionary<int, float> cooldown = new();

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
        if (controlledPlayerIndex == -1) return;
        HandleMovementInput();
        UpdateButtonState();
        UpdateCooldown();
    }

    void HandleMovementInput()
    {
        var direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector3.up;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector3.down;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector3.left;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector3.right;
        }

        direction = direction.normalized;

        bool usingJoystick = joystick.Direction != Vector2.zero;

        if (usingJoystick)
        {
            direction = DirectionHelperClient.IntToDirection(DirectionHelperClient.DirectionToInt(joystick.Direction));
        }

        ControlledPlayer.SetMovementInformation(direction, ControlledPlayer.position, ControlledPlayer.speed);

        bool changed = false;
        if (direction != lastDirection)
        {
            lastDirection = direction;
            changed = true;
        }

        changed |= Input.GetKeyDown(KeyCode.Space) | Input.GetKeyUp(KeyCode.Space);

        bool running = Input.GetKey(KeyCode.Space) || usingJoystick;
        ControlledPlayer.SetMovementInformation(direction, ControlledPlayer.position, running ? 160f : 40f);

        if (changed)
        {
            NetworkManager.Instance.SendPlayerDirection(ControlledPlayer.position, direction, running);
        }

    }

    void UpdateButtonState()
    {
        targetPlayer = GetAutoTarget();
        ControlledPlayer.SetTarget(targetPlayer);
    }

    void UpdateCooldown()
    {
        foreach (var player in players.Values)
        {
            cooldown[player.id] = Mathf.Max(0, cooldown[player.id] - Time.deltaTime);
        }
    }
    
    PlayerController GetAutoTarget()
    {
        // 가장 가까운 적.
        float minDistance = float.MaxValue;
        PlayerController target = null;
        foreach (var player in players.Values)
        {
            if (player != ControlledPlayer && Vector2.Distance(player.position, ControlledPlayer.position) < minDistance)
            {
                minDistance = Vector2.Distance(player.position, ControlledPlayer.position);
                target = player;
            }
        }

        return target;
    }

    public void ShootTarget()
    {
        NetworkManager.Instance.SendPlayerShooting(targetPlayer.id);
        cooldown[ControlledPlayer.id] = ControlledPlayer.cooldownDuration; // TODO: remove hard coding
    }

    void AddPlayer(int index, Vector2 estimatedPosition, Vector3 direction, int accuracy, float speed)
    {
        Debug.Log($"Add Player #{index} at {estimatedPosition} with accuracy {accuracy}%");
        if (players.ContainsKey(index))
        {
            Debug.LogWarning($"Trying to add player #{index} that already exists");
            return;
        }

        cooldown[index] = 15f / 2;
        var created = Instantiate(playerPrefab); // TODO: Object Pooling.
        var player = created.GetComponent<PlayerController>();
        player.SetAccuracy(accuracy);
        player.SetMovementInformation(direction, estimatedPosition, speed);
        player.ImmediatelyMoveTo(estimatedPosition);
        player.id = index;
        players[index] = player;
    }

    public void OnYouAre(YouAre payload)
    {
        controlledPlayerIndex = payload.playerId;
        joystick.SetActive(true);
    }

    public void OnInformationOfPlayers(InformationOfPlayers informationOfPlayers)
    {
        foreach (var info in informationOfPlayers.info)
        {
            var directionVec = DirectionHelperClient.IntToDirection(info.direction);
            var estimated = PlayerController.EstimatePositionByPing(new Vector3(info.x, info.y), directionVec, info.speed);
            AddPlayer(info.playerId, estimated, directionVec, info.accuracy, info.speed);
        }
    }

    public void OnPlayerJoinBroadcast(PlayerJoinBroadcast payload)
    {
        AddPlayer(payload.playerId, new Vector2(payload.initX, payload.initY), Vector3.zero, payload.accuracy, 40f);
        if (payload.playerId == controlledPlayerIndex)
        {
            mainCamera.transform.SetParent(ControlledPlayer.transform, false);
        }
    }

    public void OnPlayerDirectionBroadcast(PlayerDirectionBroadcast payload)
    {
        if (payload.playerId == controlledPlayerIndex) return;
        var targetPlayer = players[payload.playerId];
        var direction = DirectionHelperClient.IntToDirection(payload.direction);
        var position = new Vector2(payload.currentX, payload.currentY);
        targetPlayer.SetMovementInformation(direction, position, payload.speed);
    }

    public void OnUpdatePlayerSpeedBroadcast(UpdatePlayerSpeedBroadcast update)
    {
        // players[update.playerId].SetSpeed(update.speed, + Ping);
    }

    public void OnPlayerShootingBroadcast(PlayerShootingBroadcast shooting)
    {
        var shooter = players[shooting.shooterId];
        shooter.Fire();
        cooldown[shooter.id] = shooter.cooldownDuration;
        shooter.ShowHitOrMiss(shooting.hit);
        // TODO: show hit or miss text
    }

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        players[updatePlayerAlive.playerId].Die();
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        players[leave.playerId].gameObject.SetActive(false);
    }

    public void SetPing(Ping p)
    {
        Ping = p.time / 1000f;
    }
}