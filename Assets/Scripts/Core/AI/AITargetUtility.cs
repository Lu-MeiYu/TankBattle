using System.Collections.Generic;
using TankBattle.Core.Shared;

namespace TankBattle.Core.AI
{
    /// <summary>三個難度共用的目標篩選輔助方法，避免重複實作「排除自己/排除已淘汰」的邏輯。</summary>
    internal static class AITargetUtility
    {
        /// <summary>從候選清單中過濾出「存活且不是自己」的目標。</summary>
        public static IReadOnlyList<ITankState> FilterAliveOthers(ITankState self,
            IReadOnlyList<ITankState> candidates)
        {
            var result = new List<ITankState>();
            if (candidates == null)
            {
                return result;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                ITankState candidate = candidates[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                if (self != null && candidate.TankId == self.TankId)
                {
                    continue;
                }

                result.Add(candidate);
            }

            return result;
        }
    }
}
