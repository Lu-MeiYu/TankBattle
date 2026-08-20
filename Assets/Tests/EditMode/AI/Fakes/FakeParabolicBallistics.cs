using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.AI.Fakes
{
    /// <summary>
    /// 測試用的簡化拋物線彈道模擬（無地形碰撞細節，只用來驗證 AI 的二分搜尋/瞄準邏輯是否收斂）。
    /// 風力視為水平加速度；重力為建構時固定值，須與測試中傳給 <c>AimingContext.Gravity</c> 的值一致，
    /// 因為真實 <see cref="IBallisticsSimulator"/> 介面本身不接受重力參數（由該模組內部持有）。
    /// </summary>
    internal sealed class FakeParabolicBallistics : IBallisticsSimulator, IBallisticsEstimator
    {
        private readonly float _gravity;

        public FakeParabolicBallistics(float gravity = 9.8f)
        {
            _gravity = gravity;
        }

        public TrajectoryState CreateInitialState(LaunchParameters launch, WindData wind)
        {
            Vector2 velocity = ComputeInitialVelocity(launch);
            return new TrajectoryState(launch.Origin, velocity, 0f, false);
        }

        public TrajectoryState Advance(TrajectoryState state, float deltaTime, ITerrainQuery terrain)
        {
            Vector2 acceleration = new Vector2(0f, -_gravity);
            Vector2 newVelocity = state.Velocity + acceleration * deltaTime;
            Vector2 newPosition = state.Position + state.Velocity * deltaTime + 0.5f * acceleration * deltaTime * deltaTime;
            float newTime = state.ElapsedTime + deltaTime;

            bool ended = terrain != null && terrain.IsSolidAt(newPosition);
            return new TrajectoryState(newPosition, newVelocity, newTime, ended);
        }

        public ImpactInfo SimulateToImpact(LaunchParameters launch, WindData wind, ITerrainQuery terrain,
            float maxFlightTime, float simulationStep)
        {
            Vector2 velocity = ComputeInitialVelocity(launch);
            Vector2 position = launch.Origin;
            float acceleratedWind = wind.SignedValue;
            float elapsed = 0f;

            Vector2 lastPosition = position;
            while (elapsed < maxFlightTime)
            {
                Vector2 acceleration = new Vector2(acceleratedWind, -_gravity);
                velocity += acceleration * simulationStep;
                position += velocity * simulationStep + 0.5f * acceleration * simulationStep * simulationStep;
                elapsed += simulationStep;

                float groundHeight = terrain != null ? terrain.GetSurfaceHeight(position.x) : 0f;
                if (position.y <= groundHeight)
                {
                    return new ImpactInfo(ImpactType.Terrain, position, elapsed);
                }

                lastPosition = position;
            }

            return new ImpactInfo(ImpactType.OutOfBounds, lastPosition, elapsed);
        }

        public (float angleDegrees, float powerPercent) EstimateNoWind(Vector2 shooterPosition,
            Vector2 targetPosition, float gravity, float muzzleSpeedAtFullPower)
        {
            float dx = targetPosition.x - shooterPosition.x;
            float angleDegrees = dx >= 0f ? 45f : 135f;

            float requiredSpeed = Mathf.Sqrt(Mathf.Abs(dx) * gravity);
            float powerPercent = muzzleSpeedAtFullPower > 0f
                ? Mathf.Clamp(requiredSpeed / muzzleSpeedAtFullPower * 100f, 0f, 100f)
                : 0f;

            return (angleDegrees, powerPercent);
        }

        private static Vector2 ComputeInitialVelocity(LaunchParameters launch)
        {
            float angleRad = launch.AngleDegrees * Mathf.Deg2Rad;
            float speed = launch.MuzzleSpeedAtFullPower * (launch.PowerPercent / 100f);
            return new Vector2(speed * Mathf.Cos(angleRad), speed * Mathf.Sin(angleRad));
        }
    }
}
