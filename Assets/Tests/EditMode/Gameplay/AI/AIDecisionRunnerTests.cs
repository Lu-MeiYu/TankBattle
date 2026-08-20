using System.Threading;
using NUnit.Framework;
using TankBattle.Core.AI;
using TankBattle.Core.Shared;
using TankBattle.Data;
using TankBattle.Gameplay.AI;
using TankBattle.Tests.EditMode.AI.Fakes;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.AI
{
    public class AIDecisionRunnerTests
    {
        private const float Gravity = 9.8f;
        private const float MuzzleSpeed = 40f;

        private static AIDifficultySettings NoErrorSettings(int maxIterations = 20)
        {
            return new AIDifficultySettings
            {
                aimAngleErrorDegrees = 0f,
                aimPowerErrorPercent = 0f,
                windAccuracy = 1f,
                maxSearchIterations = maxIterations
            };
        }

        [Test]
        public void Constructor_ThrowsWhenRandomIsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => new AIDecisionRunner(null));
        }

        [Test]
        public void DecideTurn_ThrowsWhenStrategyIsNull()
        {
            var runner = new AIDecisionRunner(new FakeRandomSource());
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);

            Assert.Throws<System.ArgumentNullException>(() =>
                runner.DecideTurn(null, self, new ITankState[0], WindData.Zero, Gravity, null, 5f));
        }

        [Test]
        public void DecideTurn_ThrowsWhenSelfIsNull()
        {
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new NormalAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);
            var runner = new AIDecisionRunner(new FakeRandomSource());

            Assert.Throws<System.ArgumentNullException>(() =>
                runner.DecideTurn(strategy, null, new ITankState[0], WindData.Zero, Gravity, null, 5f));
        }

        [Test]
        public void DecideTurn_SelectsTargetAndReturnsSuccessfulAim()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new NormalAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);
            var runner = new AIDecisionRunner(new FakeRandomSource(0f));

            AITurnResult result = runner.DecideTurn(strategy, self, new ITankState[] { target }, WindData.Zero,
                Gravity, terrain, decisionTimeoutSeconds: 5f);

            Assert.AreSame(target, result.Target);
            Assert.IsTrue(result.Aim.Success);
        }

        [Test]
        public void DecideTurn_NonPositiveTimeout_DoesNotCancelBeforehand()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new NormalAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);
            var runner = new AIDecisionRunner(new FakeRandomSource(0f));

            AITurnResult result = runner.DecideTurn(strategy, self, new ITankState[] { target }, WindData.Zero,
                Gravity, terrain, decisionTimeoutSeconds: 0f);

            Assert.IsTrue(result.Aim.Success);
        }
    }
}
