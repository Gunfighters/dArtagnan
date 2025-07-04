using Assets.HeroEditor4D.Common.Scripts.Common;
using UnityEngine;

public class LocalPlayerController : Player
{
    public VariableJoystick movementJoystick;
    public ShootJoystickController shootJoystickController;
    private Vector2 lastDirection;
    public static LocalPlayerController Instance { get; private set; }
    static float WALKING_SPEED = 40f;
    static float RUNNING_SPEED = 160f; // TODO: remove hard coding

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        movementJoystick.SetActive(true);
        shootJoystickController.gameObject.SetActive(true);
    }
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * currentDirection);
    }

    void Update()
    {
        if (!dead)
        {
            HandleMovementInputAndUpdateOnChange();
            UpdateTarget();
        }
        UpdateModel();
    }
    
    void HandleMovementInputAndUpdateOnChange()
    {
        var direction = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector2.up;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector2.down;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector2.left;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector2.right;
        }

        direction = direction.normalized;

        bool usingJoystick = movementJoystick.Direction != Vector2.zero;

        if (usingJoystick)
        {
            direction = DirectionHelperClient.IntToDirection(DirectionHelperClient.DirectionToInt(movementJoystick.Direction));
        }

        bool needToUpdate = false;

        if (direction != lastDirection)
        {
            lastDirection = direction;
            needToUpdate = true;
        }
        currentDirection = direction;

        needToUpdate |= Input.GetKeyDown(KeyCode.Space) | Input.GetKeyUp(KeyCode.Space);

        var nowRunning = Input.GetKey(KeyCode.Space) || usingJoystick; // always run if using joystick
        speed = nowRunning ? RUNNING_SPEED : WALKING_SPEED;

        if (needToUpdate)
        {
            NetworkManager.Instance.SendPlayerMovementData(rb.position, direction, nowRunning);
        }
    }

    void UpdateTarget()
    {
        var newTarget = GetAutoTarget();
        if (TargetPlayer != newTarget)
        {
            SetTarget(newTarget);
            if (newTarget is not null)
            {
                NetworkManager.Instance.SendPlayerNewTarget(newTarget.id);
            }
        }
    }
    RemotePlayerController GetAutoTarget()
    {
        RemotePlayerController best = null;
        if (shootJoystickController.Direction == Vector2.zero) // 사거리 내 가장 가까운 적.
        {
            float minDistance = range;
            foreach (var target in GameManager.Instance.remotePlayers.Values)
            {
                if (!target.dead && Vector2.Distance(target.Position, Position) < minDistance
                    && CanShoot(target))
                {
                    minDistance = Vector2.Distance(target.Position, Position);
                    best = target;
                }
            }

            return best;
        }

        float minAngle = float.MaxValue;
        foreach (var target in GameManager.Instance.remotePlayers.Values)
        {
            if (!target.dead && Vector2.Distance(target.Position, Position) < range
                && Vector2.Angle(shootJoystickController.Direction, target.Position - Position) < minAngle
                && CanShoot(target)
               )
            {
                minAngle = Vector2.Angle(shootJoystickController.Direction, target.Position - Position);
                best = target;
            }
        }
        return best;
    }

    void SetTarget(RemotePlayerController newTarget)
    {
        TargetPlayer?.HighlightAsTarget(false);
        TargetPlayer = newTarget;
        TargetPlayer?.HighlightAsTarget(true);
    }

    bool CanShoot(RemotePlayerController target)
    {
        var mask = LayerMask.GetMask("RemotePlayer", "Obstacle");
        var hit = Physics2D.Raycast(Position, target.Position - Position, range, mask);
        return hit.transform == target.transform;
    }

    public void ShootTarget()
    {
        if (TargetPlayer is null) return;
        NetworkManager.Instance.SendPlayerShooting(TargetPlayer.id);
    }
}
