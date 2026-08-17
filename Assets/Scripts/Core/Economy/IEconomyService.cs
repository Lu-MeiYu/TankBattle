namespace TankBattle.Core.Economy
{
    public enum UpgradeType
    {
        Firepower,
        MoveSpeed
    }

    /// <summary>戰鬥結束後的表現資料，供 <see cref="IEconomyService.AwardMoney"/> 計算金錢用。</summary>
    public readonly struct BattleResult
    {
        public readonly bool IsVictory;
        public readonly int SurvivalRank;
        public readonly int TotalTanks;
        public readonly float DamageDealt;
        public readonly int KillCount;

        public BattleResult(bool isVictory, int survivalRank, int totalTanks, float damageDealt,
            int killCount)
        {
            IsVictory = isVictory;
            SurvivalRank = survivalRank;
            TotalTanks = totalTanks;
            DamageDealt = damageDealt;
            KillCount = killCount;
        }
    }

    /// <summary>金錢發放明細，供結算畫面逐項顯示。</summary>
    public readonly struct RewardBreakdown
    {
        public readonly int VictoryBonus;
        public readonly int RankBonus;
        public readonly int DamageBonus;
        public readonly int KillBonus;
        public readonly int TotalAwarded;

        public RewardBreakdown(int victoryBonus, int rankBonus, int damageBonus, int killBonus)
        {
            VictoryBonus = victoryBonus;
            RankBonus = rankBonus;
            DamageBonus = damageBonus;
            KillBonus = killBonus;
            TotalAwarded = victoryBonus + rankBonus + damageBonus + killBonus;
        }
    }

    /// <summary>
    /// 金錢與升級服務介面（由 Agent A3 於 Phase 1 實作）。
    /// GetUpgradeCost 為純函式，方便單獨測試升級曲線，不依賴服務內部狀態。
    /// </summary>
    public interface IEconomyService
    {
        int CurrentMoney { get; }
        int FirepowerLevel { get; }
        int MoveSpeedLevel { get; }

        int GetUpgradeCost(UpgradeType type, int currentLevel);
        bool CanUpgrade(UpgradeType type);
        bool TryUpgrade(UpgradeType type);

        RewardBreakdown AwardMoney(BattleResult result);
    }

    /// <summary>
    /// 等級 -> 實際效果數值換算介面。Economy 只管等級/金錢，效果換算統一由此介面負責，
    /// Combat（火力倍率）、Gameplay（移動速度倍率）只消費，不重算。
    /// </summary>
    public interface IUpgradeEffectResolver
    {
        float GetFirepowerMultiplier(int firepowerLevel);
        float GetMoveSpeedMultiplier(int moveSpeedLevel);
    }
}
