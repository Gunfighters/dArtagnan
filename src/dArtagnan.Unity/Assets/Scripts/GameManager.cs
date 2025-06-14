using System.Collections;
using System.Collections.Generic;
using dArtagnan.Shared;
using UnityEngine;

/// <summary>
///     게임 내 플레이어 전원을 조종합니다. Input 처리도 겸합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject playerPrefab;
    private readonly Dictionary<int, PlayerController> players = new();
    [SerializeField] private int controlledPlayerIndex = -1;
    private Vector3 lastDirection = Vector3.zero;
    public static GameManager Instance { get; private set; }
    PlayerController ControlledPlayer => players[controlledPlayerIndex];
    [SerializeField] private int ping; 

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
        Vector3 direction = Vector3.zero;
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

        if (direction != lastDirection)
        {
            lastDirection = direction;
            NetworkManager.Instance.SendPlayerDirection(direction);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            NetworkManager.Instance.SendPlayerIsRunning(true);
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            NetworkManager.Instance.SendPlayerIsRunning(false);
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            float nearestDist = 0x7fffffff;
            PlayerController nearestPlayer = null;
            foreach (var other in players.Values)
            {
                if (other != ControlledPlayer)
                {
                    var dist = Vector3.Distance(other.transform.position, ControlledPlayer.transform.position);
                    if (dist <= ControlledPlayer.range && nearestDist > dist)
                    {
                        nearestDist = dist;
                        nearestPlayer = other;
                    }
                }
            }

            if (nearestPlayer)
            {
                NetworkManager.Instance.SendPlayerShooting(nearestPlayer.id);
            }
        }
    }

    public void ShootAt(PlayerController target)
    {
        NetworkManager.Instance.SendPlayerShooting(target.id);
    }

    void AddPlayer(int index, Vector2 position, int direction, int accuracy)
    {
        Debug.Log($"Add Player #{index} at {position} with accuracy {accuracy}%");
        if (players.ContainsKey(index))
        {
            Debug.LogWarning($"Trying to add player #{index} that already exists");
            return;
        }

        var created = Instantiate(playerPrefab); // TODO: Object Pooling.
        var player = created.GetComponent<PlayerController>();
        player.SetAccuracy(accuracy);
        player.SetDirection(DirectionHelper.IntToDirection(direction));
        player.ImmediatelyMoveTo(position);
        player.id = index;
        players[index] = player;
    }

    public void OnYouAre(YouAre payload)
    {
        controlledPlayerIndex = payload.playerId;
    }

    public void OnInformationOfPlayers(InformationOfPlayers informationOfPlayers)
    {
        Debug.Log(informationOfPlayers.info);
        foreach (var info in informationOfPlayers.info)
        {
            Debug.Log(info.direction);
            AddPlayer(info.playerId, new Vector2(info.x, info.y), info.direction, info.accuracy);
        }
    }

    public void OnPlayerJoinBroadcast(PlayerJoinBroadcast payload)
    {
        AddPlayer(payload.playerId, new Vector2(payload.initX, payload.initY), 0, payload.accuracy);
        if (payload.playerId == controlledPlayerIndex)
        {
            mainCamera.transform.SetParent(ControlledPlayer.transform);
        }
    }

    public void OnPlayerDirectionBroadcast(PlayerDirectionBroadcast payload)
    {
        var direction = DirectionHelper.IntToDirection(payload.direction);
        Debug.Log(direction);
        players[payload.playerId].SetDirection(direction);
    }

    public void OnUpdatePlayerSpeedBroadcast(UpdatePlayerSpeedBroadcast update)
    {
        players[update.playerId].SetSpeed(update.speed);
    }

    public void OnPlayerShootingBroadcast(PlayerShootingBroadcast shooting)
    {
        players[shooting.shooterId].Fire();
        // TODO: show hit or miss text
    }

    public void OnUpdatePlayerAlive(UpdatePlayerAlive updatePlayerAlive)
    {
        players[updatePlayerAlive.playerId].gameObject.SetActive(updatePlayerAlive.alive);
    }

    public void OnPlayerLeaveBroadcast(PlayerLeaveBroadcast leave)
    {
        players[leave.playerId].gameObject.SetActive(false);
    }

    public void OnUpdatePlayerPosition(UpdatePlayerPosition update)
    {
        update.positionList.ForEach(p =>
        {
            players[p.playerId].UpdatePosition(new Vector2(p.x, p.y));
        });
    }

    public void GetPing(string address)
    {
        StartCoroutine(StartPing(address));
    }

    IEnumerator StartPing(string address)
    {
        var wait = new WaitForSeconds(0.01f);
        var p = new Ping(address);
        while (!p.isDone)
        {
            yield return wait;
        }

        ping = p.time;
        Debug.Log($"Ping: {ping}");
        SetPing(p);
    }

    void SetPing(Ping p)
    {
        foreach (var player in players.Values)
        {
            player.SetPing(p);
        }
    }
}