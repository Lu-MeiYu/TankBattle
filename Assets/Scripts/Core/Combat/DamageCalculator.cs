using UnityEngine;

namespace TankBattle.Core.Combat
{
    /// <summary>
    /// <see cref="IDamageCalculator"/> 的預設實作（Agent A2，Phase 1）。
    /// 傷害公式：基礎傷害 × 火力等級加成 × (1 - 距離比例)^衰減指數，超出爆炸半徑則傷害為 0。
    /// 純函式、無亂數依賴，衰減指數由建構子注入以利平衡調整與測試（預設 1 = 線性衰減）。
    /// </summary>
    public sealed class DamageCalculator : IDamageCalculator
    {
        private readonly float _falloffExponent;

        public DamageCalculator(float falloffExponent = 1f)
        {
            _falloffExponent = falloffExponent <= 0f ? 1f : falloffExponent;
        }

        public float CalculateDamage(DamageContext context)
        {
            if (context.ExplosionRadius <= 0f)
            {
                return 0f;
            }

            if (context.DistanceFromCenter >= context.ExplosionRadius)
            {
                return 0f;
            }

            if (context.DistanceFromCenter <= 0f)
            {
                return Mathf.Max(0f, context.BaseDamage * context.FirepowerMultiplier);
            }

            float distanceRatio = context.DistanceFromCenter / context.ExplosionRadius;
            float falloff = Mathf.Pow(1f - distanceRatio, _falloffExponent);
            float damage = context.BaseDamage * context.FirepowerMultiplier * falloff;
            return Mathf.Max(0f, damage);
        }
    }
}
