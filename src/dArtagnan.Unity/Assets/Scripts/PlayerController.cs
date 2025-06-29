using System;
using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Common;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Character4D))]
public class PlayerController : MonoBehaviour
{
    public int id;
    public float range;
    [SerializeField] private int accuracy;
    [SerializeField] private Vector3 serverPosition;
    [SerializeField] private Vector3 currentDirection;
    public Vector2 faceDirection = Vector2.down;
    private float LastServerUpdateTimestamp;
    public float lerpSpeed;
    private bool isCorrecting = false;
    private float timeOfLastServerUpdate;
    public bool dead;
    private bool firing;
    public float speed;
    private bool running => speed > 40;
    private bool lerping;
    private Character4D SpriteManager;
    public GameObject textGameObject;
    private TextMeshProUGUI accuracyText;
    [SerializeField] private PlayerController targetPlayer;
    [SerializeField] private GameObject TargetCircle;
    [SerializeField] private Color color;

    private Coroutine correctionRoutine;
    private bool correcting;
    public float cooldownDuration = 15f;
    public float cooldown => GameManager.Instance.cooldown[id];
    public Image cooldownPie;
    public bool IsControlled => this == GameManager.Instance.ControlledPlayer;
    public Rigidbody2D rb;
    public Vector2 position => rb.position;
    
    void Awake()
    {
        SpriteManager = GetComponent<Character4D>();
        SpriteManager.SetState(CharacterState.Idle);
        accuracyText = textGameObject.GetComponent<TextMeshProUGUI>();
        HighlightAsTarget(false);
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        HandleMovementInformation();
    }

    private void Update()
    {
        SetCharacterDirection();
        if (firing)
        {
            firing = false;
            SpriteManager.Fire();
        }

        if (dead)
        {
            SpriteManager.SetState(CharacterState.Death);
        }
    }

    void HandleMovementInformation()
    {
        if (IsControlled)
        {
            rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * (Vector2) currentDirection);
        }
        else if (isCorrecting)
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
            rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * (Vector2) currentDirection);
        }
    }

    void SetCharacterDirection()
    {
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

    public void ShowHitOrMiss(bool hit)
    {
        // TODO
    }
    public void SetTarget(PlayerController newTarget)
    {
        targetPlayer?.HighlightAsTarget(false);
        targetPlayer = newTarget;
        targetPlayer?.HighlightAsTarget(true);
    }

    void HighlightAsTarget(bool show)
    {
        TargetCircle.SetActive(show);
    }

    public static Vector3 EstimatePositionByPing(Vector3 position, Vector3 direction, float speed)
    {
        return position + GameManager.Instance.Ping / 2 * direction * speed;
    }

    public void SetMovementInformation(Vector3 normalizedDirection, Vector3 estimatedPosition, float newSpeed)
    {
        currentDirection = normalizedDirection;
        speed = newSpeed;
        serverPosition = estimatedPosition;
        isCorrecting = true;
        LastServerUpdateTimestamp = Time.time;
    }

    public void ImmediatelyMoveTo(Vector3 position)
    {
        rb.position = position;
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
        accuracyText.text = $"{accuracy}%";
    }

    public static Vector3 SnapToCardinalDirection(Vector3 dir)
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
