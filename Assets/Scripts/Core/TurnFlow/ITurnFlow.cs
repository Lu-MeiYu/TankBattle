using System;
using System.Collections.Generic;
using TankBattle.Core.Shared;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// 行動順序中的參與者，包裹 <see cref="ITankState"/>，不重新定義「存活」語意，
    /// 避免與 Combat 的淘汰判定不一致。
    /// </summary>
    public interface ITurnParticipant
    {
        ITankState State { get; }
    }

    /// <summary>
    /// 行動順序管理（由 Agent A4 於 Phase 1 實作）。
    /// Initialize 時以注入的 <see cref="IRandomSource"/> 將存活參與者隨機排序，
    /// 形成本場戰鬥固定循環的行動順序（US-06）。
    /// </summary>
    public interface ITurnOrderService
    {
        void Initialize(IReadOnlyList<ITurnParticipant> participants, IRandomSource random);

        ITurnParticipant Current { get; }

        /// <summary>前進到下一位存活參與者；已淘汰者自動跳過。</summary>
        ITurnParticipant Advance();

        /// <summary>淘汰後從順序中移除，不打亂剩餘順序的相對位置。</summary>
        void RemoveParticipant(int tankId);

        IReadOnlyList<ITurnParticipant> CurrentOrderSnapshot { get; }
    }

    /// <summary>回合結束原因。</summary>
    public enum TurnEndReason
    {
        Fired,
        TimedOut,
        Eliminated
    }

    /// <summary>
    /// 回合限時計時器（由 Agent A4 於 Phase 1 實作）。純邏輯運算，Tick 由外層 MonoBehaviour
    /// 每幀傳入 deltaTime 呼叫，本身不依賴 UnityEngine.Time。
    /// </summary>
    public interface ITurnTimer
    {
        float DurationSeconds { get; }
        float RemainingSeconds { get; }
        bool HasExpired { get; }

        void StartTurn();
        void Tick(float deltaTime);
    }

    /// <summary>回合流程狀態機的最小抽象，供 Gameplay 層的 MonoBehaviour 包裹呼叫。</summary>
    public interface ITurnFlowController
    {
        ITurnParticipant CurrentTurnOwner { get; }
        ITurnTimer Timer { get; }

        void BeginTurn();
        void EndTurn(TurnEndReason reason);

        event Action<ITurnParticipant> OnTurnStarted;
        event Action<ITurnParticipant, TurnEndReason> OnTurnEnded;
    }

    /// <summary>勝負判定結果。玩家陣營一旦全滅即可提前判定 PlayerDefeat，不必等到只剩最後一個 AI。</summary>
    public enum MatchOutcome
    {
        Ongoing,
        PlayerVictory,
        PlayerDefeat
    }

    /// <summary>勝負判定介面（由 Agent A4 於 Phase 1 實作）。</summary>
    public interface IMatchOutcomeEvaluator
    {
        MatchOutcome Evaluate(IReadOnlyList<ITurnParticipant> allParticipants, int playerTankId);

        IReadOnlyList<int> GetEliminationRanking();
    }
}
