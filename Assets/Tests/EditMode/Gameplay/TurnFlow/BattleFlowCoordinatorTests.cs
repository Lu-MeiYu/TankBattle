using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;
using TankBattle.Gameplay.TurnFlow;

namespace TankBattle.Tests.EditMode.Gameplay.TurnFlow
{
    [TestFixture]
    public class BattleFlowCoordinatorTests
    {
        private static BattleFlowCoordinator CreateCoordinator(int playerTankId = 0)
        {
            var orderService = new TurnOrderService();
            var timer = new TurnTimer(30f);
            var controller = new TurnFlowController(orderService, timer);
            var evaluator = new MatchOutcomeEvaluator();
            return new BattleFlowCoordinator(orderService, controller, evaluator, playerTankId);
        }

        private static List<BattleParticipant> CreateParticipants(params FakeBattleTank[] tanks)
        {
            return tanks.Select(t => new BattleParticipant(t, t)).ToList();
        }

        [Test]
        public void Constructor_WithNullDependencies_Throws()
        {
            var orderService = new TurnOrderService();
            var timer = new TurnTimer(30f);
            var controller = new TurnFlowController(orderService, timer);
            var evaluator = new MatchOutcomeEvaluator();

            Assert.Throws<ArgumentNullException>(() => new BattleFlowCoordinator(null, controller, evaluator, 0));
            Assert.Throws<ArgumentNullException>(() => new BattleFlowCoordinator(orderService, null, evaluator, 0));
            Assert.Throws<ArgumentNullException>(() => new BattleFlowCoordinator(orderService, controller, null, 0));
        }

        [Test]
        public void StartBattle_WithNullParticipants_Throws()
        {
            var coordinator = CreateCoordinator();
            Assert.Throws<ArgumentNullException>(() => coordinator.StartBattle(null, new SeededRandomSource(1)));
        }

        [Test]
        public void StartBattle_WithEmptyParticipants_Throws()
        {
            var coordinator = CreateCoordinator();
            Assert.Throws<ArgumentException>(() =>
                coordinator.StartBattle(new List<BattleParticipant>(), new SeededRandomSource(1)));
        }

        [Test]
        public void StartBattle_WithNullRandom_Throws()
        {
            var coordinator = CreateCoordinator();
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));
            Assert.Throws<ArgumentNullException>(() => coordinator.StartBattle(participants, null));
        }

        [Test]
        public void StartBattle_CalledTwice_Throws()
        {
            var coordinator = CreateCoordinator();
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            Assert.Throws<InvalidOperationException>(() =>
                coordinator.StartBattle(participants, new SeededRandomSource(1)));
        }

        [Test]
        public void StartBattle_RaisesFirstOnTurnStarted()
        {
            var coordinator = CreateCoordinator();
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1), new FakeBattleTank(2));

            ITurnParticipant started = null;
            coordinator.OnTurnStarted += p => started = p;

            coordinator.StartBattle(participants, new SeededRandomSource(1));

            Assert.IsNotNull(started);
            Assert.AreEqual(coordinator.CurrentTurnOwner, started);
        }

        [Test]
        public void StartBattle_WithSingleParticipant_ImmediatelyEndsAsPlayerVictory()
        {
            var coordinator = CreateCoordinator(playerTankId: 0);
            var participants = CreateParticipants(new FakeBattleTank(0));

            MatchOutcome? outcome = null;
            coordinator.OnMatchEnded += o => outcome = o;
            bool turnStarted = false;
            coordinator.OnTurnStarted += _ => turnStarted = true;

            coordinator.StartBattle(participants, new SeededRandomSource(1));

            Assert.AreEqual(MatchOutcome.PlayerVictory, outcome);
            Assert.IsTrue(coordinator.IsMatchOver);
            Assert.IsFalse(turnStarted, "只剩一名參與者時不應開始任何回合");
        }

        [Test]
        public void NotifyCurrentTurnFired_AdvancesToNextTurnAndRaisesEvents()
        {
            var coordinator = CreateCoordinator();
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1), new FakeBattleTank(2));
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            var firstOwner = coordinator.CurrentTurnOwner;
            (ITurnParticipant p, TurnEndReason r)? ended = null;
            coordinator.OnTurnEnded += (p, r) => ended = (p, r);
            ITurnParticipant secondStarted = null;
            coordinator.OnTurnStarted += p => secondStarted = p;

            coordinator.NotifyCurrentTurnFired();

            Assert.IsTrue(ended.HasValue);
            Assert.AreEqual(firstOwner, ended.Value.p);
            Assert.AreEqual(TurnEndReason.Fired, ended.Value.r);
            Assert.AreEqual(coordinator.CurrentTurnOwner, secondStarted);
            Assert.AreNotEqual(firstOwner, coordinator.CurrentTurnOwner);
        }

        [Test]
        public void Tick_WhenTimerExpires_AutomaticallyEndsTurnAsTimedOut()
        {
            var coordinator = CreateCoordinator();
            var participants = CreateParticipants(new FakeBattleTank(0), new FakeBattleTank(1));
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            var firstOwner = coordinator.CurrentTurnOwner;
            TurnEndReason? reason = null;
            coordinator.OnTurnEnded += (p, r) => reason = r;

            coordinator.Tick(30f);

            Assert.AreEqual(TurnEndReason.TimedOut, reason);
            Assert.AreNotEqual(firstOwner, coordinator.CurrentTurnOwner);
        }

        [Test]
        public void Tick_BeforeStartBattle_DoesNothing()
        {
            var coordinator = CreateCoordinator();
            Assert.DoesNotThrow(() => coordinator.Tick(1f));
        }

        [Test]
        public void NotifyCurrentTurnFired_BeforeStartBattle_DoesNothing()
        {
            var coordinator = CreateCoordinator();
            Assert.DoesNotThrow(() => coordinator.NotifyCurrentTurnFired());
        }

        [Test]
        public void TankElimination_RemovesFromTurnOrderAndSkipsOnAdvance()
        {
            var coordinator = CreateCoordinator();
            var tankA = new FakeBattleTank(0);
            var tankB = new FakeBattleTank(1);
            var tankC = new FakeBattleTank(2);
            var participants = CreateParticipants(tankA, tankB, tankC);
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            // Kill whichever tank is not currently acting, to verify elimination handling
            // independent of turn progression.
            var owner = (BattleParticipant)coordinator.CurrentTurnOwner;
            var victim = participants.First(p => p != owner);

            ((FakeBattleTank)victim.Health).Kill();

            coordinator.NotifyCurrentTurnFired();
            coordinator.NotifyCurrentTurnFired();

            // After cycling through remaining turns, the eliminated tank should never become current.
            Assert.AreNotEqual(victim.State.TankId, coordinator.CurrentTurnOwner.State.TankId);
        }

        [Test]
        public void PlayerElimination_ImmediatelyEndsMatchAsDefeat_EvenMidTurn()
        {
            var coordinator = CreateCoordinator(playerTankId: 0);
            var player = new FakeBattleTank(0);
            var ai1 = new FakeBattleTank(1);
            var ai2 = new FakeBattleTank(2);
            var participants = CreateParticipants(player, ai1, ai2);
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            MatchOutcome? outcome = null;
            coordinator.OnMatchEnded += o => outcome = o;

            player.Kill();

            Assert.AreEqual(MatchOutcome.PlayerDefeat, outcome);
            Assert.IsTrue(coordinator.IsMatchOver);
        }

        [Test]
        public void OnlyOneSurvivorRemaining_EndsMatchAsPlayerVictory()
        {
            var coordinator = CreateCoordinator(playerTankId: 0);
            var player = new FakeBattleTank(0);
            var ai1 = new FakeBattleTank(1);
            var ai2 = new FakeBattleTank(2);
            var participants = CreateParticipants(player, ai1, ai2);
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            MatchOutcome? outcome = null;
            coordinator.OnMatchEnded += o => outcome = o;

            ai1.Kill();
            ai2.Kill();

            Assert.AreEqual(MatchOutcome.PlayerVictory, outcome);
            Assert.IsTrue(coordinator.IsMatchOver);
        }

        [Test]
        public void AfterMatchEnded_TickAndNotifyAreNoOps()
        {
            var coordinator = CreateCoordinator(playerTankId: 0);
            var player = new FakeBattleTank(0);
            var ai1 = new FakeBattleTank(1);
            var participants = CreateParticipants(player, ai1);
            coordinator.StartBattle(participants, new SeededRandomSource(1));

            ai1.Kill();
            Assert.IsTrue(coordinator.IsMatchOver);

            int turnEndedCount = 0;
            coordinator.OnTurnEnded += (p, r) => turnEndedCount++;

            Assert.DoesNotThrow(() => coordinator.Tick(100f));
            Assert.DoesNotThrow(() => coordinator.NotifyCurrentTurnFired());
            Assert.AreEqual(0, turnEndedCount);
        }
    }
}
