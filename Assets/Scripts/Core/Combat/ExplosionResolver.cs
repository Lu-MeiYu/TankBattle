using System.Collections.Generic;
using TankBattle.Core.Economy;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.Combat
{
    /// <summary>
    /// <see cref="IExplosionResolver"/> 的預設實作（Agent A2，Phase 1）。
    /// 內部依序執行：炸地形（<see cref="ITerrainCarver"/>）-> 篩選半徑內存活坦克 ->
    /// 依 <see cref="IUpgradeEffectResolver"/> 查出火力倍率算傷害（<see cref="IDamageCalculator"/>）
    /// -> 對實作 <see cref="ITankHealth"/> 的坦克套用 TakeDamage。
    /// </summary>
    public sealed class ExplosionResolver : IExplosionResolver
    {
        private readonly IDamageCalculator _damageCalculator;
        private readonly IUpgradeEffectResolver _upgradeEffectResolver;

        public ExplosionResolver(IDamageCalculator damageCalculator,
            IUpgradeEffectResolver upgradeEffectResolver)
        {
            _damageCalculator = damageCalculator;
            _upgradeEffectResolver = upgradeEffectResolver;
        }

        public ExplosionResult Resolve(ExplosionRequest request, ITerrainCarver terrain)
        {
            TerrainModificationResult terrainChange = terrain.CarveCrater(request.Center, request.Radius);

            float firepowerMultiplier =
                _upgradeEffectResolver.GetFirepowerMultiplier(request.ShooterFirepowerLevel);

            var damages = new List<TankDamageResult>();

            if (request.TanksInRange != null)
            {
                for (int i = 0; i < request.TanksInRange.Count; i++)
                {
                    ITankState tank = request.TanksInRange[i];
                    if (tank == null || !tank.IsAlive)
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(tank.Position, request.Center);
                    if (distance > request.Radius)
                    {
                        continue;
                    }

                    var context = new DamageContext(request.BaseDamage, firepowerMultiplier,
                        request.Radius, distance);
                    float damage = _damageCalculator.CalculateDamage(context);

                    bool wasEliminated = false;
                    if (tank is ITankHealth health)
                    {
                        health.TakeDamage(damage);
                        wasEliminated = health.IsEliminated;
                    }

                    damages.Add(new TankDamageResult(tank, damage, wasEliminated));
                }
            }

            return new ExplosionResult(terrainChange, damages);
        }
    }
}
