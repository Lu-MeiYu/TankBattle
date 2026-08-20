using System;
using System.Collections.Generic;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Core.AI
{
    /// <summary>
    /// Normal 難度：優先攻擊血量最低的目標，血量相同時挑最近的（Spec §4.1）。
    /// 中等瞄準誤差，且納入風力計算但仍有一定誤差（<c>WindAccuracy</c> &lt; 1）。
    /// </summary>
    public sealed class NormalAIStrategy : AIStrategyBase
    {
        private readonly AIDifficultySettings _settings;

        public NormalAIStrategy(AIDifficultySettings settings, IBallisticsSimulator simulator,
            IBallisticsEstimator estimator, float muzzleSpeedAtFullPower,
            float maxFlightTimeSeconds = 10f, float simulationStepSeconds = 0.02f)
            : base(simulator, estimator, muzzleSpeedAtFullPower, maxFlightTimeSeconds, simulationStepSeconds)
        {
            _settings = settings;
        }

        public override AIDifficulty Difficulty => AIDifficulty.Normal;

        protected override AIDifficultySettings Settings => _settings;

        public override ITankState SelectTarget(ITankState self, IReadOnlyList<ITankState> candidates,
            IRandomSource random)
        {
            IReadOnlyList<ITankState> aliveOthers = AITargetUtility.FilterAliveOthers(self, candidates);
            if (aliveOthers.Count == 0)
            {
                throw new InvalidOperationException("沒有可選擇的存活目標。");
            }

            ITankState best = aliveOthers[0];
            float bestDistance = Vector2.Distance(self.Position, best.Position);

            for (int i = 1; i < aliveOthers.Count; i++)
            {
                ITankState candidate = aliveOthers[i];
                float distance = Vector2.Distance(self.Position, candidate.Position);

                if (candidate.CurrentHp < best.CurrentHp ||
                    (candidate.CurrentHp == best.CurrentHp && distance < bestDistance))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
