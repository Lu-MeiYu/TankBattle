using UnityEngine;

namespace TankBattle.Data
{
    /// <summary>
    /// Tank 模組（A2，Phase 2）獨立設定，命名慣例 `TankConfig`（見 Docs/SharedContracts.md §4）。
    /// 只放「單一標準砲彈」情境下坦克的基礎數值：
    /// - <see cref="muzzleSpeedAtFullPower"/> 是 Tank 實際開火時使用的滿威力初速，
    ///   AI（A3 的 AIController/AIStrategyFactory）反推瞄準角度/威力時必須讀取「同一份」數值，
    ///   否則 AI 算出的角度/威力會系統性偏移。
    /// - <see cref="baseFirepowerDamage"/>／<see cref="baseMoveSpeed"/> 皆為「基準值」，
    ///   實際生效數值由呼叫端乘上 Economy 的 <c>IUpgradeEffectResolver</c> 對應倍率
    ///   （GetFirepowerMultiplier / GetMoveSpeedMultiplier），本檔案不重複定義倍率
    ///   （見 SharedContracts §2.3：Economy 只管等級換算倍率，Combat/Gameplay 只消費、不重算）。
    /// Core 邏輯類別不得直接引用本 ScriptableObject；一律由 Gameplay 層（Tank.cs）讀取後，
    /// 以純數值（例如 <see cref="TankBattle.Core.Shared.LaunchParameters"/>）注入 Core。
    /// </summary>
    [CreateAssetMenu(fileName = "TankConfig", menuName = "TankBattle/Data/TankConfig")]
    public class TankConfig : ScriptableObject
    {
        [Header("Health")]
        [Min(1)]
        public int baseMaxHp = 100;

        [Header("Ballistics (single source of truth for Tank + AI aiming)")]
        [Min(0f)]
        public float muzzleSpeedAtFullPower = 40f;
        [Min(0f)]
        public float barrelLength = 1f;

        [Header("Combat (base values only, multiplier comes from EconomyConfig)")]
        [Min(0f)]
        public float baseFirepowerDamage = 25f;
        [Min(0f)]
        public float explosionRadius = 2f;

        [Header("Movement (base value only, multiplier comes from EconomyConfig)")]
        [Min(0f)]
        public float baseMoveSpeed = 3f;

        [Header("Ground Follow")]
        [Min(0f)]
        public float fallSpeed = 10f;
    }
}
