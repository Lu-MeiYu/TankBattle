using System;
using System.Collections.Generic;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// <see cref="IMatchOutcomeEvaluator"/> 的預設實作。
    /// 玩家陣營一旦全滅即可提前判定 <see cref="MatchOutcome.PlayerDefeat"/>，
    /// 不必等到只剩最後一個 AI（與 Combat 的淘汰事件時機一致）。
    /// 每次呼叫 <see cref="Evaluate"/> 時，會將本次新偵測到的淘汰者依傳入清單的順序
    /// 附加到內部的淘汰名次紀錄中；因此呼叫端應在每次有坦克被淘汰後即時呼叫本方法，
    /// 才能取得符合實際淘汰順序的名次。
    /// </summary>
    public sealed class MatchOutcomeEvaluator : IMatchOutcomeEvaluator
    {
        private readonly List<int> _eliminationRanking = new List<int>();
        private readonly HashSet<int> _recordedTankIds = new HashSet<int>();

        public MatchOutcome Evaluate(IReadOnlyList<ITurnParticipant> allParticipants, int playerTankId)
        {
            if (allParticipants == null)
            {
                throw new ArgumentNullException(nameof(allParticipants));
            }

            bool playerAlive = false;
            int aliveCount = 0;

            foreach (ITurnParticipant participant in allParticipants)
            {
                if (participant.State.IsAlive)
                {
                    aliveCount++;
                    if (participant.State.TankId == playerTankId)
                    {
                        playerAlive = true;
                    }
                }
                else if (_recordedTankIds.Add(participant.State.TankId))
                {
                    _eliminationRanking.Add(participant.State.TankId);
                }
            }

            if (!playerAlive)
            {
                return MatchOutcome.PlayerDefeat;
            }

            if (aliveCount <= 1)
            {
                return MatchOutcome.PlayerVictory;
            }

            return MatchOutcome.Ongoing;
        }

        public IReadOnlyList<int> GetEliminationRanking() => _eliminationRanking.AsReadOnly();
    }
}
