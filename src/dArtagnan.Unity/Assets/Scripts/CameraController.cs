using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Transform target;
    public SpriteRenderer groundRenderer;
    private Vector3 offset = new(0, 0, -10);
    private Camera cam;
    [SerializeField] private float cameraMoveSpeed;
    private float height;
    private float width;
    private Vector2 mapSize;
    private Vector2 center;

    public void Follow(Transform newTarget)
    {
        target = newTarget;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        mapSize = groundRenderer.bounds.size / 2;
        height = cam.orthographicSize;
        width = height * Screen.width / Screen.height;
    }

    private void Update()
    {
        LimitCameraArea();
    }

    private void LimitCameraArea()
    {
        transform.position = Vector3.Lerp(transform.position, 
            target.position + offset, 
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