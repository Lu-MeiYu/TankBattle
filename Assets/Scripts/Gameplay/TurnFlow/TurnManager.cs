using System;
using System.Collections.Generic;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Gameplay.TurnFlow
{
    /// <summary>
    /// 戰鬥回合流程的 MonoBehaviour 入口（Agent A4，Phase 2，對應 Spec 5.2 的
    /// <c>Scripts/Gameplay/TurnManager</c>）。實際規則邏輯全數委派給不依賴 Unity 的
    /// <see cref="BattleFlowCoordinator"/>，本類別只負責：
    /// (1) 銜接 Unity 生命週期（<see cref="Update"/> 呼叫 Tick 推進限時計時器），
    /// (2) 將 <see cref="BalanceConfig"/> 的回合限時秒數轉換為 Core 物件建構參數，
    /// (3) 把 Coordinator 的事件轉發給場景中的 UI/AIController/PlayerController（Phase 3）。
    /// </summary>
    public sealed class TurnManager : MonoBehaviour
    {
        [SerializeField]
        private BalanceConfig balanceConfig;

        private BattleFlowCoordinator _coordinator;

        /// <summary>
        /// 除了在 Inspector 指定之外，也允許由場景初始化流程（或測試）以程式方式注入，
        /// 方便 BattleCoordinator/PlayMode 測試在執行期動態建立設定。
        /// </summary>
        public BalanceConfig BalanceConfig
        {
            get => balanceConfig;
            set => balanceConfig = value;
        }

        public event Action<ITurnParticipant> OnTurnStarted;
        public event Action<ITurnParticipant, TurnEndReason> OnTurnEnded;
        public event Action<MatchOutcome> OnMatchEnded;

        public ITurnParticipant CurrentTurnOwner => _coordinator?.CurrentTurnOwner;

        public ITurnTimer Timer => _coordinator?.Timer;

        public bool IsMatchOver => _coordinator == null || _coordinator.IsMatchOver;

        /// <summary>
        /// 開始一場戰鬥。由 BattleCoordinator/Scene 初始化流程呼叫一次，
        /// 傳入場上所有坦克參與者、玩家坦克 Id，以及本場戰鬥共用的種子亂數源。
        /// </summary>
        public void StartBattle(IReadOnlyList<BattleParticipant> participants, int playerTankId,
            IRandomSource random)
        {
            if (balanceConfig == null)
            {
                throw new InvalidOperationException("TurnManager 尚未指定 BalanceConfig");
            }

            var turnOrderService = new TurnOrderService();
            var turnTimer = new TurnTimer(balanceConfig.turnTimeLimitSeconds);
            var turnFlowController = new TurnFlowController(turnOrderService, turnTimer);
            var matchOutcomeEvaluator = new MatchOutcomeEvaluator();

            _coordinator = new BattleFlowCoordinator(turnOrderService, turnFlowController,
                matchOutcomeEvaluator, playerTankId);
            _coordinator.OnTurnStarted += HandleTurnStarted;
            _coordinator.OnTurnEnded += HandleTurnEnded;
            _coordinator.OnMatchEnded += HandleMatchEnded;

            _coordinator.StartBattle(participants, random);
        }

        /// <summary>由玩家/AI 控制器在完成發射後呼叫，結束目前回合並輪到下一位。</summary>
        public void NotifyCurrentTurnFired()
        {
            _coordinator?.NotifyCurrentTurnFired();
        }

        private void Update()
        {
            if (_coordinator == null || _coordinator.IsMatchOver)
            {
                return;
            }

            _coordinator.Tick(Time.deltaTime);
        }

        private void HandleTurnStarted(ITurnParticipant participant)
        {
            OnTurnStarted?.Invoke(participant);
        }

        private void HandleTurnEnded(ITurnParticipant participant, TurnEndReason reason)
        {
            OnTurnEnded?.Invoke(participant, reason);
        }

        private void HandleMatchEnded(MatchOutcome outcome)
        {
            OnMatchEnded?.Invoke(outcome);
        }

        private void OnDestroy()
        {
            if (_coordinator == null)
            {
                return;
            }

            _coordinator.OnTurnStarted -= HandleTurnStarted;
            _coordinator.OnTurnEnded -= HandleTurnEnded;
            _coordinator.OnMatchEnded -= HandleMatchEnded;
        }
    }
}
