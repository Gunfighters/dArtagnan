using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public List<int> accuracyPool;
    public List<RouletteSlot> slots;
    public SpriteCollection GunCollection;

    private void Awake()
    {
        slots = GetComponentsInChildren<RouletteSlot>().ToList();
    }

    public void SetAccuracyPool(List<int> pool)
    {
        accuracyPool = pool;
        for (var i = 0; i < accuracyPool.Count; i++)
        {
            slots[i].SetItemImage(GunSpriteByAccuracy(accuracyPool[i]));
            slots[i].SetSlotText($"{accuracyPool[i]}%");
        }
    }

    private ItemSprite GunSpriteByAccuracy(int accuracy)
    {
        return accuracy switch
        {
            <= 25 => GunCollection.Firearm1H.Single(s => s.Name == "Anaconda"),
            <= 50 => GunCollection.Firearm1H.Single(s => s.Name == "DesertEagle"),
            <= 75 => GunCollection.Firearm2H.Single(s => s.Name == "AK47"),
            _ => GunCollection.Firearm2H.Single(s => s.Name == "Widowmaker")
        };
    }
}