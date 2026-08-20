using NUnit.Framework;
using TankBattle.Core.Combat;

namespace TankBattle.Tests.EditMode.Combat
{
    [TestFixture]
    public class DamageCalculatorTests
    {
        private DamageCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new DamageCalculator();
        }

        [Test]
        public void CalculateDamage_AtExplosionCenter_ReturnsFullBaseDamageTimesFirepower()
        {
            var context = new DamageContext(baseDamage: 50f, firepowerMultiplier: 2f,
                explosionRadius: 5f, distanceFromCenter: 0f);

            float damage = _calculator.CalculateDamage(context);

            Assert.AreEqual(100f, damage, 0.001f);
        }

        [Test]
        public void CalculateDamage_AtExplosionEdge_ReturnsZero()
        {
            var context = new DamageContext(baseDamage: 50f, firepowerMultiplier: 1f,
                explosionRadius: 5f, distanceFromCenter: 5f);

            float damage = _calculator.CalculateDamage(context);

            Assert.AreEqual(0f, damage, 0.001f);
        }

        [Test]
        public void CalculateDamage_BeyondExplosionRadius_ReturnsZero()
        {
            var context = new DamageContext(baseDamage: 50f, firepowerMultiplier: 1f,
                explosionRadius: 5f, distanceFromCenter: 10f);

            float damage = _calculator.CalculateDamage(context);

            Assert.AreEqual(0f, damage);
        }

        [Test]
        public void CalculateDamage_HalfwayToRadius_ReturnsHalfDamage_ForLinearFalloff()
        {
            var context = new DamageContext(baseDamage: 100f, firepowerMultiplier: 1f,
                explosionRadius: 10f, distanceFromCenter: 5f);

            float damage = _calculator.CalculateDamage(context);

            Assert.AreEqual(50f, damage, 0.001f);
        }

        [Test]
        public void CalculateDamage_ZeroRadius_IsNoOpAndReturnsZero()
        {
            var context = new DamageContext(baseDamage: 100f, firepowerMultiplier: 1f,
                explosionRadius: 0f, distanceFromCenter: 0f);

            float damage = _calculator.CalculateDamage(context);

            Assert.AreEqual(0f, damage);
        }

        [Test]
        public void CalculateDamage_NegativeRadius_ReturnsZero()
        {
            var context = new DamageContext(baseDamage: 100f, firepowerMultiplier: 1f,
                explosionRadius: -1f, distanceFromCenter: 0f);

            float damage = _calculator.CalculateDamage(context);

            Assert.AreEqual(0f, damage);
        }

        [Test]
        public void CalculateDamage_NeverReturnsNegativeValue()
        {
            var context = new DamageContext(baseDamage: -50f, firepowerMultiplier: 1f,
                explosionRadius: 10f, distanceFromCenter: 1f);

            float damage = _calculator.CalculateDamage(context);

            Assert.GreaterOrEqual(damage, 0f);
        }

        [Test]
        public void CalculateDamage_WithCustomFalloffExponent_AppliesExponentCurve()
        {
            var steepCalculator = new DamageCalculator(falloffExponent: 2f);
            var context = new DamageContext(baseDamage: 100f, firepowerMultiplier: 1f,
                explosionRadius: 10f, distanceFromCenter: 5f);

            // (1 - 0.5)^2 * 100 = 25
            float damage = steepCalculator.CalculateDamage(context);

            Assert.AreEqual(25f, damage, 0.001f);
        }

        [Test]
        public void CalculateDamage_NonPositiveFalloffExponent_FallsBackToLinear()
        {
            var linearFallback = new DamageCalculator(falloffExponent: 0f);
            var context = new DamageContext(baseDamage: 100f, firepowerMultiplier: 1f,
                explosionRadius: 10f, distanceFromCenter: 5f);

            float damage = linearFallback.CalculateDamage(context);

            Assert.AreEqual(50f, damage, 0.001f);
        }
    }
}
