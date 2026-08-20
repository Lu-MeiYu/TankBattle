using System;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.Ballistics
{
    /// <summary>
    /// <see cref="IBallisticsSimulator"/> 的正式實作。純邏輯、無狀態（除建構時注入的重力常數外），
    /// 不依賴 MonoBehaviour，可完全由 NUnit 覆蓋（對應 Spec §8「Ballistics（彈道/風力）」）。
    ///
    /// 物理模型：
    /// - 垂直方向：等加速度運動，加速度為 <c>-Gravity</c>（世界座標 Y 軸向上為正）。
    /// - 水平方向：<see cref="WindData.SignedValue"/> 視為持續施加的水平加速度（Spec 3.3）。
    /// - 積分方式採半隱式歐拉法（先更新速度、再以「舊速度+新加速度貢獻的位移平均」更新位置），
    ///   在小步長下與解析解（拋物線）誤差極小，且保證「風力=0」時退化為標準拋物線（US-05）。
    /// </summary>
    public sealed class BallisticsSimulator : IBallisticsSimulator
    {
        private readonly float _gravity;

        /// <param name="gravity">重力加速度大小（正值，例如 9.81）。內部套用時會轉為向下（-Y）分量。</param>
        public BallisticsSimulator(float gravity)
        {
            if (gravity < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(gravity), "重力必須為正值（表示向下加速度大小）");
            }

            _gravity = gravity;
        }

        public TrajectoryState CreateInitialState(LaunchParameters launch, WindData wind)
        {
            float clampedPower = Mathf.Clamp(launch.PowerPercent,
                LaunchParameters.MinPowerPercent, LaunchParameters.MaxPowerPercent);
            float clampedAngle = Mathf.Clamp(launch.AngleDegrees,
                LaunchParameters.MinAngleDegrees, LaunchParameters.MaxAngleDegrees);

            float speed = launch.MuzzleSpeedAtFullPower * (clampedPower / 100f);
            float angleRad = clampedAngle * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(Mathf.Cos(angleRad) * speed, Mathf.Sin(angleRad) * speed);

            return new TrajectoryState(launch.Origin, velocity, 0f, false);
        }

        public TrajectoryState Advance(TrajectoryState state, WindData wind, float deltaTime, ITerrainQuery terrain)
        {
            if (state.HasEnded || deltaTime <= 0f)
            {
                return state;
            }

            Vector2 acceleration = new Vector2(wind.SignedValue, -_gravity);
            Vector2 fromPosition = state.Position;
            Vector2 newVelocity = state.Velocity + acceleration * deltaTime;
            Vector2 newPosition = fromPosition + state.Velocity * deltaTime
                + 0.5f * acceleration * deltaTime * deltaTime;
            float newElapsedTime = state.ElapsedTime + deltaTime;

            if (terrain != null)
            {
                if (terrain.TryGetCollision(fromPosition, newPosition, out Vector2 hitPoint))
                {
                    return new TrajectoryState(hitPoint, newVelocity, newElapsedTime, true);
                }

                if (!terrain.GetWorldBounds().Contains(newPosition))
                {
                    return new TrajectoryState(newPosition, newVelocity, newElapsedTime, true);
                }
            }

            return new TrajectoryState(newPosition, newVelocity, newElapsedTime, false);
        }

        public ImpactInfo SimulateToImpact(LaunchParameters launch, WindData wind, ITerrainQuery terrain,
            float maxFlightTime, float simulationStep)
        {
            if (simulationStep <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(simulationStep), "模擬步長必須為正值");
            }

            if (maxFlightTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFlightTime), "最大飛行時間必須為正值");
            }

            TrajectoryState state = CreateInitialState(launch, wind);

            while (!state.HasEnded && state.ElapsedTime < maxFlightTime)
            {
                float remaining = maxFlightTime - state.ElapsedTime;
                float step = Mathf.Min(simulationStep, remaining);
                state = Advance(state, wind, step, terrain);
            }

            if (!state.HasEnded)
            {
                // 已達最大模擬時間仍未命中地形/出界，視為出界（避免無限模擬，對應介面註解）。
                return new ImpactInfo(ImpactType.OutOfBounds, state.Position, state.ElapsedTime);
            }

            bool endedWithinBounds = terrain == null || terrain.GetWorldBounds().Contains(state.Position);
            ImpactType type = endedWithinBounds ? ImpactType.Terrain : ImpactType.OutOfBounds;
            return new ImpactInfo(type, state.Position, state.ElapsedTime);
        }
    }
}
