using UnityEngine;

namespace TankBattle.Data
{
    /// <summary>
    /// Economy 模組獨立設定檔（金錢獎勵公式 + 升級花費曲線 + 升級效果數值），
    /// 對應 Spec §3.7 商店與升級系統、§7 US-02/US-03/US-10。
    /// Core 邏輯（EconomyService / UpgradeEffectResolver）以建構子注入純數值，
    /// 不直接引用本 ScriptableObject，以利 NUnit 測試 mock。
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "TankBattle/Data/EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Upgrade Levels")]
        [Min(1)]
        public int maxFirepowerLevel = 5;
        [Min(1)]
        public int maxMoveSpeedLevel = 5;

        [Header("Upgrade Cost Curve (cost = baseCost * growth^currentLevel)")]
        [Min(0)]
        public int firepowerBaseCost = 100;
        [Min(1f)]
        public float firepowerCostGrowth = 1.5f;
        [Min(0)]
        public int moveSpeedBaseCost = 80;
        [Min(1f)]
        public float moveSpeedCostGrowth = 1.4f;

        [Header("Upgrade Effect (multiplier = base + level * perLevel)")]
        public float baseFirepowerMultiplier = 1f;
        public float firepowerMultiplierPerLevel = 0.2f;
        public float baseMoveSpeedMultiplier = 1f;
        public float moveSpeedMultiplierPerLevel = 0.15f;

        [Header("Battle Reward Formula")]
        [Min(0)]
        public int victoryBonus = 200;
        [Min(0)]
        public int rankBonusPerPlace = 20;
        [Min(0f)]
        public float damagePerMoneyRatio = 0.5f;
        [Min(0)]
        public int moneyPerKill = 30;
    }
}
