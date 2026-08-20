using System.Collections.Generic;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.Combat
{
    /// <summary>單一坦克的傷害結算結果。</summary>
    public readonly struct TankDamageResult
    {
        public readonly ITankState Tank;
        public readonly float DamageApplied;
        public readonly bool WasEliminated;

        public TankDamageResult(ITankState tank, float damageApplied, bool wasEliminated)
        {
            Tank = tank;
            DamageApplied = damageApplied;
            WasEliminated = wasEliminated;
        }
    }

    /// <summary>爆炸結算請求。</summary>
    public readonly struct ExplosionRequest
    {
        public readonly Vector2 Center;
        public readonly float Radius;
        public readonly float BaseDamage;

        /// <summary>候選坦克清單（例如場上所有存活坦克），實際是否受影響由 Resolver 依距離篩選。</summary>
        public readonly IReadOnlyList<ITankState> TanksInRange;

        /// <summary>
        /// 發射方當前的火力等級，供 Resolver 透過 <see cref="Economy.IUpgradeEffectResolver"/>
        /// 查出火力倍率後寫入 <see cref="DamageContext.FirepowerMultiplier"/>。
        /// </summary>
        public readonly int ShooterFirepowerLevel;

        public ExplosionRequest(Vector2 center, float radius, float baseDamage,
            IReadOnlyList<ITankState> tanksInRange, int shooterFirepowerLevel = 0)
        {
            Center = center;
            Radius = radius;
            BaseDamage = baseDamage;
            TanksInRange = tanksInRange;
            ShooterFirepowerLevel = shooterFirepowerLevel;
        }
    }

    /// <summary>爆炸結算結果。</summary>
    public readonly struct ExplosionResult
    {
        public readonly TerrainModificationResult TerrainChange;
        public readonly IReadOnlyList<TankDamageResult> Damages;

        public ExplosionResult(TerrainModificationResult terrainChange,
            IReadOnlyList<TankDamageResult> damages)
        {
            TerrainChange = terrainChange;
            Damages = damages;
        }
    }

    /// <summary>
    /// 爆炸結算的單一入口（由 Agent A2 於 Phase 1 實作）。
    /// 內部依序執行：炸地形（透過 ITerrainCarver）-> 找出範圍內坦克 -> 算傷害（IDamageCalculator）
    /// -> 套用 TakeDamage。Gameplay 層偵測到 Ballistics 回報命中後，只需呼叫這一個方法，
    /// 不必自行協調「先炸地形還是先算傷害」的呼叫順序。
    /// </summary>
    public interface IExplosionResolver
    {
        ExplosionResult Resolve(ExplosionRequest request, ITerrainCarver terrain);
    }
}
