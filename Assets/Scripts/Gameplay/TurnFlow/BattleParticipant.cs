using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Gameplay.TurnFlow
{
    /// <summary>
    /// 戰鬥參與者：包裹 <see cref="ITankState"/>（唯讀狀態）與 <see cref="ITankHealth"/>
    /// （扣血/淘汰事件），供 <see cref="BattleFlowCoordinator"/> 同時取用兩者，
    /// 而不需要依賴具體的 Tank MonoBehaviour 類別（由 Agent A2 於 Phase 2 實作）。
    /// </summary>
    public sealed class BattleParticipant : ITurnParticipant
    {
        public ITankState State { get; }
        public ITankHealth Health { get; }

        public BattleParticipant(ITankState state, ITankHealth health)
        {
            State = state;
            Health = health;
        }
    }
}
