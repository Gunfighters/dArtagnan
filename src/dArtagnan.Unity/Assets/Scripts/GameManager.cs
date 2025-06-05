using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
///     게임 내 플레이어 전원을 조종합니다. Input 처리도 겸합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public Camera mainCamera;
    public int controlledPlayerIndex;
    public float doubleTapThreshold;
    private readonly List<PlayerController> players = new();
    private int tapCount;
    public PlayerController ControlledPlayer => players[controlledPlayerIndex];

    private void Start()
    {
        for (var i = 0; i < 8; i++)
            AddPlayer(i);
        mainCamera.transform.SetParent(ControlledPlayer.transform);
    }

    public void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == TouchPhase.Began)
            {
                tapCount++;
                if (tapCount == 1)
                {
                    StartCoroutine(ResetTapCount());
                }

                if (tapCount >= 2)
                {
                    ControlledPlayer.SetRunning();
                }
            }

            Vector3 screenPosition = new(
                touch.screenPosition.x,
                touch.screenPosition.y,
                Mathf.Abs(mainCamera.transform.position.z - ControlledPlayer.transform.position.z)
            );
            var worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPoint.z = ControlledPlayer.transform.position.z;
            ControlledPlayer.SetDirectionTowards(worldPoint);
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            var point = Mouse.current.position.ReadValue();
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                tapCount++;
                if (tapCount == 1)
                {
                    StartCoroutine(ResetTapCount());
                }

                if (tapCount >= 2)
                {
                    ControlledPlayer.SetRunning();
                }
            }

            Vector3 screenPosition = new(
                point.x,
                point.y,
                Mathf.Abs(mainCamera.transform.position.z - ControlledPlayer.transform.position.z)
            );
            var worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPoint.z = ControlledPlayer.transform.position.z;
            ControlledPlayer.SetDirectionTowards(worldPoint);
        }
        else
        {
            ControlledPlayer.StopMoving();
        }
    }

    private IEnumerator ResetTapCount()
    {
        yield return new WaitForSeconds(doubleTapThreshold);
        if (tapCount == 1)
            tapCount = 0;
        else if (tapCount >= 2)
            tapCount = 0;
    }

    public void AddPlayer(int index)
    {
        var created = Instantiate(playerPrefab); // TODO: Object Pooling.
        var player = created.GetComponent<PlayerController>();
        players.Add(player);
    }

    public void OnPlayerMove(int index, Vector2 position)
    {
        players[index].SetDirectionTowards(position);
    }

    public void OnPlayerFire(int firingIndex, int targetIndex)
    {
        players[firingIndex].Fire();
    }
}