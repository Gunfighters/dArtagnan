using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using dArtagnan.Shared;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public abstract class Player : MonoBehaviour
{
    public int id;
    public string nickname { get; private set; }
    public TextMeshProUGUI nicknameText;
    public float range;
    public int accuracy;
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
    public CooldownPie cooldownPie;
    [CanBeNull] public RemotePlayerController TargetPlayer { get; protected set; }
    public SpriteRenderer targetHighlightCircle;
    public TextMeshProUGUI HitText;
    public TextMeshProUGUI Misstext;
    private TextMeshProUGUI HitMissShowing;
    private IEnumerator HitMissFader;

    public void ToggleUIOverHead(bool show)
    {
        accuracyText.gameObject.SetActive(show);
        cooldownPie.gameObject.SetActive(show);
        HitText.gameObject.SetActive(show);
        Misstext.gameObject.SetActive(show);
    }

    public void SetNickname(string newNickname)
    {
        nickname = newNickname;
        nicknameText.text = nickname;
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
            modelManager.SetDirection(currentDirection);
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

    public void Fire(Player target)
    {
        modelManager.SetDirection((Vector2) target.transform.position - rb.position);
        modelManager.Fire();
        modelManager.ShowTrajectory(target.transform.position);
        modelManager.ScheduleHideTrajectory();
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

    public void ImmediatelyMoveTo(Vector2 position)
    {
        Debug.Log($"Immediately moving to {position}");
        transform.position = position;
        // rb.MovePosition(position);
    }

    public void ShowHitOrMiss(bool hit)
    {
        if (HitMissShowing)
        {
            StopCoroutine(HitMissFader);
            HitMissShowing.enabled = false;
        }
        if (hit)
        {
            HitText.enabled = true;
            HitMissShowing = HitText;
        }
        else
        {
            Misstext.enabled = true;
            HitMissShowing = Misstext;
        }

        HitMissFader = FadeOutHitMissShowing();
        StartCoroutine(HitMissFader);
    }

    IEnumerator FadeOutHitMissShowing()
    {
        yield return new WaitForSeconds(1);
        HitMissShowing.enabled = false;
        HitMissShowing = null;
    }

    public void SetAsInfo(PlayerInformation info)
    {
        id = info.PlayerId;
        nickname = info.Nickname;
        accuracy = info.Accuracy;
        cooldownDuration = info.TotalReloadTime;
        cooldown = info.RemainingReloadTime;
        dead = !info.Alive;
        range = info.Range;
        currentDirection = DirectionHelperClient.IntToDirection(info.MovementData.Direction);
        ImmediatelyMoveTo(new Vector2(info.MovementData.Position.X, info.MovementData.Position.Y));
        speed = info.MovementData.Speed;
    }
}