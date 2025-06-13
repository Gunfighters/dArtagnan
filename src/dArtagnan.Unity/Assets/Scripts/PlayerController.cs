using System;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using TMPro;
using UnityEngine;

/// <summary>
///     플레이어 한 명을 조종합니다.
/// </summary>
[RequireComponent(typeof(Character4D))]
public class PlayerController : MonoBehaviour
{
    public int id;
    public float range { get; private set; }
    [SerializeField] private int accuracy;
    public Vector3 currentDirection;
    private bool dead;
    private float directionLerpSpeed = 10f;
    private bool firing;
    [SerializeField] private float speed;
    private bool running => speed > 1;
    private Character4D SpriteManager;
    public GameObject textGameObject;
    // private TextMeshPro textMeshPro;
    // private LineRenderer rangeCircleRenderer;

    private void Start()
    {
        SpriteManager = GetComponent<Character4D>();
        SpriteManager.SetState(CharacterState.Idle);
        // textMeshPro = textGameObject.GetComponent<TextMeshPro>();
        range = 5;

        // var rangeCirclePoints = 36;
        // rangeCircleRenderer = gameObject.GetComponent<LineRenderer>();
        // rangeCircleRenderer.loop = true;
        // rangeCircleRenderer.useWorldSpace = false;
        // rangeCircleRenderer.positionCount = rangeCirclePoints;
        // var points = new Vector3[rangeCirclePoints];
        // for (var i = 0; i < rangeCirclePoints; i++)
        // {
        //     var angle = 2 * Mathf.PI * i / rangeCirclePoints;
        //     points[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * range;
        // }
        // rangeCircleRenderer.SetPositions(points);
    }

    private void Update()
    {
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

        if (firing)
        {
            firing = false;
            SpriteManager.Fire();
        }
    }

    public void SetDirection(Vector3 normalizedDirection)
    {
        currentDirection = normalizedDirection;
    }

    public void ImmediatelyMoveTo(Vector3 position)
    {
        transform.position = position;
    }

    public void Fire()
    {
        firing = true;
    }

    public void Die()
    {
        dead = true;
    }

    public void SetAccuracy(int newAccuracy)
    {
        accuracy = newAccuracy;
        // textMeshPro.SetText($"{accuracy}%");
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
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