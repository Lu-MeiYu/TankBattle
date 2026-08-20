using System;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Core.Terrain;
using TankBattle.Core.TurnFlow;
using TankBattle.Gameplay.Map;

namespace TankBattle.Tests.EditMode.Gameplay.Map
{
    [TestFixture]
    public class MapGeneratorTests
    {
        private static MapGenerator CreateGenerator()
        {
            return new MapGenerator(new MapScaleCalculator(20f, 3f), new TankSpawnDistributor(),
                new RandomWalkTerrainGenerator());
        }

        [Test]
        public void Constructor_WithNullDependencies_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MapGenerator(null, new TankSpawnDistributor(), new RandomWalkTerrainGenerator()));
            Assert.Throws<ArgumentNullException>(() =>
                new MapGenerator(new MapScaleCalculator(20f, 3f), null, new RandomWalkTerrainGenerator()));
            Assert.Throws<ArgumentNullException>(() =>
                new MapGenerator(new MapScaleCalculator(20f, 3f), new TankSpawnDistributor(), null));
        }

        [Test]
        public void Generate_WithZeroTankCount_Throws()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                generator.Generate(0, 2f, 32, settings, 0f, 30f, 1f, new SeededRandomSource(1)));
        }

        [Test]
        public void Generate_WithNullRandom_Throws()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            Assert.Throws<ArgumentNullException>(() =>
                generator.Generate(4, 2f, 32, settings, 0f, 30f, 1f, null));
        }

        [Test]
        public void Generate_MapWidthMatchesMapScaleFormula()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            // N = 4: 20 + (4 - 1) * 3 = 29
            MapGenerationResult result = generator.Generate(4, 2f, 32, settings, 0f, 30f, 1f,
                new SeededRandomSource(1));

            Assert.AreEqual(29f, result.MapWidth, 0.0001f);
        }

        [Test]
        public void Generate_ProducesOneSpawnPositionPerTank()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            MapGenerationResult result = generator.Generate(5, 2f, 32, settings, 0f, 30f, 1f,
                new SeededRandomSource(7));

            Assert.AreEqual(5, result.SpawnPositions.Count);
        }

        [Test]
        public void Generate_SpawnPositionsAreWithinMapBounds()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            MapGenerationResult result = generator.Generate(6, 2f, 32, settings, 0f, 30f, 1f,
                new SeededRandomSource(42));

            foreach (var pos in result.SpawnPositions)
            {
                Assert.GreaterOrEqual(pos.x, 0f);
                Assert.LessOrEqual(pos.x, result.MapWidth);
            }
        }

        [Test]
        public void Generate_SpawnPositionYIsAboveTerrainSurfaceByClearance()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);
            const float clearance = 1.5f;

            MapGenerationResult result = generator.Generate(3, 2f, 32, settings, 0f, 30f, clearance,
                new SeededRandomSource(123));

            foreach (var pos in result.SpawnPositions)
            {
                float surfaceHeight = result.Terrain.GetSurfaceHeight(pos.x);
                Assert.AreEqual(surfaceHeight + clearance, pos.y, 0.0001f);
            }
        }

        [Test]
        public void Generate_TerrainHeightsAreWithinConfiguredRange()
        {
            var generator = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            MapGenerationResult result = generator.Generate(4, 2f, 16, settings, 0f, 30f, 1f,
                new SeededRandomSource(5));

            for (float x = 0f; x <= result.MapWidth; x += result.MapWidth / 20f)
            {
                float height = result.Terrain.GetSurfaceHeight(x);
                Assert.GreaterOrEqual(height, 2f - 0.0001f);
                Assert.LessOrEqual(height, 10f + 0.0001f);
            }
        }

        [Test]
        public void Generate_WithSameSeed_IsDeterministic()
        {
            var generatorA = CreateGenerator();
            var generatorB = CreateGenerator();
            var settings = new TerrainGenerationSettings(2f, 10f, 1.5f);

            MapGenerationResult resultA = generatorA.Generate(4, 2f, 16, settings, 0f, 30f, 1f,
                new SeededRandomSource(999));
            MapGenerationResult resultB = generatorB.Generate(4, 2f, 16, settings, 0f, 30f, 1f,
                new SeededRandomSource(999));

            Assert.AreEqual(resultA.MapWidth, resultB.MapWidth, 0.0001f);
            for (int i = 0; i < resultA.SpawnPositions.Count; i++)
            {
                Assert.AreEqual(resultA.SpawnPositions[i], resultB.SpawnPositions[i]);
            }
        }
    }
}
