using NUnit.Framework;
using TankBattle.Core.Terrain;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Terrain
{
    [TestFixture]
    public class HeightmapTerrainTests
    {
        private HeightmapTerrain CreateFlatTerrain(float height = 5f, int resolution = 11,
            float mapWidth = 20f)
        {
            var heights = new float[resolution];
            for (int i = 0; i < resolution; i++)
            {
                heights[i] = height;
            }

            return new HeightmapTerrain(mapWidth, heights, worldMinY: 0f, worldMaxY: 30f);
        }

        [Test]
        public void Constructor_TooFewHeightSamples_Throws()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new HeightmapTerrain(10f, new float[] { 1f }, 0f, 20f));
        }

        [Test]
        public void Constructor_NonPositiveMapWidth_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new HeightmapTerrain(0f, new float[] { 1f, 2f }, 0f, 20f));
        }

        [Test]
        public void Constructor_WorldMaxNotGreaterThanMin_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new HeightmapTerrain(10f, new float[] { 1f, 2f }, 10f, 5f));
        }

        [Test]
        public void GetSurfaceHeight_FlatTerrain_ReturnsConstantHeight()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 7f);

            Assert.AreEqual(7f, terrain.GetSurfaceHeight(0f), 0.001f);
            Assert.AreEqual(7f, terrain.GetSurfaceHeight(10f), 0.001f);
            Assert.AreEqual(7f, terrain.GetSurfaceHeight(20f), 0.001f);
        }

        [Test]
        public void GetSurfaceHeight_XOutsideMapBounds_ClampsToNearestEdge()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 7f, mapWidth: 20f);

            Assert.AreEqual(7f, terrain.GetSurfaceHeight(-100f), 0.001f);
            Assert.AreEqual(7f, terrain.GetSurfaceHeight(1000f), 0.001f);
        }

        [Test]
        public void GetSurfaceHeight_InterpolatesLinearlyBetweenSamples()
        {
            // Two columns, resolution = 2, mapWidth = 10 => column width 10.
            var heights = new float[] { 0f, 10f };
            var terrain = new HeightmapTerrain(10f, heights, 0f, 20f);

            Assert.AreEqual(5f, terrain.GetSurfaceHeight(5f), 0.001f);
        }

        [Test]
        public void IsSolidAt_BelowSurfaceHeight_ReturnsTrue()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f);

            Assert.IsTrue(terrain.IsSolidAt(new Vector2(5f, 2f)));
        }

        [Test]
        public void IsSolidAt_AboveSurfaceHeight_ReturnsFalse()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f);

            Assert.IsFalse(terrain.IsSolidAt(new Vector2(5f, 8f)));
        }

        [Test]
        public void IsSolidAt_AtWorldFloor_ReturnsTrue()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f);

            Assert.IsTrue(terrain.IsSolidAt(new Vector2(5f, 0f)));
        }

        [Test]
        public void GetWorldBounds_ReturnsExpectedRect()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(mapWidth: 20f);

            Rect bounds = terrain.GetWorldBounds();

            Assert.AreEqual(0f, bounds.xMin, 0.001f);
            Assert.AreEqual(20f, bounds.xMax, 0.001f);
            Assert.AreEqual(0f, bounds.yMin, 0.001f);
            Assert.AreEqual(30f, bounds.yMax, 0.001f);
        }

        [Test]
        public void CarveCrater_ZeroOrNegativeRadius_IsNoOp()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f);

            var resultZero = terrain.CarveCrater(new Vector2(5f, 5f), 0f);
            var resultNegative = terrain.CarveCrater(new Vector2(5f, 5f), -3f);

            Assert.AreEqual(0, resultZero.RemovedRegionBounds.Count);
            Assert.IsFalse(resultZero.WasClampedToMapBounds);
            Assert.AreEqual(0, resultNegative.RemovedRegionBounds.Count);
            Assert.AreEqual(5f, terrain.GetSurfaceHeight(5f), 0.001f);
        }

        [Test]
        public void CarveCrater_AtCenterColumn_LowersHeightThere()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, resolution: 21, mapWidth: 20f);

            terrain.CarveCrater(new Vector2(10f, 5f), 3f);

            // At the crater center column, new height should be pushed well below original 5.
            Assert.Less(terrain.GetSurfaceHeight(10f), 5f);
        }

        [Test]
        public void CarveCrater_FarFromExistingSurface_DoesNotRaiseSurface()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, resolution: 21, mapWidth: 20f);

            // Explosion floating high above the ground should not affect terrain at all.
            var result = terrain.CarveCrater(new Vector2(10f, 100f), 3f);

            Assert.AreEqual(0, result.RemovedRegionBounds.Count);
            Assert.AreEqual(5f, terrain.GetSurfaceHeight(10f), 0.001f);
        }

        [Test]
        public void CarveCrater_NearMapEdge_ClampsToMapBoundsAndReportsClamped()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, resolution: 21, mapWidth: 20f);

            var result = terrain.CarveCrater(new Vector2(0f, 5f), 5f);

            Assert.IsTrue(result.WasClampedToMapBounds);
            // Should not throw and should still carve within valid bounds.
            Assert.LessOrEqual(terrain.GetSurfaceHeight(0f), 5f);
        }

        [Test]
        public void CarveCrater_RepeatedOverlappingCraters_TakesUnionAndStaysMonotonicallyLower()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, resolution: 21, mapWidth: 20f);

            terrain.CarveCrater(new Vector2(10f, 5f), 3f);
            float afterFirst = terrain.GetSurfaceHeight(10f);

            terrain.CarveCrater(new Vector2(10f, 5f), 3f);
            float afterSecond = terrain.GetSurfaceHeight(10f);

            Assert.AreEqual(afterFirst, afterSecond, 0.001f);
        }

        [Test]
        public void CarveCrater_DoesNotLowerBelowWorldFloor()
        {
            var heights = new float[21];
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = 1f;
            }
            var terrain = new HeightmapTerrain(20f, heights, worldMinY: 0f, worldMaxY: 30f);

            terrain.CarveCrater(new Vector2(10f, 1f), 50f);

            Assert.GreaterOrEqual(terrain.GetSurfaceHeight(10f), 0f);
        }

        [Test]
        public void TryGetCollision_SegmentEndingUndergroundSurface_ReturnsTrueWithHitPoint()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, mapWidth: 20f);

            bool hit = terrain.TryGetCollision(new Vector2(5f, 20f), new Vector2(5f, -5f),
                out Vector2 hitPoint);

            Assert.IsTrue(hit);
            Assert.AreEqual(5f, hitPoint.y, 0.5f);
        }

        [Test]
        public void TryGetCollision_SegmentEntirelyAboveSurface_ReturnsFalse()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, mapWidth: 20f);

            bool hit = terrain.TryGetCollision(new Vector2(2f, 20f), new Vector2(18f, 15f),
                out Vector2 hitPoint);

            Assert.IsFalse(hit);
            Assert.AreEqual(new Vector2(18f, 15f), hitPoint);
        }

        [Test]
        public void TryGetCollision_FromPointAlreadySolid_ReturnsImmediatelyAtFromPoint()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, mapWidth: 20f);

            bool hit = terrain.TryGetCollision(new Vector2(5f, 1f), new Vector2(5f, 20f),
                out Vector2 hitPoint);

            Assert.IsTrue(hit);
            Assert.AreEqual(new Vector2(5f, 1f), hitPoint);
        }

        [Test]
        public void TryGetCollision_ZeroLengthSegment_TestsSinglePoint()
        {
            HeightmapTerrain terrain = CreateFlatTerrain(height: 5f, mapWidth: 20f);

            bool hitSolid = terrain.TryGetCollision(new Vector2(5f, 1f), new Vector2(5f, 1f),
                out Vector2 solidHitPoint);
            bool hitAir = terrain.TryGetCollision(new Vector2(5f, 20f), new Vector2(5f, 20f),
                out Vector2 airHitPoint);

            Assert.IsTrue(hitSolid);
            Assert.IsFalse(hitAir);
        }
    }
}
