using System;
using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Character4D))]
public class PlayerController : MonoBehaviour
{
    public int id;
    public float range { get; private set; }
    [SerializeField] private int accuracy;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private Vector3 currentDirection;
    [SerializeField] private float RTT;
    private float timeOfLastServerUpdate;
    private bool dead;
    public float lerpSpeed = 0.5f;
    private bool firing;
    [SerializeField] private float speed;
    private bool running => speed > 1;
    private bool lerping;
    private Character4D SpriteManager;
    public GameObject textGameObject;

    private Coroutine correctionRoutine;
    private bool correcting;

    private void Start()
    {
        SpriteManager = GetComponent<Character4D>();
        SpriteManager.SetState(CharacterState.Idle);
        range = 5;
    }

    private void Update()
    {
        if (!lerping)
        {
            transform.position += speed * Time.deltaTime * currentDirection;
        }
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

    public void UpdatePosition(Vector3 positionFromServer)
    {
        targetPosition = positionFromServer + speed * RTT * currentDirection;
        StartCoroutine(LerpToTargetPosition(targetPosition));
    }

    IEnumerator LerpToTargetPosition(Vector3 targetPosition)
    {
        lerping = true;
        var wf = new WaitForEndOfFrame();
        while (transform.position != targetPosition)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed);
            yield return wf;
        }

        lerping = false;
    }

    public void SetPing(Ping p)
    {
        RTT = (float) p.time / 1000;
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
