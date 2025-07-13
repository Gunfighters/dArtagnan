using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Player player;
    public float cameraMoveSpeed;
    public Vector3 cameraPosition;
    public Vector2 center;
    public Vector2 mapSize;
    public float height;
    public float width;

    private void Start()
    {
        height = Camera.main.orthographicSize;
        width = height * Screen.width / Screen.height;
    }

    private void Update()
    {
        if (player)
            LimitCameraArea();
    }

    private void LimitCameraArea()
    {
        transform.position = Vector3.Lerp(transform.position, player.transform.position + cameraPosition, cameraMoveSpeed * Time.deltaTime);
        var lx = mapSize.x - width;
        var clampX = Mathf.Clamp(transform.position.x, -lx + center.x, lx + center.x);
        var ly = mapSize.y - height;
        var clampY = Mathf.Clamp(transform.position.y, -ly + center.y, ly + center.y);
        
        transform.position = new Vector3(clampX, clampY, cameraPosition.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, mapSize * 2);
    }
}
