using System;
using System.Linq;
using NUnit.Framework;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    [TestFixture]
    public class TankSpawnDistributorTests
    {
        [Test]
        public void DistributeSpawnPositions_WithZeroTankCount_Throws()
        {
            var distributor = new TankSpawnDistributor();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                distributor.DistributeSpawnPositions(0, 20f, 2f));
        }

        [Test]
        public void DistributeSpawnPositions_WithNegativeTankCount_Throws()
        {
            var distributor = new TankSpawnDistributor();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                distributor.DistributeSpawnPositions(-1, 20f, 2f));
        }

        [Test]
        public void DistributeSpawnPositions_WithZeroMapWidth_Throws()
        {
            var distributor = new TankSpawnDistributor();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                distributor.DistributeSpawnPositions(3, 0f, 2f));
        }

        [Test]
        public void DistributeSpawnPositions_WithNegativeMinSafeSpacing_Throws()
        {
            var distributor = new TankSpawnDistributor();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                distributor.DistributeSpawnPositions(3, 20f, -1f));
        }

        [Test]
        public void DistributeSpawnPositions_WithSingleTank_ReturnsMapCenter()
        {
            var distributor = new TankSpawnDistributor();
            var positions = distributor.DistributeSpawnPositions(1, 20f, 2f);

            Assert.AreEqual(1, positions.Count);
            Assert.AreEqual(10f, positions[0], 0.0001f);
        }

        [Test]
        public void DistributeSpawnPositions_ReturnsEvenlySpacedPositionsWithinMapBounds()
        {
            var distributor = new TankSpawnDistributor();
            float mapWidth = 40f;
            var positions = distributor.DistributeSpawnPositions(4, mapWidth, 2f);

            Assert.AreEqual(4, positions.Count);
            CollectionAssert.AreEqual(new[] { 5f, 15f, 25f, 35f }, positions.ToArray());

            foreach (var p in positions)
            {
                Assert.GreaterOrEqual(p, 0f);
                Assert.LessOrEqual(p, mapWidth);
            }
        }

        [Test]
        public void DistributeSpawnPositions_AdjacentSpacingMeetsMinSafeSpacing()
        {
            var distributor = new TankSpawnDistributor();
            var positions = distributor.DistributeSpawnPositions(5, 50f, 5f);

            for (int i = 1; i < positions.Count; i++)
            {
                float spacing = positions[i] - positions[i - 1];
                Assert.GreaterOrEqual(spacing, 5f - 0.0001f);
            }
        }

        [Test]
        public void DistributeSpawnPositions_WhenMapTooNarrowForMinSafeSpacing_Throws()
        {
            var distributor = new TankSpawnDistributor();
            Assert.Throws<InvalidOperationException>(() =>
                distributor.DistributeSpawnPositions(10, 20f, 5f));
        }
    }
}
