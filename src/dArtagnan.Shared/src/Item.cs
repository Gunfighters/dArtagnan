using System;
using System.Collections.Generic;
using System.Linq;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 아이템 ID 열거형
    /// </summary>
    public enum ItemId
    {
        None = -1,
        SpeedBoost = 1, // 이동속도 순간 증가
        EnergyRestore = 2, // 행동력 4칸 회복
        DamageShield = 3, // 한번 총알 피해 가드
        AccuracyReset = 4, // 확률 재설정
    }

    /// <summary>
    /// 아이템 정보를 담는 구조체
    /// </summary>
    [Serializable]
    public struct ItemData
    {
        public ItemId Id;
        public string Name;
        public string Description;
        public int EnergyCost; // 소모 행동력
        public int Weight; // 획득 가중치

        public ItemData(ItemId id, string name, string description, int energyCost, int weight)
        {
            Id = id;
            Name = name;
            Description = description;
            EnergyCost = energyCost;
            Weight = weight;
        }
    }

    /// <summary>
    /// 아이템 관련 상수 및 데이터
    /// </summary>
    public static class ItemConstants
    {
        // 아이템 효과 관련 상수
        public const float SPEED_BOOST_MULTIPLIER = 1.5f; // 속도 증가 배율
        public const float SPEED_BOOST_DURATION = 5.0f; // 속도 증가 지속시간 (초)
        public const int ENERGY_RESTORE_AMOUNT = 4; // 에너지 회복량

        // 아이템 데이터 정의
        public static readonly Dictionary<ItemId, ItemData> Items = new Dictionary<ItemId, ItemData>
        {
            { ItemId.SpeedBoost, new ItemData(ItemId.SpeedBoost, "속도 증가", "5초간 이동속도 1.5배 증가", 1, 3) },
            { ItemId.EnergyRestore, new ItemData(ItemId.EnergyRestore, "행동력 회복", "행동력 4칸 즉시 회복", 0, 3) },
            { ItemId.DamageShield, new ItemData(ItemId.DamageShield, "피해 가드", "다음 피격 1회 무효화", 2, 3) },
            { ItemId.AccuracyReset, new ItemData(ItemId.AccuracyReset, "확률 재설정", "정확도를 25~75 사이로 재설정", 3, 3) },
        };
    }
}