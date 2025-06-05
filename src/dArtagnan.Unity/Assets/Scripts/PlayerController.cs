using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

/// <summary>
///     플레이어 한 명을 조종합니다.
/// </summary>
[RequireComponent(typeof(Character4D))]
public class PlayerController : MonoBehaviour
{
    public float speed;
    private Vector3 _direction;
    private bool firing;
    private bool running;
    private Character4D SpriteManager;

    private void Start()
    {
        SpriteManager = GetComponent<Character4D>();
        SetDirectionTowards(transform.position);
        SpriteManager.SetState(CharacterState.Idle);
    }

    private void Update()
    {
        if (_direction != Vector3.zero)
        {
            transform.position += Time.deltaTime * speed * _direction;
            var cardinalized = SnapToCardinalDirection(_direction);
            SpriteManager.SetDirection(cardinalized);
            SpriteManager.SetState(running ? CharacterState.Run : CharacterState.Walk);
        }
        else
        {
            SpriteManager.SetState(CharacterState.Idle);
        }

        if (firing)
        {
            firing = false;
            SpriteManager.Fire();
        }
    }

    public void SetDirectionTowards(Vector3 destination)
    {
        _direction = (destination - transform.position).normalized;
    }

    public void StopMoving()
    {
        _direction = Vector3.zero;
        running = false;
        speed = 1f;
    }

    public void SetRunning()
    {
        running = true;
        speed = 4f;
    }

    public void Fire()
    {
        firing = true;
    }


    private static Vector2 SnapToCardinalDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.zero;

        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        return angle switch
        {
            >= 45f and < 135f => Vector2.up,
            >= 135f and < 225f => Vector2.left,
            >= 225f and < 315f => Vector2.down,
            _ => Vector2.right
        };
    }
}