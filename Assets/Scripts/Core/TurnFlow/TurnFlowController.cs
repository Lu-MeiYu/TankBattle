using System;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// <see cref="ITurnFlowController"/> 的預設實作，組合 <see cref="ITurnOrderService"/> 與
    /// <see cref="ITurnTimer"/>。是否逾時（TimedOut）或已發射（Fired）由呼叫端（Gameplay 層）判斷後
    /// 透過 <see cref="EndTurn"/> 告知；淘汰移除由 Combat 的事件處理者呼叫
    /// <see cref="ITurnOrderService.RemoveParticipant"/> 完成，本類別不重複處理淘汰邏輯。
    /// </summary>
    public sealed class TurnFlowController : ITurnFlowController
    {
        private readonly ITurnOrderService _turnOrderService;
        private readonly ITurnTimer _timer;

        public TurnFlowController(ITurnOrderService turnOrderService, ITurnTimer timer)
        {
            _turnOrderService = turnOrderService ?? throw new ArgumentNullException(nameof(turnOrderService));
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        }

        public ITurnParticipant CurrentTurnOwner => _turnOrderService.Current;

        public ITurnTimer Timer => _timer;

        public event Action<ITurnParticipant> OnTurnStarted;
        public event Action<ITurnParticipant, TurnEndReason> OnTurnEnded;

        public void BeginTurn()
        {
            if (CurrentTurnOwner == null)
            {
                throw new InvalidOperationException("目前沒有存活的參與者可以開始回合");
            }

            _timer.StartTurn();
            OnTurnStarted?.Invoke(CurrentTurnOwner);
        }

        /// <summary>
        /// 結束目前回合：先推進行動順序到下一位存活參與者，再觸發 <see cref="OnTurnEnded"/>。
        /// 順序刻意如此（先 Advance 再觸發事件），是因為事件訂閱者（例如 Gameplay 層的
        /// BattleCoordinator）常會在收到 OnTurnEnded 時，同一個呼叫堆疊內立即呼叫
        /// <see cref="BeginTurn"/> 開始下一回合；若在 Advance 之前才觸發事件，
        /// 訂閱者於事件處理常式中讀到的 <see cref="CurrentTurnOwner"/> 會是尚未推進的舊值，
        /// 導致下一回合誤判成同一位參與者。
        /// </summary>
        public void EndTurn(TurnEndReason reason)
        {
            ITurnParticipant owner = CurrentTurnOwner;
            _turnOrderService.Advance();
            OnTurnEnded?.Invoke(owner, reason);
        }
    }
}
