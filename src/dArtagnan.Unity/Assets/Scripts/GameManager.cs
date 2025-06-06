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
        AddPlayer(payload.playerId, new Vector2(payload.initX, payload.initY), payload.accuracy);
        if (payload.playerId == controlledPlayerIndex)
        {
            mainCamera.transform.SetParent(ControlledPlayer.transform);
        }
    }

    public void OnPlayerDirectionFromServer(PlayerDirectionFromServer payload)
    {
        players[payload.playerId].SetDirection(IntToDirection(payload.direction));
    }

    public void OnPlayerRunningFromServer(PlayerRunningFromServer payload)
    {
        players[payload.playerId].SetRunning(payload.isRunning);
    }

    public static int DirectionToInt(Vector3 direction)
    {
        if (direction == Vector3.zero) return 0;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle = (angle + 360f + 22.5f) % 360f; // Normalize and offset
        var index = Mathf.FloorToInt(angle / 45f); // 0 to 7
        return index + 1; // 1 to 8 
    }

    public static Vector3 IntToDirection(int direction)
    {
        return direction switch
        {
            0 => Vector2.zero,
            1 => new Vector2(0, 1),
            2 => new Vector2(1, 1),
            3 => new Vector2(1, 0),
            4 => new Vector2(1, -1),
            5 => new Vector2(0, -1),
            6 => new Vector2(-1, -1),
            7 => new Vector2(-1, 0),
            8 => new Vector2(-1, 1),
            _ => Vector2.zero // fallback
        };
    }
}