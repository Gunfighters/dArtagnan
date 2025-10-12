using System;
using System.Collections.Generic;
using System.Linq;

namespace dArtagnan.Shared
{
    public enum ItemId
    {
        None = 0,
        DebuffRange = 1,
        DebuffTotalReloadTime = 2,
        GiftBox = 3,
        NewRoulette = 4,
        Magazine = 5,
        FastReload = 6,
        Boots = 7,
        AccuracyBooster = 8,
        Berserker = 9,
        Overflow = 10,
        IllegalMod = 11,
        Scope = 12,
        SteadyAim = 13,
        HideAccuracy = 14,
        DavidGoliath = 15,
        VipTicket = 16,
        LifeInsurance = 17,
        CreditCard = 18,
        RegularDiscount = 19,
        Robbery = 20,
        BodyArmor = 21,
        FearBullet = 22,
        Beret = 23,
        GreatMiner = 24,
    }

    /// <summary>
    /// 아이템 태그 (서버 구현 관점) - 비트 마스크로 여러 태그 조합 가능
    /// </summary>
    [Flags]
    public enum ItemTags
    {
        None = 0,
        Debuff = 1 << 1,      // 디버프 효과
        Instant = 1 << 2,     // 즉시효과 (ownedItem에 추가되지 않음)
        Stackable = 1 << 3,   // 중첩 가능
        ShopItem = 1 << 4,    // 상점에 출현 가능 아이템
        RoundTemporary = 1 << 5, // 라운드 종료 시 자동 제거
    }

    /// <summary>
    /// 서버용 아이템 정보를 담는 구조체 (GameProtocol.cs 의 ShopItem = 클라용)
    /// </summary>
    public struct ItemData
    {
        public readonly ItemId Id;
        public readonly string Name;
        public readonly string Description;
        public readonly int BasePrice; // 기본 가격 (실제 가격은 변동 가능)
        public readonly ItemTags Tags; // 아이템 태그
        public readonly int Weight; // 등장 가중치 (낮을수록 희귀)

        public ItemData(ItemId id, string name, string description, int basePrice, ItemTags tags, int weight)
        {
            Id = id;
            Name = name;
            Description = description;
            BasePrice = basePrice;
            Tags = tags;
            Weight = weight;
        }
    }

    /// <summary>
    /// 아이템 관련 상수 및 데이터
    /// </summary>
    public static class ItemConstants
    {
        public static class SteadyAim
        {
            public const float UPDATE_INTERVAL = 1f; // 업데이트 주기
            public const float RANGE_INCREASE_PER_UPDATE = 0.05f; // 업데이트당 사거리 증가량
        }

        public static class DavidGoliath
        {
            public const float MONEY_STEAL_MULTIPLIER = 2.0f;   // 2배 빼앗기
        }

        public static class VipTicket
        {
            public const float BETTING_REDUCTION = 0.25f;
        }

        public static class LifeInsurance
        {
            public const int TIME_PERIOD = 5;           // T초 주기로 비용 지불
            public const int COST_PER_SECOND = 1;       // T초당 비용
            public const int DEATH_REWARD = 25;         // 사망시 보상
        }

        public static class GiftBox
        {
            public const float GIFTBOX_CHANCE = 3f; 
            public const int GIFTBOX_REWARD = 100; 
        }

        public static class FearBullet
        {
            public const int TARGET_ACCURACY_ON_MISS = 10; // 빗나가면 상대 명중률 10%
        }

        public static class FastReload
        {
            public const float RELOAD_TIME_REDUCTION = 0.5f; // 0.5초 감소
        }

        public static class Boots
        {
            public const float SPEED_INCREASE = 0.1f; // 장화 1개당 속도 증가량
        }

        public static class RegularDiscount
        {
            public const float FREE_PROBABILITY = 0.5f; // 50% 확률로 0원
        }

        public static class GoldenShovel
        {
            public const float REWARD_MULTIPLIER = 2f; // 황금삽 보상 배수
        }

        public static class InterestFarm
        {
            public const float INTEREST_RATE = 0.25f;
        }

        public static class Underflow
        {
            public const float FURY_SPEED_MULTIPLIER = 1.15f; 
        }

        // 아이템 데이터 정의
        public static readonly Dictionary<ItemId, ItemData> Items = new Dictionary<ItemId, ItemData>
        {
            // 기타
            { ItemId.None, new ItemData(ItemId.None, "없음", "없음", 0, ItemTags.None, 0) },
            { ItemId.DebuffRange, new ItemData(ItemId.DebuffRange, "사막의 신기루", "사거리가 명중률에 반비례합니다", 0, ItemTags.Debuff, 0) },
            { ItemId.DebuffTotalReloadTime, new ItemData(ItemId.DebuffTotalReloadTime, "손떨림의 저주", "재장전 시간이 명중률에 비례합니다", 0, ItemTags.Debuff, 0) },
            
            // 즉시효과 (Instant)
            { ItemId.GiftBox, new ItemData(ItemId.GiftBox, "골드러쉬", "매우 낮은 확률로 매우 큰 돈을 얻습니다", 1, ItemTags.ShopItem | ItemTags.Instant, 3) },
            { ItemId.NewRoulette, new ItemData(ItemId.NewRoulette, "살룬의 재도전", "돌림판을 새것으로 교체합니다.", 1, ItemTags.ShopItem | ItemTags.Instant, 12) },
            { ItemId.Scope, new ItemData(ItemId.Scope, "보안관의 조준경", "이제는 명중률이 높아도 사거리가 짧아지지 않습니다", 6, ItemTags.ShopItem | ItemTags.Instant, 2) },

            // 중첩 가능 (Stackable)
            { ItemId.Magazine, new ItemData(ItemId.Magazine, "보급 탄창", "총을 한번 더 쏠 수 있게 됩니다 (일회용)", 7, ItemTags.ShopItem, 2) },
            { ItemId.FastReload, new ItemData(ItemId.FastReload, "총잡이의 손놀림", $"장전 시간이 {FastReload.RELOAD_TIME_REDUCTION}초 감소합니다 (중첩 가능)", 5, ItemTags.ShopItem | ItemTags.Stackable, 2) },
            { ItemId.Boots, new ItemData(ItemId.Boots, "모래폭풍 장화", $"이동속도가 약간 증가합니다 (중첩 가능)", 5, ItemTags.ShopItem | ItemTags.Stackable, 2) },
            { ItemId.AccuracyBooster, new ItemData(ItemId.AccuracyBooster, "이중 방아쇠", "증감버튼의 효과가 2배가 됩니다 (중첩 가능) (Todo: 미구현)", 8, ItemTags.Stackable, 2) },

            // 중첩 불가능 (NonStackable)
            { ItemId.Robbery, new ItemData(ItemId.Robbery, "강도왕", "적을 쏴서 죽이면, 그 적의 아이템을 아무거나 하나 빼앗습니다 (일회용)", 5, ItemTags.ShopItem, 2) },
            { ItemId.BodyArmor, new ItemData(ItemId.BodyArmor, "철갑 조끼", "다음 라운드에서만 한번 총을 막아주는 방탄조끼를 장착합니다 (일화용)", 12, ItemTags.ShopItem | ItemTags.RoundTemporary, 1) },
            { ItemId.FearBullet, new ItemData(ItemId.FearBullet, "공포탄", $"적을 쐈는데 빗나가면, 그 적의 명중률이 {FearBullet.TARGET_ACCURACY_ON_MISS}%가 됩니다 (일회용)", 3, ItemTags.ShopItem, 3) },
            { ItemId.Berserker, new ItemData(ItemId.Berserker, "광기의 연료", $"파산 직전일 때 이동속도가 더 증가합니다!", 7, ItemTags.ShopItem, 2) },
            { ItemId.Overflow, new ItemData(ItemId.Overflow, "태양의 분노", "(Todo: 미구현)", 3, ItemTags.None, 2) },
            { ItemId.VipTicket, new ItemData(ItemId.VipTicket, "황금 보증서", $"내 징수금의 양이 {(int)(VipTicket.BETTING_REDUCTION * 100)}% 감소합니다", 6, ItemTags.ShopItem, 2) },
            { ItemId.Beret, new ItemData(ItemId.Beret, "크리스마스 카우보이", "모자를 산타모자로 바꿉니다(Todo: 미구현)", 0, ItemTags.None, 5) },
            { ItemId.DavidGoliath, new ItemData(ItemId.DavidGoliath, "약자의 복수", $"나보다 명중률이 높은 사람을 처치하면 평소보다 소지금을 {DavidGoliath.MONEY_STEAL_MULTIPLIER}배 빼앗습니다", 6, ItemTags.ShopItem, 3) },
            { ItemId.LifeInsurance, new ItemData(ItemId.LifeInsurance, "죽음의 계약서", $"{LifeInsurance.TIME_PERIOD}초마다 {LifeInsurance.COST_PER_SECOND}달러를 지불합니다. 사망 시 {LifeInsurance.DEATH_REWARD}달러를 받습니다", 3, ItemTags.None, 3) },
            { ItemId.GreatMiner, new ItemData(ItemId.GreatMiner, "황금 삽", $"채굴 보상이 {(int)((GoldenShovel.REWARD_MULTIPLIER - 1) * 50)}% 증가합니다", 6, ItemTags.ShopItem, 2) },
            { ItemId.HideAccuracy, new ItemData(ItemId.HideAccuracy, "그림자 사수", "내 명중률과 사거리를 적에게 숨깁니다", 3, ItemTags.ShopItem, 4) },
            { ItemId.IllegalMod, new ItemData(ItemId.IllegalMod, "암시장 개조품", "명중률이 70%를 초과할 수 있게 됩니다 (Todo: 오버플로우 언더플로우 구현을 위해 일단 명중률 최대 100프로는 모두에게 적용해놨음 => 다른 효과로 바꾸거나 해야할 듯)", 6, ItemTags.None, 3) },
            { ItemId.SteadyAim, new ItemData(ItemId.SteadyAim, "고요한 사수", $"한 자리에 가만히 있으면 {SteadyAim.UPDATE_INTERVAL}초마다 사거리가 {SteadyAim.RANGE_INCREASE_PER_UPDATE}만큼 증가합니다. 움직이면 초기화됩니다", 5, ItemTags.ShopItem, 3) },
            { ItemId.CreditCard, new ItemData(ItemId.CreditCard, "은행가의 부적", $"라운드가 끝날 때마다 현재 소지금의 {(int)(InterestFarm.INTEREST_RATE * 100)}%를 이자로 받습니다", 4, ItemTags.ShopItem, 3) },
            { ItemId.RegularDiscount, new ItemData(ItemId.RegularDiscount, "단골 손님", $"명중률 돌림판을 돌렸을시 {(int)(RegularDiscount.FREE_PROBABILITY * 100)}% 확률로 그 다음 비용이 0원이 됩니다.", 2, ItemTags.ShopItem, 4) },
        };
    }
}