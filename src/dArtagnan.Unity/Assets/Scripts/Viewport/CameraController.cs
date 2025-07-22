using System.Linq;
using dArtagnan.Shared;
using Game;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour, IChannelListener
{
    public Player target;
    public SpriteRenderer groundRenderer;
    public Vector3 offset = new(0, 0, -10);
    public Camera cam;
    [SerializeField] public float cameraMoveSpeed;
    public float height;
    public float width;
    public Vector2 mapSize;
    public Vector2 center;

    public void Initialize()
    {
        LocalEventChannel.OnNewCameraTarget += Follow;
        PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
    }

    private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
    {
        if (!e.Alive && e.PlayerId == target.ID)
            LocalEventChannel.InvokeOnNewCameraTarget(PlayerGeneralManager.Survivors.First());
    }

    private void Follow(Player newTarget)
    {
        target = newTarget;
    }

    private void Start()
    {
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
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, mapSize * 2);
    }
}