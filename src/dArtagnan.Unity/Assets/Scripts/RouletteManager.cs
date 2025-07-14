using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance;
    [SerializeField] private GameObject roulettePrefab;
    [SerializeField] private List<int> accuracyPool;
    [SerializeField] private List<RouletteSlot> slots;
    [SerializeField] private SpriteCollection GunCollection;
    [SerializeField] private int target;
    [SerializeField] private bool Spinning;
    [SerializeField] private float slotPadding;
    private float SlotAngle => 360f / slots.Count;
    private float HalfSlotAngle => SlotAngle / 2;
    private float HalfSlotAngleWithPadding => HalfSlotAngle * (1 - slotPadding);
    [SerializeField] private int rotateSpeed;
    [SerializeField] private float spinDuration;

    private void Awake()
    {
        Instance = this;
        slots = GetComponentsInChildren<RouletteSlot>().ToList();
    }

    public void Start()
    {
        Debug.Log("Showing AccuracyRoulette...");
        SetAccuracyPool(new List<int> { 1, 25, 58, 88, 90, 74, 22, 3 });
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
    }

    public void Spin()
    {
        if (Spinning) return;
        var selectedIndex = accuracyPool.IndexOf(target);
        if (selectedIndex == -1)
        {
            Debug.LogError($"Not found among the slots: {target}");
            return;
        }
        var angle = SlotAngle * selectedIndex;
        var leftOffset = (angle - HalfSlotAngleWithPadding) % 360;
        var rightOffset = (angle + HalfSlotAngleWithPadding) % 360;
        var randomAngle = Random.Range(leftOffset, rightOffset);
        var targetAngle = randomAngle + 360 * spinDuration * rotateSpeed;
        Spinning = true;
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

        Spinning = false;
    }

    public static float RouletteProgressFormula(float progress)
    {
        return 1 - Mathf.Pow(1 - progress, 4); // y = 1 - (1 - x) ^ 4
    }
}