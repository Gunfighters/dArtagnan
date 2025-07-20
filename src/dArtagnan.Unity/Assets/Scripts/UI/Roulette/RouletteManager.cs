using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    [SerializeField] private GameObject roulettePrefab;
    [SerializeField] private List<int> accuracyPool;
    [SerializeField] private List<RouletteSlot> slots;
    [SerializeField] private SpriteCollection GunCollection;
    [SerializeField] private int target;
    [SerializeField] private bool Spinned;
    [SerializeField] private float slotPadding;
    private float SlotAngle => 360f / slots.Count;
    private float HalfSlotAngle => SlotAngle / 2;
    private float HalfSlotAngleWithPadding => HalfSlotAngle * (1 - slotPadding);
    [SerializeField] private int rotateSpeed;
    [SerializeField] private float spinDuration;
    
    private Coroutine autoSpinCoroutine;

    public void Awake()
    {
        PacketChannel.On<YourAccuracyAndPool>(e => SetAccuracyPool(e.AccuracyPool));
        PacketChannel.On<YourAccuracyAndPool>(e => SetTarget(e.YourAccuracy));
        slots = GetComponentsInChildren<RouletteSlot>().ToList();
    }

    private void OnEnable()
    {
        // 룰렛 화면이 활성화될 때 자동으로 초기화
        ResetRoulette();
        Debug.Log("[룰렛] 화면 활성화 - 자동 초기화");
    }

    private void OnDisable()
    {
        // 룰렛 화면이 비활성화될 때 자동으로 정리
        StopAutoSpinCoroutine();
        Debug.Log("[룰렛] 화면 비활성화 - 자동 정리");
    }

    public void SetAccuracyPool(List<int> pool)
    {
        accuracyPool = pool;
        for (var i = 0; i < accuracyPool.Count; i++)
        {
            slots[i].SetItem(GunCollection.GunSpriteByAccuracy(accuracyPool[i]));
            slots[i].SetSlotText($"{accuracyPool[i]}%");
        }
    }

    public void SetTarget(int targetAccuracy)
    {
        target = targetAccuracy;
        // 초 후 자동 스핀 시작
        autoSpinCoroutine = StartCoroutine(AutoSpinAfterSeconds());
    }

    public void Spin()
    {
        if (Spinned) return;
        
        StopAutoSpinCoroutine();
        
        var selectedIndex = accuracyPool.IndexOf(target);
        if (selectedIndex == -1)
        {
            Debug.LogError($"Not found among the slots: {target}");
            return;
        }
        var angle = SlotAngle * selectedIndex * -1;
        var leftOffset = (angle - HalfSlotAngleWithPadding) % 360;
        var rightOffset = (angle + HalfSlotAngleWithPadding) % 360;
        var randomAngle = Random.Range(leftOffset, rightOffset);
        var targetAngle = randomAngle + 360 * spinDuration * rotateSpeed;
        Spinned = true;
        OnSpin(targetAngle).Forget();
    }

    private async UniTask OnSpin(float end)
    {
        float current = 0;
        float progress = 0;
        while (progress < 1)
        {
            current += Time.deltaTime;
            progress = current / spinDuration;
            var z = Mathf.Lerp(0, end, RouletteProgressFormula(progress));
            roulettePrefab.transform.rotation = Quaternion.Euler(0, 0, z);
            await UniTask.WaitForEndOfFrame();
        }
        PacketChannel.Raise(new RouletteDone());
    }

    private IEnumerator AutoSpinAfterSeconds()
    {
        yield return new WaitForSeconds(5f);
        
        if (!Spinned)
        {
            Spin();
        }
    }

    private void StopAutoSpinCoroutine()
    {
        if (autoSpinCoroutine != null)
        {
            StopCoroutine(autoSpinCoroutine);
            autoSpinCoroutine = null;
        }
    }

    private void ResetRoulette()
    {
        Spinned = false;
        StopAutoSpinCoroutine();
        roulettePrefab.transform.rotation = Quaternion.identity;
    }

    public static float RouletteProgressFormula(float progress)
    {
        return 1 - Mathf.Pow(1 - progress, 4); // y = 1 - (1 - x) ^ 4
    }
}