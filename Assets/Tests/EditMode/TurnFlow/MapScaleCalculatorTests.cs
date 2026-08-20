using System;
using NUnit.Framework;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    [TestFixture]
    public class MapScaleCalculatorTests
    {
        [Test]
        public void Constructor_WithNegativeBaseWidth_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MapScaleCalculator(-1f, 3f));
        }

        [Test]
        public void Constructor_WithNegativeUnitSpacing_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MapScaleCalculator(20f, -1f));
        }

        [Test]
        public void CalculateMapWidth_WithSingleTank_ReturnsBaseWidth()
        {
            var calculator = new MapScaleCalculator(20f, 3f);
            Assert.AreEqual(20f, calculator.CalculateMapWidth(1), 0.0001f);
        }

        [Test]
        public void CalculateMapWidth_MatchesFormula()
        {
            var calculator = new MapScaleCalculator(20f, 3f);
            // N = 11 (player + 10 AI): 20 + (11 - 1) * 3 = 50
            Assert.AreEqual(50f, calculator.CalculateMapWidth(11), 0.0001f);
        }

        [Test]
        public void CalculateMapWidth_WithZeroTankCount_Throws()
        {
            var calculator = new MapScaleCalculator(20f, 3f);
            Assert.Throws<ArgumentOutOfRangeException>(() => calculator.CalculateMapWidth(0));
        }

        [Test]
        public void CalculateMapWidth_WithNegativeTankCount_Throws()
        {
            var calculator = new MapScaleCalculator(20f, 3f);
            Assert.Throws<ArgumentOutOfRangeException>(() => calculator.CalculateMapWidth(-2));
        }

        [Test]
        public void CalculateMapWidth_WithZeroUnitSpacing_IsConstantAcrossTankCounts()
        {
            var calculator = new MapScaleCalculator(20f, 0f);
            Assert.AreEqual(20f, calculator.CalculateMapWidth(1), 0.0001f);
            Assert.AreEqual(20f, calculator.CalculateMapWidth(10), 0.0001f);
        }
    }
}
