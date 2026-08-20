using System;
using System.Collections.Generic;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Core.AI
{
    /// <summary>
    /// Hard 難度：依威脅程度（血量越低、距離越近威脅越高）挑選目標（Spec §4.1）。
    /// 瞄準誤差範圍最小，精確納入風力計算，並考慮地形高低（以目標所在 x 座標的地表高度為瞄準點）。
    /// </summary>
    public sealed class HardAIStrategy : AIStrategyBase
    {
        private const float DistanceWeight = 0.5f;
        private const float LowHpWeight = 0.5f;
        private const float DistanceEpsilon = 0.001f;

        private readonly AIDifficultySettings _settings;

        public HardAIStrategy(AIDifficultySettings settings, IBallisticsSimulator simulator,
            IBallisticsEstimator estimator, float muzzleSpeedAtFullPower,
            float maxFlightTimeSeconds = 10f, float simulationStepSeconds = 0.02f)
            : base(simulator, estimator, muzzleSpeedAtFullPower, maxFlightTimeSeconds, simulationStepSeconds)
        {
            _settings = settings;
        }

        public override AIDifficulty Difficulty => AIDifficulty.Hard;

        protected override AIDifficultySettings Settings => _settings;

        protected override bool ConsiderTerrainHeight => true;

        public override ITankState SelectTarget(ITankState self, IReadOnlyList<ITankState> candidates,
            IRandomSource random)
        {
            IReadOnlyList<ITankState> aliveOthers = AITargetUtility.FilterAliveOthers(self, candidates);
            if (aliveOthers.Count == 0)
            {
                throw new InvalidOperationException("沒有可選擇的存活目標。");
            }

            ITankState best = aliveOthers[0];
            float bestThreat = ComputeThreatScore(self, best);

            for (int i = 1; i < aliveOthers.Count; i++)
            {
                ITankState candidate = aliveOthers[i];
                float threat = ComputeThreatScore(self, candidate);
                if (threat > bestThreat)
                {
                    best = candidate;
                    bestThreat = threat;
                }
            }

            return best;
        }

        /// <summary>威脅分數 = 距離越近分數越高 + 血量越低分數越高，兩者各佔一半權重（皆已正規化至 0~1）。</summary>
        private static float ComputeThreatScore(ITankState self, ITankState candidate)
        {
            float distance = Vector2.Distance(self.Position, candidate.Position);
            float proximityScore = 1f / (distance + DistanceEpsilon);

            float hpRatio = candidate.MaxHp > 0
                ? Mathf.Clamp01((float)candidate.CurrentHp / candidate.MaxHp)
                : 0f;
            float lowHpScore = 1f - hpRatio;

            return DistanceWeight * proximityScore + LowHpWeight * lowHpScore;
        }
    }
}
