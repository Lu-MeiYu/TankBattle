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

        public void EndTurn(TurnEndReason reason)
        {
            ITurnParticipant owner = CurrentTurnOwner;
            OnTurnEnded?.Invoke(owner, reason);
            _turnOrderService.Advance();
        }
    }
}
