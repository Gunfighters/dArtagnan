using System.Collections;
using dArtagnan.Shared;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int ID { get; private set; }
    public string Nickname { get; private set; }
    public TextMeshProUGUI nicknameText;
    public float Range { get; private set; }
    public int Accuracy { get; private set; }
    public Vector2 CurrentDirection { get; private set; }
    public bool Alive { get; private set; }
    public float RemainingReloadTime { get; private set; }
    public float TotalReloadTime { get; private set; }
    public float Speed { get; private set; }
    public bool Running => Speed > 40;
    public Rigidbody2D rb;
    public ModelManager modelManager;
    public TextMeshProUGUI accuracyText;
    public ReloadingTimePie reloadingTimePie;
    [CanBeNull] public Player TargetPlayer;
    public SpriteRenderer targetHighlightCircle;
    public TextMeshProUGUI HitMissText;
    [CanBeNull] private IEnumerator HitMissFader;
    public Color HitTextColor;
    public Color MissTextColor;
    public float hitMissShowingDuration;
    float LastServerUpdateTimestamp;
    public float lerpSpeed;
    private bool isCorrecting;
    private Vector2 lastUpdatedPosition;
    public float PositionCorrectionThreshold;
    public Vector2 Position => rb.position;

    private void Awake()
    {
        HighlightAsTarget(false);
    }

    private void Update()
    {
        UpdateModel();
        UpdateRemainingReloadTime(RemainingReloadTime - Time.deltaTime);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(NextPosition());
    }

    public void ToggleUIOverHead(bool show)
    {
        accuracyText.gameObject.SetActive(show);
        reloadingTimePie.gameObject.SetActive(show);
        HitMissText.gameObject.SetActive(show);
    }

    public void SetNickname(string newNickname)
    {
        Nickname = newNickname;
        nicknameText.text = Nickname;
    }

    public void SetAlive(bool alive)
    {
        Alive = alive;
        if (alive)
        {
            modelManager.Idle();
        }
        else
        {
            modelManager.Die();
            ScheduleDeactivation();
        }
    }
    
    private void ScheduleDeactivation()
    {
        StartCoroutine(Deactivate(1.5f));
    }

    IEnumerator Deactivate(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private void UpdateModel()
    {
        if (!Alive) return;
        if (CurrentDirection == Vector2.zero)
        {
            modelManager.Idle();
        }
        else
        {
            if (Running)
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
        modelManager.SetDirection(target.Position - rb.position);
        modelManager.Fire();
        modelManager.ShowTrajectory(target.transform);
        modelManager.ScheduleHideTrajectory();
    }
    
    public void SetAccuracy(int newAccuracy)
    {
        Accuracy = newAccuracy;
        accuracyText.text = $"{Accuracy}%";
    }

    public void UpdateRemainingReloadTime(float newRemainingReloadTime)
    {
        RemainingReloadTime = Mathf.Max(0, newRemainingReloadTime);
    }

    public void ShowHitOrMiss(bool hit)
    {
        if (HitMissFader != null)
        {
            StopCoroutine(HitMissFader);
        }

        HitMissText.text = hit ? "HIT!" : "MISS!";
        HitMissText.color = hit ? HitTextColor : MissTextColor;
        HitMissText.enabled = true;
        HitMissFader = FadeOutHitMissShowing();
        StartCoroutine(HitMissFader);
    }

    private IEnumerator FadeOutHitMissShowing()
    {
        yield return new WaitForSeconds(hitMissShowingDuration);
        HitMissText.enabled = false;
    }

    public void SetDirection(Vector2 direction)
    {
        CurrentDirection = direction;
        modelManager.SetDirection(direction);
    }

    public void SetRunning(bool speed)
    {
        Speed = speed ? Constants.RUNNING_SPEED : Constants.WALKING_SPEED;
    }
    public void UpdateMovementDataForReckoning(Vector2 direction, Vector2 position, float speed)
    {
        SetDirection(direction.normalized);
        Speed = speed;
        lastUpdatedPosition = position;
        LastServerUpdateTimestamp = Time.time;
        isCorrecting = true;
    }

    private Vector2 NextPosition()
    {
        if (!isCorrecting) return rb.position + Speed * Time.fixedDeltaTime * CurrentDirection;
        var elapsed = Time.time - LastServerUpdateTimestamp;
        var predictedPosition = lastUpdatedPosition + Speed * elapsed * CurrentDirection;
        isCorrecting = Vector2.Distance(rb.position, predictedPosition) < 0.01f;
        return Vector2.MoveTowards(rb.position, predictedPosition, Speed * Time.fixedDeltaTime * lerpSpeed);
    }

    private void ImmediatelyMoveTo(Vector2 position)
    {
        transform.position = position;
        lastUpdatedPosition = position;
        LastServerUpdateTimestamp = Time.time;
        isCorrecting = false;
    }

    public void HighlightAsTarget(bool show)
    {
        targetHighlightCircle.enabled = show;
    }

    public void Aim([CanBeNull] Player target)
    {
        TargetPlayer = target;
        if (target is null)
            modelManager.HideTrajectory();
        else
            modelManager.ShowTrajectory(target.transform, true);
    }

    public void Initialize(PlayerInformation info)
    {
        Reset();
        ID = info.PlayerId;
        SetNickname(info.Nickname);
        SetAlive(info.Alive);
        SetAccuracy(info.Accuracy);
        Speed = info.MovementData.Speed;
        lastUpdatedPosition = VecConverter.ToUnityVec(info.MovementData.Position); 
        ImmediatelyMoveTo(lastUpdatedPosition);
        SetDirection(DirectionHelperClient.IntToDirection(info.MovementData.Direction));
        isCorrecting = false;
        Range = info.Range;
        TotalReloadTime = info.TotalReloadTime;
        RemainingReloadTime = info.RemainingReloadTime;
    }

    public void Reset()
    {
        modelManager.ResetModel();
    }

    public bool CanShoot(Player target)
    {
        var mask = LayerMask.GetMask("RemotePlayer", "Obstacle");
        var hit = Physics2D.Raycast(Position, target.Position - Position, Range, mask);
        return hit.transform == target.transform;
    }
}