using System.Linq;
using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public PlayerCore target;
    public SpriteRenderer groundRenderer;
    public Vector3 offset = new(0, 0, -10);
    public Camera cam;
    [SerializeField] public float cameraMoveSpeed;
    public float height;
    public float width;
    public Vector2 mapSize;
    public Vector2 center;

    private void Awake()
    {
        LocalEventChannel.OnNewCameraTarget += Follow;
        PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
        cam = GetComponent<Camera>();
        mapSize = groundRenderer.bounds.size / 2;
        height = cam.orthographicSize;
        width = height * cam.aspect;
    }

    private void Update()
    {
        if (target)
        {
            LimitCameraArea();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, mapSize * 2);
    }

    private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
    {
        if (!e.Alive && e.PlayerId == target.ID)
        {
            var newTarget = GameService.Survivors.FirstOrDefault(p => p != target);
            if (newTarget is not null)
                LocalEventChannel.InvokeOnNewCameraTarget(newTarget);
        }
    }

    private void Follow(PlayerCore newTarget)
    {
        target = newTarget;
    }

    private void LimitCameraArea()
    {
        transform.position = Vector3.Lerp(transform.position,
            target.transform.position + offset,
            Time.deltaTime * cameraMoveSpeed);
        var lx = mapSize.x - width;
        var clampX = Mathf.Clamp(transform.position.x, -lx + center.x, lx + center.x);

        var ly = mapSize.y - height;
        var clampY = Mathf.Clamp(transform.position.y, -ly + center.y, ly + center.y);

        transform.position = new Vector3(clampX, clampY, -10f);
    }
}