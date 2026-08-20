using System;
using System.Collections.Generic;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Data;

namespace TankBattle.Core.AI
{
    /// <summary>
    /// Easy 難度：隨機挑選存活目標（Spec §4.1），瞄準誤差範圍最大，且僅套用少量風力修正。
    /// </summary>
    public sealed class EasyAIStrategy : AIStrategyBase
    {
        private readonly AIDifficultySettings _settings;

        public EasyAIStrategy(AIDifficultySettings settings, IBallisticsSimulator simulator,
            IBallisticsEstimator estimator, float muzzleSpeedAtFullPower,
            float maxFlightTimeSeconds = 10f, float simulationStepSeconds = 0.02f)
            : base(simulator, estimator, muzzleSpeedAtFullPower, maxFlightTimeSeconds, simulationStepSeconds)
        {
            _settings = settings;
        }

        public override AIDifficulty Difficulty => AIDifficulty.Easy;

        protected override AIDifficultySettings Settings => _settings;

        public override ITankState SelectTarget(ITankState self, IReadOnlyList<ITankState> candidates,
            IRandomSource random)
        {
            IReadOnlyList<ITankState> aliveOthers = AITargetUtility.FilterAliveOthers(self, candidates);
            if (aliveOthers.Count == 0)
            {
                throw new InvalidOperationException("沒有可選擇的存活目標。");
            }

            int index = random.NextInt(0, aliveOthers.Count);
            return aliveOthers[index];
        }
    }
}
