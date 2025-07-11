using System.Collections;
using Cysharp.Threading.Tasks;
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
    public int Balance { get; private set; }
    public Vector2 CurrentDirection { get; private set; }
    public bool Alive { get; private set; }
    public float RemainingReloadTime { get; private set; }
    public float TotalReloadTime { get; private set; }
    public float Speed { get; private set; }
    public bool Running;
    public Rigidbody2D rb;
    public ModelManager modelManager;
    public TextMeshProUGUI accuracyText;
    public ReloadingTimePie reloadingTimePie;
    [CanBeNull] public Player TargetPlayer;
    public SpriteRenderer targetHighlightCircle;
    public TextMeshProUGUI HitMissText;
    public TextMeshProUGUI BalanceText;
    [CanBeNull] private IEnumerator HitMissFader;
    public Color HitTextColor;
    public Color MissTextColor;
    public float hitMissShowingDuration;
    float LastServerUpdateTimestamp;
    public float lerpSpeed;
    private bool needToCorrect;
    private Vector2 lastUpdatedPosition;
    public float PositionCorrectionThreshold;
    public Vector2 Position => rb.position;
    private Vector2 faceDirection;
    private bool moving;

    // ID에 따른 플레이어 색깔 (어두운 배경에서 잘 보이도록 조정)
    private static readonly Color[] PlayerColors = {
        new(1f, 0.3f, 0.3f),   // 밝은 빨강 - ID 1
        new(0.4f, 0.7f, 1f),   // 밝은 파랑 - ID 2  
        new(0.4f, 1f, 0.4f),   // 밝은 초록 - ID 3
        new(1f, 0.8f, 0.2f),   // 밝은 주황 - ID 4 (노란색 대체)
        new(1f, 0.4f, 1f),     // 밝은 자홍 - ID 5
        new(0.4f, 1f, 1f),     // 밝은 시안 - ID 6
        new(1f, 0.6f, 0.2f),   // 따뜻한 주황 - ID 7
        new(0.8f, 0.4f, 1f)    // 밝은 보라 - ID 8
    };

    public Color MyColor => ID >= 1 && ID <= 8 ? PlayerColors[ID - 1] : Color.white;

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
        if (Alive)
        {
            var nextPosition = NextPosition();
            moving = nextPosition != rb.position;
            rb.MovePosition(nextPosition);
        }
    }

    public void ToggleUIInGame(bool show)
    {
        accuracyText.gameObject.SetActive(show);
        reloadingTimePie.gameObject.SetActive(show);
        HitMissText.gameObject.SetActive(show);
        BalanceText.gameObject.SetActive(show);
    }

    public void SetNickname(string newNickname)
    {
        Nickname = newNickname;
        nicknameText.text = Nickname;
        
        // ID에 따른 색깔 설정
        nicknameText.color = MyColor;
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
        }
    }
    
    private void UpdateModel()
    {
        if (!Alive) return;
        SetFaceDirection(faceDirection);
        if (!moving)
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

    public void SetBalance(int newBalance)
    {
        var gain = newBalance > Balance;
        Balance = newBalance;
        BalanceText.text = $"${Balance}";
        BalanceText.color = gain ? Color.green : Color.red;
        ResetBalanceTextColor().Forget();
    }

    private async UniTaskVoid ResetBalanceTextColor()
    {
        await UniTask.WaitForSeconds(0.5f);
        BalanceText.color = Color.white;
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
        SetFaceDirection(direction);
    }

    public void SetFaceDirection(Vector2 direction)
    {
        modelManager.SetDirection(direction);
    }

    public void SetRunning(bool running)
    {
        Running = running;
        Speed = running ? Constants.RUNNING_SPEED : Constants.WALKING_SPEED;
    }
    public void UpdateMovementDataForReckoning(Vector2 direction, Vector2 position, float speed)
    {
        SetDirection(direction.normalized);
        Speed = speed;
        lastUpdatedPosition = position;
        LastServerUpdateTimestamp = Time.time;
        needToCorrect = true;
    }

    private Vector2 NextPosition()
    {
        if (!needToCorrect) return rb.position + Speed * Time.fixedDeltaTime * CurrentDirection;
        var elapsed = Time.time - LastServerUpdateTimestamp;
        var predictedPosition = lastUpdatedPosition + Speed * elapsed * CurrentDirection;
        var diff = Vector2.Distance(rb.position, predictedPosition);
        needToCorrect = diff > 0.01f;
        if (diff > PositionCorrectionThreshold) return predictedPosition;
        var correctionSpeed = Mathf.Max(Speed, Constants.RUNNING_SPEED) * lerpSpeed;
        if (diff > correctionSpeed)
        {
            SetFaceDirection(predictedPosition - rb.position);
            SetRunning(true);
        }
        return Vector2.MoveTowards(rb.position, predictedPosition, correctionSpeed * Time.fixedDeltaTime);
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
        modelManager.ResetModel();
        ID = info.PlayerId;
        SetNickname(info.Nickname);
        SetBalance(info.Balance);
        SetAlive(info.Alive);
        SetAccuracy(info.Accuracy);
        Speed = info.MovementData.Speed;
        transform.position = VecConverter.ToUnityVec(info.MovementData.Position);
        SetDirection(DirectionHelperClient.IntToDirection(info.MovementData.Direction));
        Range = info.Range;
        TotalReloadTime = info.TotalReloadTime;
        RemainingReloadTime = info.RemainingReloadTime;
    }

    public bool CanShoot(Player target)
    {
        var mask = LayerMask.GetMask("RemotePlayer", "Obstacle");
        var hit = Physics2D.Raycast(Position, target.Position - Position, Range, mask);
        return hit.transform == target.transform;
    }
}