namespace TankBattle.Core.Combat
{
    /// <summary>
    /// 傷害計算的輸入內容。FirepowerMultiplier 由呼叫端（ExplosionResolver）
    /// 透過 Economy 的 IUpgradeEffectResolver 查出後帶入，Combat 不反查 Economy 服務。
    /// </summary>
    public readonly struct DamageContext
    {
        public readonly float BaseDamage;
        public readonly float FirepowerMultiplier;
        public readonly float ExplosionRadius;
        public readonly float DistanceFromCenter;

        public DamageContext(float baseDamage, float firepowerMultiplier, float explosionRadius,
            float distanceFromCenter)
        {
            BaseDamage = baseDamage;
            FirepowerMultiplier = firepowerMultiplier;
            ExplosionRadius = explosionRadius;
            DistanceFromCenter = distanceFromCenter;
        }
    }

    /// <summary>
    /// 傷害計算介面（由 Agent A2 於 Phase 1 實作）。純函式，無亂數依賴。
    /// 傷害公式：基礎傷害 × 火力等級加成 − 依爆炸中心距離的傷害衰減；超出爆炸半徑則傷害為 0。
    /// </summary>
    public interface IDamageCalculator
    {
        float CalculateDamage(DamageContext context);
    }
}
