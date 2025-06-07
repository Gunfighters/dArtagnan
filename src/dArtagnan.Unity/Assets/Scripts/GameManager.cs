using System.Collections.Generic;
using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     게임 내 플레이어 전원을 조종합니다. Input 처리도 겸합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public Camera mainCamera;
    public NetworkManager networkManager;
    public GameObject playerPrefab;
    private readonly Dictionary<int, PlayerController> players = new();
    private int controlledPlayerIndex = -1;
    private Vector3 lastDirection = Vector3.zero;
    PlayerController ControlledPlayer => players[controlledPlayerIndex];

    void Start()
    {
        networkManager.SendJoinRequest();
    }

    void Update()
    {
        if (controlledPlayerIndex == -1) return;
        Vector3 direction = Vector3.zero;
        if (Keyboard.current.wKey.isPressed)
        {
            direction += Vector3.up;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            direction += Vector3.down;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            direction += Vector3.left;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            direction += Vector3.right;
        }

        direction = direction.normalized;

        if (direction != lastDirection)
        {
            lastDirection = direction;
            networkManager.SendPlayerDirection(direction); // TODO: send only on difference
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            networkManager.SendPlayerIsRunning(true);
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            networkManager.SendPlayerIsRunning(false);
        }
    }

    void AddPlayer(int index, Vector2 position, int accuracy)
    {
        Debug.Log($"Add Player #{index} at {position} with accuracy {accuracy}%");
        if (players.ContainsKey(index))
        {
            Debug.LogError($"Player {index} already exists");
            return;
        }

        var created = Instantiate(playerPrefab); // TODO: Object Pooling.
        var player = created.GetComponent<PlayerController>();
        player.Accuracy = accuracy;
        player.ImmediatelyMoveTo(position);
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
            AddPlayer(info.playerId, new Vector2(info.x, info.y), info.accuracy);
        }
    }

    public void OnJoinResponseFromServer(JoinResponseFromServer payload)
    {
        AddPlayer(payload.playerId, new Vector2(payload.initX, payload.initY), payload.accuracy);
        if (payload.playerId == controlledPlayerIndex)
        {
            mainCamera.transform.SetParent(ControlledPlayer.transform);
        }
    }

    public void OnPlayerDirectionFromServer(PlayerDirectionFromServer payload)
    {
        var direction = DirectionHelper.IntToDirection(payload.direction);
        Debug.Log(direction);
        players[payload.playerId].SetDirection(direction);
    }

    public void OnPlayerRunningFromServer(PlayerRunningFromServer payload)
    {
        players[payload.playerId].SetRunning(payload.isRunning);
    }
}