using System.Collections;
using System.Collections.Generic;
using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
///     게임 내 플레이어 전원을 조종합니다. Input 처리도 겸합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public NetworkManager networkManager;
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

    public async void Update()
    {
        Vector2 clicked;
        bool firstTap = false;
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == TouchPhase.Began)
            {
                firstTap = true;
                HandleDoubleTap();
            }

            clicked = touch.screenPosition;
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                firstTap = true;
                HandleDoubleTap();
            }

            clicked = Mouse.current.position.ReadValue();
        }
        else
        {
            ControlledPlayer.StopMoving();
            return;
        }

        Vector3 screenPosition = clicked;
        screenPosition.z = Mathf.Abs(mainCamera.transform.position.z - ControlledPlayer.transform.position.z);
        var worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
        if (firstTap)
        {
            var hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider && hit.transform.CompareTag(playerPrefab.tag))
            {
                ControlledPlayer.Fire();
                var hitPlayer = hit.transform.gameObject.GetComponent<PlayerController>();
                hitPlayer.Die();
            }
        }
        else
        {
            worldPoint.z = ControlledPlayer.transform.position.z;
            ControlledPlayer.SetDirectionTowards(worldPoint);
            // var normalized = (worldPoint - ControlledPlayer.transform.position).normalized;
            // await networkManager.SendPacket(PacketType.PlayerMove, new MovePacket
            // {
            //     PlayerId = controlledPlayerIndex,
            //     X = ControlledPlayer.transform.position.x + Time.deltaTime * ControlledPlayer.speed * normalized.x,
            //     Y = ControlledPlayer.transform.position.y + Time.deltaTime * ControlledPlayer.speed * normalized.y,
            // });
        }
    }

    private void HandleDoubleTap()
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

    private IEnumerator ResetTapCount()
    {
        yield return new WaitForSeconds(doubleTapThreshold);
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