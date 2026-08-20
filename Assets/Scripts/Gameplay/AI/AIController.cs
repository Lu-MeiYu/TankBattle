using System;
using System.Collections.Generic;
using TankBattle.Core.AI;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Gameplay.AI
{
    /// <summary>
    /// 掛在 AI 坦克上的 Gameplay 層元件（Phase 2）。負責在輪到自己時，
    /// 用 <see cref="AIDecisionRunner"/>／<see cref="AIStrategyFactory"/> 算出目標與瞄準結果，
    /// 交由 TurnFlow/BattleCoordinator（其他 Agent 的 Gameplay 模組）驅動實際發射。
    /// 本類別刻意保持精簡（只做欄位持有與委派），重邏輯都在可被 NUnit 覆蓋的純 C# 類別中。
    /// </summary>
    public sealed class AIController : MonoBehaviour
    {
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;
        [SerializeField] private AIDifficultyConfig difficultyConfig;
        [SerializeField] private float muzzleSpeedAtFullPower = 40f;
        [SerializeField] private float decisionTimeoutSeconds = 5f;

        private IAIStrategy _strategy;
        private AIDecisionRunner _runner;

        public AIDifficulty Difficulty => difficulty;

        /// <summary>由外部（BattleCoordinator）在戰鬥開始時呼叫一次，注入本場戰鬥共用的彈道服務與亂數源。</summary>
        public void Initialize(IBallisticsSimulator simulator, IBallisticsEstimator estimator, IRandomSource random)
        {
            if (difficultyConfig == null)
            {
                throw new InvalidOperationException("AIController 需要指定 AIDifficultyConfig。");
            }

            _strategy = AIStrategyFactory.Create(difficulty, difficultyConfig, simulator, estimator,
                muzzleSpeedAtFullPower);
            _runner = new AIDecisionRunner(random);
        }

        /// <summary>輪到本坦克時由 TurnFlow 呼叫，回傳選中的目標與瞄準結果（Success = false 代表跳過本回合）。</summary>
        public AITurnResult DecideTurn(ITankState self, IReadOnlyList<ITankState> candidates, WindData wind,
            float gravity, ITerrainQuery terrain)
        {
            if (_runner == null || _strategy == null)
            {
                throw new InvalidOperationException("AIController 尚未初始化，請先呼叫 Initialize。");
            }

            return _runner.DecideTurn(_strategy, self, candidates, wind, gravity, terrain, decisionTimeoutSeconds);
        }
    }
}
