using UnityEngine;

public class RemotePlayerController : Player
{
    float LastServerUpdateTimestamp;
    public float lerpSpeed;
    private bool isCorrecting;
    private Vector2 serverPosition;
    
    void Awake()
    {
        HighlightAsTarget(false);
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        ReckonMovement();
    }

    private void Update()
    {
        UpdateModel();
    }

    void ReckonMovement()
    {
        if (isCorrecting)
        {
            var predictedPosition = serverPosition + speed * (Time.time - LastServerUpdateTimestamp) * currentDirection;
            rb.MovePosition(Vector2.MoveTowards( rb.position, predictedPosition, speed * Time.fixedDeltaTime * lerpSpeed));
            if (Vector2.Distance(rb.position, predictedPosition) < 0.01f)
            {
                isCorrecting = false;
            }
        }
        else
        {
            rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * currentDirection);
        }
    }

    public void HighlightAsTarget(bool show)
    {
        targetHighlightCircle.enabled = show;
    }

    public static Vector3 EstimatePositionByPing(Vector3 position, Vector3 direction, float speed)
    {
        return position + GameManager.Instance.Ping / 2 * direction * speed;
    }

    public void SetMovementData(Vector3 normalizedDirection, Vector3 estimatedPosition, float newSpeed)
    {
        currentDirection = normalizedDirection;
        speed = newSpeed;
        serverPosition = estimatedPosition;
        isCorrecting = true;
        LastServerUpdateTimestamp = Time.time;
    }

    public new void ImmediatelyMoveTo(Vector3 position)
    {
        // rb.MovePosition(position);
        transform.position = position;
        serverPosition = position;
        isCorrecting = false;
    }
}
