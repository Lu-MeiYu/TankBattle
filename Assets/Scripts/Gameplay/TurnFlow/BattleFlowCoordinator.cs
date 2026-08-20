using System;
using System.Collections.Generic;
using System.Linq;
using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Gameplay.TurnFlow
{
    /// <summary>
    /// 一場戰鬥的回合流程協調器（Agent A4，Phase 2）。對應 Docs/SharedContracts.md §1 所述
    /// 「對戰協調層…最終會在 Gameplay 層的 BattleCoordinator」。組合 Phase 1 的
    /// <see cref="ITurnOrderService"/>／<see cref="ITurnFlowController"/>／
    /// <see cref="IMatchOutcomeEvaluator"/>，並訂閱每位參與者的 <see cref="ITankHealth.OnEliminated"/>，
    /// 讓淘汰立即從行動順序移除、立即檢查勝負（US-06、US-09、US-10）。
    /// 本類別不依賴 UnityEngine.MonoBehaviour，因此可在 EditMode 以假物件完整測試；
    /// Unity 場景生命週期（Update）由 <see cref="TurnManager"/> 負責銜接。
    /// </summary>
    public sealed class BattleFlowCoordinator
    {
        private readonly ITurnOrderService _turnOrderService;
        private readonly ITurnFlowController _turnFlowController;
        private readonly IMatchOutcomeEvaluator _matchOutcomeEvaluator;
        private readonly int _playerTankId;

        private IReadOnlyList<BattleParticipant> _participants = Array.Empty<BattleParticipant>();
        private bool _isMatchOver;
        private bool _isStarted;

        public event Action<ITurnParticipant> OnTurnStarted;
        public event Action<ITurnParticipant, TurnEndReason> OnTurnEnded;
        public event Action<MatchOutcome> OnMatchEnded;

        public ITurnParticipant CurrentTurnOwner => _turnFlowController.CurrentTurnOwner;
        public ITurnTimer Timer => _turnFlowController.Timer;
        public bool IsMatchOver => _isMatchOver;

        public BattleFlowCoordinator(ITurnOrderService turnOrderService,
            ITurnFlowController turnFlowController, IMatchOutcomeEvaluator matchOutcomeEvaluator,
            int playerTankId)
        {
            _turnOrderService = turnOrderService ?? throw new ArgumentNullException(nameof(turnOrderService));
            _turnFlowController = turnFlowController ?? throw new ArgumentNullException(nameof(turnFlowController));
            _matchOutcomeEvaluator = matchOutcomeEvaluator ?? throw new ArgumentNullException(nameof(matchOutcomeEvaluator));
            _playerTankId = playerTankId;

            _turnFlowController.OnTurnStarted += HandleTurnStarted;
            _turnFlowController.OnTurnEnded += HandleTurnEnded;
        }

        /// <summary>
        /// 開始一場戰鬥：以隨機亂數建立行動順序、訂閱所有參與者的淘汰事件，並開始第一回合。
        /// 若一開局即已判定非 Ongoing（例如只有 1 名參與者），則直接結束戰鬥、不開始任何回合。
        /// </summary>
        public void StartBattle(IReadOnlyList<BattleParticipant> participants, IRandomSource random)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (participants.Count == 0)
            {
                throw new ArgumentException("participants 不可為空", nameof(participants));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (_isStarted)
            {
                throw new InvalidOperationException("BattleFlowCoordinator 只能 StartBattle 一次");
            }

            _isStarted = true;
            _participants = participants;

            foreach (BattleParticipant participant in _participants)
            {
                participant.Health.OnEliminated += HandleTankEliminated;
            }

            _turnOrderService.Initialize(_participants.Cast<ITurnParticipant>().ToList(), random);

            if (TryEvaluateAndEndIfMatchOver())
            {
                return;
            }

            _turnFlowController.BeginTurn();
        }

        /// <summary>由外層（TurnManager）每幀呼叫，推進回合限時計時器；逾時自動結束回合。</summary>
        public void Tick(float deltaTime)
        {
            if (_isMatchOver || !_isStarted)
            {
                return;
            }

            _turnFlowController.Timer.Tick(deltaTime);

            if (_turnFlowController.Timer.HasExpired)
            {
                _turnFlowController.EndTurn(TurnEndReason.TimedOut);
            }
        }

        /// <summary>由玩家/AI 控制器在完成發射後呼叫，正常結束目前回合。</summary>
        public void NotifyCurrentTurnFired()
        {
            if (_isMatchOver || !_isStarted)
            {
                return;
            }

            _turnFlowController.EndTurn(TurnEndReason.Fired);
        }

        private void HandleTurnStarted(ITurnParticipant participant)
        {
            OnTurnStarted?.Invoke(participant);
        }

        private void HandleTurnEnded(ITurnParticipant participant, TurnEndReason reason)
        {
            OnTurnEnded?.Invoke(participant, reason);

            if (TryEvaluateAndEndIfMatchOver())
            {
                return;
            }

            _turnFlowController.BeginTurn();
        }

        private void HandleTankEliminated(ITankHealth health)
        {
            BattleParticipant participant = _participants.FirstOrDefault(p => p.Health == health);
            if (participant == null)
            {
                return;
            }

            _turnOrderService.RemoveParticipant(participant.State.TankId);
            TryEvaluateAndEndIfMatchOver();
        }

        /// <summary>評估目前勝負；若已非 Ongoing 則結束戰鬥並回傳 true。</summary>
        private bool TryEvaluateAndEndIfMatchOver()
        {
            if (_isMatchOver)
            {
                return true;
            }

            MatchOutcome outcome = _matchOutcomeEvaluator.Evaluate(
                _participants.Cast<ITurnParticipant>().ToList(), _playerTankId);

            if (outcome == MatchOutcome.Ongoing)
            {
                return false;
            }

            EndMatch(outcome);
            return true;
        }

        private void EndMatch(MatchOutcome outcome)
        {
            _isMatchOver = true;

            foreach (BattleParticipant participant in _participants)
            {
                participant.Health.OnEliminated -= HandleTankEliminated;
            }

            OnMatchEnded?.Invoke(outcome);
        }
    }
}
