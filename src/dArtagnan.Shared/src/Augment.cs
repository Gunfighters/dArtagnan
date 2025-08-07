using System;
using System.Collections.Generic;
using System.Linq;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 증강 ID 열거형 (1000번대 사용)
    /// </summary>
    public enum AugmentId
    {
        None = -1,
        
        // 증강 목록 (1000번대)
        AccuracyStateDoubleApplication = 1001, // AccuracyState 두 배 적용
        HalfBettingCost = 1002,                // 나만 베팅금 절반
        MaxEnergyIncrease = 1003,              // 최대 행동력 1칸 증가
        DoubleMoneySteakOnKill = 1004,         // 죽일 때 두 배로 뺏어옴
    }

    /// <summary>
    /// 증강 정보를 담는 구조체
    /// </summary>
    [Serializable]
    public struct AugmentData
    {
        public AugmentId Id;
        public string Name;
        public string Description;
        public int Weight; // 선택 가중치

        public AugmentData(AugmentId id, string name, string description, int weight)
        {
            Id = id;
            Name = name;
            Description = description;
            Weight = weight;
        }
    }

    /// <summary>
    /// 증강 관련 상수 및 데이터
    /// </summary>
    public static class AugmentConstants
    {
        // 증강 효과 관련 상수
        public const int ACCURACY_STATE_DOUBLE_MULTIPLIER = 2;
        public const float HALF_BETTING_COST_MULTIPLIER = 0.5f;
        public const int MAX_ENERGY_INCREASE_AMOUNT = 1;
        public const float DOUBLE_MONEY_STEAL_MULTIPLIER = 2.0f;

        // 증강 데이터 정의
        public static readonly Dictionary<AugmentId, AugmentData> Augments = new Dictionary<AugmentId, AugmentData>
        {
            { AugmentId.AccuracyStateDoubleApplication, new AugmentData(AugmentId.AccuracyStateDoubleApplication, "정확도 상태 강화", "정확도 증가/감소 효과가 2배로 적용됩니다", 3) },
            { AugmentId.HalfBettingCost, new AugmentData(AugmentId.HalfBettingCost, "베팅 할인", "베팅금을 절반만 지불합니다", 4) },
            { AugmentId.MaxEnergyIncrease, new AugmentData(AugmentId.MaxEnergyIncrease, "행동력 확장", "최대 행동력이 1 증가합니다", 5) },
            { AugmentId.DoubleMoneySteakOnKill, new AugmentData(AugmentId.DoubleMoneySteakOnKill, "강탈자", "적을 처치할 때 2배의 돈을 획득합니다", 3) },
        };

    }
}