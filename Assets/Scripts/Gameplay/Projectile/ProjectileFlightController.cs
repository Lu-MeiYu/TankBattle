using System;
using System.Collections.Generic;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Gameplay.Projectile
{
    /// <summary>
    /// 砲彈飛行的純邏輯狀態機（Phase 2，Agent A1 - Gameplay/Projectile）。
    /// 包裹 <see cref="IBallisticsSimulator"/> 逐步推進彈道，並依 SharedContracts §2.1 的約定，
    /// 由本類別（Gameplay 層）每幀用候選坦克的 <see cref="ITankState.Position"/> 做簡單距離/半徑
    /// 檢查來判斷坦克命中；地形碰撞與出界仍完全交由 Ballistics 的 <see cref="ITerrainQuery"/> 判斷。
    /// 不依賴 MonoBehaviour，可在 EditMode 以 NUnit 完整覆蓋；<see cref="Projectile"/> 只是包裹本類別
    /// 的薄 MonoBehaviour（每幀呼叫 <see cref="Step"/> 並同步 transform）。
    /// </summary>
    public sealed class ProjectileFlightController
    {
        private readonly IBallisticsSimulator _simulator;
        private readonly ITerrainQuery _terrain;
        private readonly WindData _wind;
        private readonly float _tankHitRadius;
        private readonly bool _hasExcludedTankId;
        private readonly int _excludedTankId;

        /// <summary>目前彈道狀態（位置/速度/經過時間）。</summary>
        public TrajectoryState CurrentState { get; private set; }

        /// <summary>飛行是否已結束（命中地形/坦克或出界）。</summary>
        public bool HasEnded { get; private set; }

        /// <summary>結束原因與命中資訊；<see cref="HasEnded"/> 為 false 時內容無意義。</summary>
        public ImpactInfo Impact { get; private set; }

        /// <param name="simulator">正向彈道模擬器（一般為 <see cref="BallisticsSimulator"/>）。</param>
        /// <param name="launch">發射參數（角度/威力/砲口位置/滿威力初速）。</param>
        /// <param name="wind">本次發射固定不變的風力（US-05：同一發射過程中風力保持不變）。</param>
        /// <param name="terrain">地形查詢，供逐步碰撞判定使用；可為 null（測試無地形情境）。</param>
        /// <param name="tankHitRadius">坦克命中判定半徑，須為非負值。</param>
        /// <param name="excludedTankId">
        /// 排除檢查的坦克 Id（通常是發射者自身，避免砲彈剛從砲口射出就命中自己）。
        /// </param>
        public ProjectileFlightController(IBallisticsSimulator simulator, LaunchParameters launch,
            WindData wind, ITerrainQuery terrain, float tankHitRadius, int? excludedTankId = null)
        {
            if (simulator == null)
            {
                throw new ArgumentNullException(nameof(simulator));
            }

            if (tankHitRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tankHitRadius), "tankHitRadius 不可為負數");
            }

            _simulator = simulator;
            _terrain = terrain;
            _wind = wind;
            _tankHitRadius = tankHitRadius;
            _hasExcludedTankId = excludedTankId.HasValue;
            _excludedTankId = excludedTankId ?? 0;

            CurrentState = _simulator.CreateInitialState(launch, wind);
            HasEnded = false;
            Impact = default;
        }

        /// <summary>
        /// 推進一個時間步。若飛行已結束，直接回傳 true 且不再變動任何狀態（冪等）。
        /// </summary>
        /// <param name="deltaTime">本幀經過時間；非正值時視為不推進（回傳目前 HasEnded 狀態）。</param>
        /// <param name="candidateTanks">本幀用於坦克命中檢查的候選坦克清單（可為 null）。</param>
        /// <returns>飛行是否已（因本次呼叫而）結束。</returns>
        public bool Step(float deltaTime, IReadOnlyList<ITankState> candidateTanks)
        {
            if (HasEnded)
            {
                return true;
            }

            if (deltaTime <= 0f)
            {
                return false;
            }

            TrajectoryState next = _simulator.Advance(CurrentState, _wind, deltaTime, _terrain);

            ITankState hitTank = FindNearestHitTank(next.Position, candidateTanks);
            if (hitTank != null)
            {
                CurrentState = next;
                HasEnded = true;
                Impact = new ImpactInfo(ImpactType.Tank, hitTank.Position, next.ElapsedTime);
                return true;
            }

            CurrentState = next;

            if (next.HasEnded)
            {
                HasEnded = true;
                ImpactType type = ResolveTerrainOrOutOfBounds(next.Position);
                Impact = new ImpactInfo(type, next.Position, next.ElapsedTime);
                return true;
            }

            return false;
        }

        private ITankState FindNearestHitTank(Vector2 point, IReadOnlyList<ITankState> candidateTanks)
        {
            if (candidateTanks == null)
            {
                return null;
            }

            ITankState nearest = null;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < candidateTanks.Count; i++)
            {
                ITankState tank = candidateTanks[i];
                if (tank == null || !tank.IsAlive)
                {
                    continue;
                }

                if (_hasExcludedTankId && tank.TankId == _excludedTankId)
                {
                    continue;
                }

                float distance = Vector2.Distance(tank.Position, point);
                if (distance > _tankHitRadius)
                {
                    continue;
                }

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = tank;
                }
            }

            return nearest;
        }

        private ImpactType ResolveTerrainOrOutOfBounds(Vector2 position)
        {
            if (_terrain == null)
            {
                return ImpactType.OutOfBounds;
            }

            return _terrain.GetWorldBounds().Contains(position) ? ImpactType.Terrain : ImpactType.OutOfBounds;
        }
    }
}
