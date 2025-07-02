using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public abstract class Player : MonoBehaviour
{
    public int id;
    public float range;
    public int accuracy;
    public Vector2 serverPosition;
    public Vector2 currentDirection;
    public Vector2 Position => rb.position;
    public bool dead;
    public float cooldown;
    public float cooldownDuration;
    public float speed;
    public bool running => speed > 40;
    public Rigidbody2D rb;
    public ModelManager modelManager;
    public TextMeshProUGUI accuracyText;
    [CanBeNull] public RemotePlayerController TargetPlayer { get; protected set; }
    public SpriteRenderer targetHighlightCircle;
    public TextMeshProUGUI HitText;
    public TextMeshProUGUI Misstext;
    private TextMeshProUGUI HitMissShowing;
    private IEnumerator HitMissFader;
    
    protected static Vector3 SnapToCardinalDirection(Vector3 dir)
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

    protected void UpdateModel()
    {
        if (dead) return;
        if (currentDirection == Vector2.zero)
        {
            modelManager.Stop();
        }
        else
        {
            modelManager.SetDirection(SnapToCardinalDirection(currentDirection));
            if (running)
            {
                modelManager.Run();
            }
            else
            {
                modelManager.Walk();
            }
        }
    }

    public void Fire()
    {
        modelManager.Fire();
    }

    public void Die()
    {
        dead = true;
        modelManager.Die();
    }
    
    public void SetAccuracy(int newAccuracy)
    {
        accuracy = newAccuracy;
        accuracyText.text = $"{accuracy}%";
    }
    public void ImmediatelyMoveTo(Vector3 position)
    {
        rb.MovePosition(position);
    }

    public void ShowHitOrMiss(bool hit)
    {
        if (HitMissShowing)
        {
            StopCoroutine(HitMissFader);
            HitMissShowing.gameObject.SetActive(false);
        }
        if (hit)
        {
            HitText.gameObject.SetActive(true);
            HitMissShowing = HitText;
        }
        else
        {
            Misstext.gameObject.SetActive(true);
            HitMissShowing = Misstext;
        }

        HitMissFader = FadeOutHitMissShowing();
        StartCoroutine(HitMissFader);
    }

    IEnumerator FadeOutHitMissShowing()
    {
        float remaining = 1f;
        while (remaining > 0f)
        {
            yield return new WaitForEndOfFrame();
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            HitMissShowing.alpha = 255 * remaining;
        }
        HitMissShowing.gameObject.SetActive(false);
        HitMissShowing = null;
    }
}