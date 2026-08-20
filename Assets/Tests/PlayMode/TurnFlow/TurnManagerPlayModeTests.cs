using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;
using TankBattle.Data;
using TankBattle.Gameplay.TurnFlow;
using UnityEngine;
using UnityEngine.TestTools;

namespace TankBattle.Tests.PlayMode.TurnFlow
{
    /// <summary>
    /// 驗證 <see cref="TurnManager"/> 正確銜接 Unity 生命週期（Update 推進計時器）
    /// 並轉發 <see cref="BattleFlowCoordinator"/> 的事件。詳細規則分支已在
    /// EditMode 的 BattleFlowCoordinatorTests 涵蓋，這裡只驗證 MonoBehaviour 整合層本身。
    /// </summary>
    public class TurnManagerPlayModeTests
    {
        private GameObject _gameObject;
        private TurnManager _turnManager;
        private BalanceConfig _balanceConfig;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TurnManagerUnderTest");
            _turnManager = _gameObject.AddComponent<TurnManager>();

            _balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
            _balanceConfig.turnTimeLimitSeconds = 0.05f;
            _turnManager.BalanceConfig = _balanceConfig;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_gameObject);
            Object.Destroy(_balanceConfig);
        }

        private static List<BattleParticipant> CreateParticipants(params FakeBattleTank[] tanks)
        {
            var list = new List<BattleParticipant>();
            foreach (var tank in tanks)
            {
                list.Add(new BattleParticipant(tank, tank));
            }

            return list;
        }

        [Test]
        public void StartBattle_WithoutBalanceConfig_Throws()
        {
            var gameObjectWithoutConfig = new GameObject("NoConfig");
            var turnManager = gameObjectWithoutConfig.AddComponent<TurnManager>();
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));

            Assert.Throws<System.InvalidOperationException>(() =>
                turnManager.StartBattle(participants, 0, new SeededRandomSource(1)));

            Object.Destroy(gameObjectWithoutConfig);
        }

        [Test]
        public void StartBattle_RaisesOnTurnStarted()
        {
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));

            ITurnParticipant started = null;
            _turnManager.OnTurnStarted += p => started = p;

            _turnManager.StartBattle(participants, 0, new SeededRandomSource(1));

            Assert.IsNotNull(started);
            Assert.AreEqual(_turnManager.CurrentTurnOwner, started);
        }

        [UnityTest]
        public IEnumerator Update_TicksTimerAndAutoEndsExpiredTurn()
        {
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));

            TurnEndReason? endedReason = null;
            _turnManager.OnTurnEnded += (p, r) => endedReason = r;

            _turnManager.StartBattle(participants, 0, new SeededRandomSource(1));

            // turnTimeLimitSeconds = 0.05f，等待數個影格讓 Update 推進計時器超過限時。
            float elapsed = 0f;
            while (elapsed < 1f && endedReason == null)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.AreEqual(TurnEndReason.TimedOut, endedReason);
        }

        [Test]
        public void NotifyCurrentTurnFired_EndsCurrentTurn()
        {
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));
            _turnManager.StartBattle(participants, 0, new SeededRandomSource(1));

            var firstOwner = _turnManager.CurrentTurnOwner;
            TurnEndReason? reason = null;
            _turnManager.OnTurnEnded += (p, r) => reason = r;

            _turnManager.NotifyCurrentTurnFired();

            Assert.AreEqual(TurnEndReason.Fired, reason);
            Assert.AreNotEqual(firstOwner, _turnManager.CurrentTurnOwner);
        }
    }
}
