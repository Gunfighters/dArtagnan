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
    private readonly List<PlayerController> players = new();
    private int controlledPlayerIndex;
    PlayerController ControlledPlayer => players[controlledPlayerIndex];

    void Update()
    {
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

        networkManager.SendPlayerDirection(direction); // TODO: send only on difference
    }

    void AddPlayer(int index, Vector2 position, int accuracy)
    {
        var created = Instantiate(playerPrefab); // TODO: Object Pooling.
        var player = created.GetComponent<PlayerController>();
        player.Accuracy = accuracy;
        player.ImmediatelyMoveTo(position);
        players.Add(player);
    }

    public void OnYouAre(YouAre payload)
    {
        controlledPlayerIndex = payload.playerId;
    }

    public void OnJoinResponseFromServer(JoinResponseFromServer payload)
    {
        AddPlayer(payload.playerId, payload.position, payload.accuracy);
        if (payload.playerId == controlledPlayerIndex)
        {
            mainCamera.transform.SetParent(ControlledPlayer.transform);
        }
    }

    public void OnPlayerDirectionFromServer(PlayerDirectionFromServer payload)
    {
        players[payload.playerId].SetDirection(payload.direction);
    }

    public void OnPlayerRunningFromServer(PlayerRunningFromServer payload)
    {
        players[payload.playerId].SetRunning(payload.isRunning);
    }
}