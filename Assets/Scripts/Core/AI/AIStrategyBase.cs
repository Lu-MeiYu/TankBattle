using System;
using System.Threading;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Core.AI
{
    /// <summary>
    /// 三個難度共用的瞄準演算法（見 Docs/SharedContracts.md §2.3）：
    /// 1. 用 <see cref="IBallisticsEstimator.EstimateNoWind"/> 取得無風力的初始猜測角度/威力。
    /// 2. 固定角度，對威力做二分搜尋，逼近目標的水平距離（風力依難度的 <c>WindAccuracy</c> 打折扣後套用）。
    /// 3. 依難度誤差範圍對最終角度/威力加上隨機誤差（見 Spec §4.1 難度分級表）。
    /// SelectTarget 與難度誤差設定由子類別提供，子類別不得直接建立亂數源（由呼叫端注入）。
    /// </summary>
    public abstract class AIStrategyBase : IAIStrategy
    {
        private readonly IBallisticsSimulator _simulator;
        private readonly IBallisticsEstimator _estimator;
        private readonly float _muzzleSpeedAtFullPower;
        private readonly float _maxFlightTimeSeconds;
        private readonly float _simulationStepSeconds;

        protected AIStrategyBase(IBallisticsSimulator simulator, IBallisticsEstimator estimator,
            float muzzleSpeedAtFullPower, float maxFlightTimeSeconds = 10f,
            float simulationStepSeconds = 0.02f)
        {
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
            _muzzleSpeedAtFullPower = muzzleSpeedAtFullPower;
            _maxFlightTimeSeconds = maxFlightTimeSeconds;
            _simulationStepSeconds = simulationStepSeconds;
        }

        public abstract AIDifficulty Difficulty { get; }

        protected abstract AIDifficultySettings Settings { get; }

        /// <summary>Hard 難度會考慮地形高低（Spec §4.1），瞄準目標時採用該處地表高度而非坦克中心點。</summary>
        protected virtual bool ConsiderTerrainHeight => false;

        public abstract ITankState SelectTarget(ITankState self, System.Collections.Generic.IReadOnlyList<ITankState> candidates,
            IRandomSource random);

        public AimResult DecideAim(AimingContext context, IRandomSource random, CancellationToken cancellationToken)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AimResult.Failed;
            }

            AIDifficultySettings settings = Settings;
            Vector2 origin = context.Self.Position;
            Vector2 targetPoint = ResolveTargetPoint(context);

            float effectiveWindValue = context.Wind.SignedValue * settings.windAccuracy;
            var effectiveWind = new WindData(effectiveWindValue);

            (float estimatedAngle, float estimatedPower) = _estimator.EstimateNoWind(
                origin, targetPoint, context.Gravity, _muzzleSpeedAtFullPower);
            float angle = Mathf.Clamp(estimatedAngle, LaunchParameters.MinAngleDegrees, LaunchParameters.MaxAngleDegrees);

            float bestPower = SearchPowerForDistance(context, angle, targetPoint, effectiveWind, settings,
                cancellationToken, out bool cancelled);
            if (cancelled)
            {
                return AimResult.Failed;
            }

            float finalAngle = angle + random.NextFloat(-settings.aimAngleErrorDegrees, settings.aimAngleErrorDegrees);
            float finalPower = bestPower + random.NextFloat(-settings.aimPowerErrorPercent, settings.aimPowerErrorPercent);

            finalAngle = Mathf.Clamp(finalAngle, LaunchParameters.MinAngleDegrees, LaunchParameters.MaxAngleDegrees);
            finalPower = Mathf.Clamp(finalPower, LaunchParameters.MinPowerPercent, LaunchParameters.MaxPowerPercent);

            return new AimResult(finalAngle, finalPower, true);
        }

        private Vector2 ResolveTargetPoint(in AimingContext context)
        {
            Vector2 targetPosition = context.Target.Position;
            if (!ConsiderTerrainHeight || context.Terrain == null)
            {
                return targetPosition;
            }

            float surfaceHeight = context.Terrain.GetSurfaceHeight(targetPosition.x);
            return new Vector2(targetPosition.x, surfaceHeight);
        }

        private float SearchPowerForDistance(in AimingContext context, float angle, Vector2 targetPoint,
            WindData effectiveWind, AIDifficultySettings settings, CancellationToken cancellationToken,
            out bool cancelled)
        {
            cancelled = false;
            Vector2 origin = context.Self.Position;
            float targetDistance = Mathf.Abs(targetPoint.x - origin.x);

            float lo = LaunchParameters.MinPowerPercent;
            float hi = LaunchParameters.MaxPowerPercent;
            float mid = hi;

            for (int i = 0; i < settings.maxSearchIterations; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    return 0f;
                }

                mid = (lo + hi) * 0.5f;
                var launch = LaunchParameters.Clamp(angle, mid, origin, _muzzleSpeedAtFullPower);
                ImpactInfo impact = _simulator.SimulateToImpact(launch, effectiveWind, context.Terrain,
                    _maxFlightTimeSeconds, _simulationStepSeconds);

                float landingDistance = Mathf.Abs(impact.Point.x - origin.x);
                if (landingDistance < targetDistance)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return mid;
        }
    }
}
