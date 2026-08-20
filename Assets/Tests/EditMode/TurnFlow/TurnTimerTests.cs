using System;
using NUnit.Framework;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    [TestFixture]
    public class TurnTimerTests
    {
        [Test]
        public void Constructor_WithZeroDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TurnTimer(0f));
        }

        [Test]
        public void Constructor_WithNegativeDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TurnTimer(-5f));
        }

        [Test]
        public void StartTurn_ResetsRemainingToFullDuration()
        {
            var timer = new TurnTimer(30f);
            timer.Tick(10f);
            timer.StartTurn();

            Assert.AreEqual(30f, timer.RemainingSeconds, 0.0001f);
            Assert.IsFalse(timer.HasExpired);
        }

        [Test]
        public void Tick_ReducesRemainingSeconds()
        {
            var timer = new TurnTimer(30f);
            timer.StartTurn();
            timer.Tick(10f);

            Assert.AreEqual(20f, timer.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void Tick_WithNegativeDeltaTime_Throws()
        {
            var timer = new TurnTimer(30f);
            timer.StartTurn();
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(-1f));
        }

        [Test]
        public void HasExpired_BecomesTrueExactlyAtDuration()
        {
            var timer = new TurnTimer(5f);
            timer.StartTurn();
            timer.Tick(5f);

            Assert.IsTrue(timer.HasExpired);
            Assert.AreEqual(0f, timer.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void RemainingSeconds_NeverGoesNegative_WhenOvershooting()
        {
            var timer = new TurnTimer(5f);
            timer.StartTurn();
            timer.Tick(100f);

            Assert.AreEqual(0f, timer.RemainingSeconds, 0.0001f);
            Assert.IsTrue(timer.HasExpired);
        }
    }
}
