using System;
using System.Collections.Generic;
using System.Threading;
using TankBattle.Core.AI;
using TankBattle.Core.Shared;

namespace TankBattle.Gameplay.AI
{
    /// <summary>單一次 AI 回合決策的結果：選中的目標與反推出的瞄準結果。</summary>
    public readonly struct AITurnResult
    {
        public readonly ITankState Target;
        public readonly AimResult Aim;

        public AITurnResult(ITankState target, AimResult aim)
        {
            Target = target;
            Aim = aim;
        }
    }

    /// <summary>
    /// 串接 <see cref="IAIStrategy"/> 完成單次「選目標 -&gt; 反推瞄準」的流程（Phase 2：Gameplay/AIController）。
    /// 純 C#、無 MonoBehaviour 依賴，方便 NUnit 覆蓋。風力由呼叫端（Turn/Match Flow／BattleCoordinator）
    /// 於輪到此坦克發射前產生後傳入，本類別不負責產生風力（見 Docs/SharedContracts.md §2.1）。
    /// decisionTimeoutSeconds &lt;= 0 時代表不設限時（測試/除錯用）。
    /// </summary>
    public sealed class AIDecisionRunner
    {
        private readonly IRandomSource _random;

        public AIDecisionRunner(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public AITurnResult DecideTurn(IAIStrategy strategy, ITankState self,
            IReadOnlyList<ITankState> candidates, WindData wind, float gravity, ITerrainQuery terrain,
            float decisionTimeoutSeconds)
        {
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            ITankState target = strategy.SelectTarget(self, candidates, _random);
            var context = new AimingContext(self, target, wind, gravity, terrain);

            using var cts = new CancellationTokenSource();
            if (decisionTimeoutSeconds > 0f)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(decisionTimeoutSeconds));
            }

            AimResult aim = strategy.DecideAim(context, _random, cts.Token);
            return new AITurnResult(target, aim);
        }
    }
}
