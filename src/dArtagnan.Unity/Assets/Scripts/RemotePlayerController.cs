using System;
using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Common;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RemotePlayerController : Player
{
    float LastServerUpdateTimestamp;
    public float lerpSpeed;
    private bool isCorrecting;
    
    void Awake()
    {
        SpriteManager = GetComponent<Character4D>();
        HighlightAsTarget(false);
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        ReckonMovement();
    }

    private void Update()
    {
        SetCharacterMovementAnimation();
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
            rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * (Vector2) currentDirection);
        }
    }

    public void HighlightAsTarget(bool show)
    {
        targetHighlightCircle.SetActive(show);
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
}
