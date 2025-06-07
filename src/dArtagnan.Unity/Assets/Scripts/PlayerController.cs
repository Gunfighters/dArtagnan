using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

/// <summary>
///     플레이어 한 명을 조종합니다.
/// </summary>
[RequireComponent(typeof(Character4D))]
public class PlayerController : MonoBehaviour
{
    public float range;
    public int Accuracy;
    private Vector3 currentDirection;
    private bool dead;
    private float directionLerpSpeed = 10f;
    private bool firing;
    private bool running;
    private float speed = 1f;
    private Character4D SpriteManager;
    private Vector3 targetDirection;

    private void Start()
    {
        SpriteManager = GetComponent<Character4D>();
        SpriteManager.SetState(CharacterState.Idle);
    }

    private void Update()
    {
        currentDirection = targetDirection == Vector3.zero
            ? Vector3.zero
            : Vector3.Lerp(currentDirection, targetDirection, Time.deltaTime * directionLerpSpeed);
        transform.position += (running ? speed * 4 : speed) * Time.deltaTime * currentDirection;
        if (currentDirection == Vector3.zero)
        {
            SpriteManager.SetState(CharacterState.Idle);
        }
        else
        {
            SpriteManager.SetDirection(SnapToCardinalDirection(currentDirection));
            SpriteManager.SetState(running ? CharacterState.Run : CharacterState.Walk);
        }
    }

    public void SetDirection(Vector3 normalizedDirection)
    {
        targetDirection = normalizedDirection;
    }

    public void ImmediatelyMoveTo(Vector3 position)
    {
        transform.position = position;
    }

    public void SetRunning(bool isRunning)
    {
        running = isRunning;
    }

    public void Fire()
    {
        firing = true;
    }

    public void Die()
    {
        dead = true;
    }

    private static Vector3 SnapToCardinalDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return Vector3.zero;

        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        return angle switch
        {
            >= 45f and < 135f => Vector3.up,
            >= 135f and < 225f => Vector3.left,
            >= 225f and < 315f => Vector3.down,
            _ => Vector3.right
        };
    }
}