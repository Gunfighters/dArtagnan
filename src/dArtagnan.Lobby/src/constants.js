// 코스튬 관련 상수
export const ShopBox1 = {
    // 뽑기 비용 (골드)
    BOX1_COST: 50,

    // 룰렛 풀 크기
    ROULETTE_POOL_SIZE: 8,

    /**
     * 티어별 코스튬 목록
     * - 빈 티어가 있어도 됩니다. (서버에서 자동으로 제외 후 재정규화)
     */
    TIERS: {
        COMMON: [2, 3, 4, 5, 6, 7],
        RARE: [8, 9, 10, 11],
        EPIC: [12, 13, 14],
        LEGENDARY: [15]
    },

    /**
     * 티어 가중치
     * - 합계가 1.0이 아니어도 됩니다. (서버에서 자동 정규화)
     * - 0 또는 음수 가중치는 무시됩니다.
     */
    TIER_WEIGHTS: {
        COMMON: 0.5,
        RARE: 0.3,
        EPIC: 0.15,
        LEGENDARY: 0.05
    }
};
