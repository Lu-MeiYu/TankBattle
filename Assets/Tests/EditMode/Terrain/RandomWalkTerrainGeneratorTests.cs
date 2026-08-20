using NUnit.Framework;
using TankBattle.Core.Terrain;

namespace TankBattle.Tests.EditMode.Terrain
{
    [TestFixture]
    public class RandomWalkTerrainGeneratorTests
    {
        private RandomWalkTerrainGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new RandomWalkTerrainGenerator();
        }

        [Test]
        public void GenerateHeights_ReturnsArrayOfRequestedResolution()
        {
            var settings = new TerrainGenerationSettings(minHeight: 0f, maxHeight: 10f, maxStepPerColumn: 3f);
            var random = new FakeRandomSource(5f, 1f, 1f, 1f);

            float[] heights = _generator.GenerateHeights(6, settings, random);

            Assert.AreEqual(6, heights.Length);
        }

        [Test]
        public void GenerateHeights_ResolutionLessThanTwo_Throws()
        {
            var settings = new TerrainGenerationSettings(0f, 10f, 3f);
            var random = new FakeRandomSource(5f);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _generator.GenerateHeights(1, settings, random));
        }

        [Test]
        public void GenerateHeights_NullRandom_Throws()
        {
            var settings = new TerrainGenerationSettings(0f, 10f, 3f);

            Assert.Throws<System.ArgumentNullException>(() =>
                _generator.GenerateHeights(4, settings, null));
        }

        [Test]
        public void GenerateHeights_WithFixedRandomSource_IsDeterministicAndReproducible()
        {
            var settings = new TerrainGenerationSettings(0f, 10f, 3f);

            float[] first = _generator.GenerateHeights(4, settings, new FakeRandomSource(5f, 2f, -1f, 3f));
            float[] second = _generator.GenerateHeights(4, settings, new FakeRandomSource(5f, 2f, -1f, 3f));

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void GenerateHeights_AccumulatesDeltasAsRandomWalk()
        {
            var settings = new TerrainGenerationSettings(0f, 100f, 10f);
            var random = new FakeRandomSource(5f, 2f, -1f, 3f);

            float[] heights = _generator.GenerateHeights(4, settings, random);

            Assert.AreEqual(5f, heights[0], 0.001f);
            Assert.AreEqual(7f, heights[1], 0.001f);
            Assert.AreEqual(6f, heights[2], 0.001f);
            Assert.AreEqual(9f, heights[3], 0.001f);
        }

        [Test]
        public void GenerateHeights_ClampsWithinMinMaxHeightRange()
        {
            var settings = new TerrainGenerationSettings(minHeight: 0f, maxHeight: 10f, maxStepPerColumn: 3f);
            // Second delta of 5 (larger than declared maxStep, simulating an out-of-range sample)
            // should still be clamped by the generator's own min/max guard.
            var random = new FakeRandomSource(9f, 5f);

            float[] heights = _generator.GenerateHeights(2, settings, random);

            Assert.AreEqual(9f, heights[0], 0.001f);
            Assert.AreEqual(10f, heights[1], 0.001f);
        }

        [Test]
        public void GenerateHeights_SwappedMinMax_StillProducesValuesWithinNormalizedRange()
        {
            // MinHeight > MaxHeight (misconfigured), generator should normalize internally.
            var settings = new TerrainGenerationSettings(minHeight: 10f, maxHeight: 0f, maxStepPerColumn: 2f);
            var random = new FakeRandomSource(5f, 1f, 1f);

            float[] heights = _generator.GenerateHeights(3, settings, random);

            foreach (float h in heights)
            {
                Assert.GreaterOrEqual(h, 0f);
                Assert.LessOrEqual(h, 10f);
            }
        }
    }
}
