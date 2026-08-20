using System;
using System.Collections.Generic;
using NUnit.Framework;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    [TestFixture]
    public class MatchOutcomeEvaluatorTests
    {
        [Test]
        public void Evaluate_WithNullParticipants_Throws()
        {
            var evaluator = new MatchOutcomeEvaluator();
            Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(null, 0));
        }

        [Test]
        public void Evaluate_AllAlive_ReturnsOngoing()
        {
            var evaluator = new MatchOutcomeEvaluator();
            var participants = new List<FakeTurnParticipant>
            {
                new FakeTurnParticipant(new FakeTankState(0)),
                new FakeTurnParticipant(new FakeTankState(1)),
                new FakeTurnParticipant(new FakeTankState(2)),
            };

            var result = evaluator.Evaluate(participants.ConvertAll(p => (ITurnParticipant)p), playerTankId: 0);

            Assert.AreEqual(MatchOutcome.Ongoing, result);
        }

        [Test]
        public void Evaluate_PlayerEliminated_ReturnsPlayerDefeatImmediately()
        {
            var evaluator = new MatchOutcomeEvaluator();
            var player = new FakeTankState(0);
            var ai1 = new FakeTankState(1);
            var ai2 = new FakeTankState(2);
            player.Kill();

            var participants = new List<ITurnParticipant>
            {
                new FakeTurnParticipant(player),
                new FakeTurnParticipant(ai1),
                new FakeTurnParticipant(ai2),
            };

            var result = evaluator.Evaluate(participants, playerTankId: 0);

            Assert.AreEqual(MatchOutcome.PlayerDefeat, result);
        }

        [Test]
        public void Evaluate_OnlyPlayerAlive_ReturnsPlayerVictory()
        {
            var evaluator = new MatchOutcomeEvaluator();
            var player = new FakeTankState(0);
            var ai1 = new FakeTankState(1);
            var ai2 = new FakeTankState(2);
            ai1.Kill();
            ai2.Kill();

            var participants = new List<ITurnParticipant>
            {
                new FakeTurnParticipant(player),
                new FakeTurnParticipant(ai1),
                new FakeTurnParticipant(ai2),
            };

            var result = evaluator.Evaluate(participants, playerTankId: 0);

            Assert.AreEqual(MatchOutcome.PlayerVictory, result);
        }

        [Test]
        public void Evaluate_PlayerAliveWithMultipleSurvivors_ReturnsOngoing()
        {
            var evaluator = new MatchOutcomeEvaluator();
            var player = new FakeTankState(0);
            var ai1 = new FakeTankState(1);
            var ai2 = new FakeTankState(2);
            ai2.Kill();

            var participants = new List<ITurnParticipant>
            {
                new FakeTurnParticipant(player),
                new FakeTurnParticipant(ai1),
                new FakeTurnParticipant(ai2),
            };

            var result = evaluator.Evaluate(participants, playerTankId: 0);

            Assert.AreEqual(MatchOutcome.Ongoing, result);
        }

        [Test]
        public void GetEliminationRanking_AccumulatesNewlyEliminatedTanksInOrder()
        {
            var evaluator = new MatchOutcomeEvaluator();
            var player = new FakeTankState(0);
            var ai1 = new FakeTankState(1);
            var ai2 = new FakeTankState(2);

            var participants = new List<ITurnParticipant>
            {
                new FakeTurnParticipant(player),
                new FakeTurnParticipant(ai1),
                new FakeTurnParticipant(ai2),
            };

            evaluator.Evaluate(participants, playerTankId: 0);
            CollectionAssert.IsEmpty(evaluator.GetEliminationRanking());

            ai2.Kill();
            evaluator.Evaluate(participants, playerTankId: 0);
            CollectionAssert.AreEqual(new[] { 2 }, evaluator.GetEliminationRanking());

            ai1.Kill();
            evaluator.Evaluate(participants, playerTankId: 0);
            CollectionAssert.AreEqual(new[] { 2, 1 }, evaluator.GetEliminationRanking());

            // Re-evaluating without new eliminations should not duplicate entries.
            evaluator.Evaluate(participants, playerTankId: 0);
            CollectionAssert.AreEqual(new[] { 2, 1 }, evaluator.GetEliminationRanking());
        }

        [Test]
        public void Evaluate_EmptyParticipantList_ReturnsPlayerDefeat()
        {
            var evaluator = new MatchOutcomeEvaluator();
            var result = evaluator.Evaluate(new List<ITurnParticipant>(), playerTankId: 0);

            Assert.AreEqual(MatchOutcome.PlayerDefeat, result);
        }
    }
}
