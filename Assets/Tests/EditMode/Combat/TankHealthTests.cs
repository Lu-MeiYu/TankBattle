using NUnit.Framework;
using TankBattle.Core.Combat;

namespace TankBattle.Tests.EditMode.Combat
{
    [TestFixture]
    public class TankHealthTests
    {
        [Test]
        public void Constructor_SetsCurrentHpToMaxHp()
        {
            var health = new TankHealth(100);

            Assert.AreEqual(100, health.MaxHp);
            Assert.AreEqual(100, health.CurrentHp);
            Assert.IsFalse(health.IsEliminated);
        }

        [Test]
        public void Constructor_NonPositiveMaxHp_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TankHealth(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TankHealth(-10));
        }

        [Test]
        public void TakeDamage_ReducesCurrentHp()
        {
            var health = new TankHealth(100);

            health.TakeDamage(30f);

            Assert.AreEqual(70, health.CurrentHp);
            Assert.IsFalse(health.IsEliminated);
        }

        [Test]
        public void TakeDamage_NonPositiveDamage_IsNoOp()
        {
            var health = new TankHealth(100);

            health.TakeDamage(0f);
            health.TakeDamage(-5f);

            Assert.AreEqual(100, health.CurrentHp);
        }

        [Test]
        public void TakeDamage_ExceedingCurrentHp_ClampsToZeroAndEliminates()
        {
            var health = new TankHealth(50);

            health.TakeDamage(999f);

            Assert.AreEqual(0, health.CurrentHp);
            Assert.IsTrue(health.IsEliminated);
        }

        [Test]
        public void TakeDamage_CrossingZero_FiresOnEliminatedExactlyOnce()
        {
            var health = new TankHealth(10);
            int firedCount = 0;
            health.OnEliminated += _ => firedCount++;

            health.TakeDamage(15f);
            health.TakeDamage(15f);
            health.TakeDamage(15f);

            Assert.AreEqual(1, firedCount);
            Assert.AreEqual(0, health.CurrentHp);
        }

        [Test]
        public void TakeDamage_AfterEliminated_DoesNotChangeStateOrRefireEvent()
        {
            var health = new TankHealth(10);
            int firedCount = 0;
            health.OnEliminated += _ => firedCount++;

            health.TakeDamage(10f);
            Assert.IsTrue(health.IsEliminated);

            health.TakeDamage(5f);

            Assert.AreEqual(1, firedCount);
            Assert.AreEqual(0, health.CurrentHp);
        }

        [Test]
        public void OnEliminated_PassesSelfAsArgument()
        {
            var health = new TankHealth(10);
            ITankHealth received = null;
            health.OnEliminated += h => received = h;

            health.TakeDamage(10f);

            Assert.AreSame(health, received);
        }
    }
}
