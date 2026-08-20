using NUnit.Framework;
using TankBattle.Gameplay.Terrain;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.Terrain
{
    [TestFixture]
    public class TerrainMeshBuilderTests
    {
        [Test]
        public void BuildGroundMesh_ProducesTwoVerticesPerColumn()
        {
            var heights = new float[] { 1f, 2f, 3f, 4f };

            TerrainMeshBuilder.BuildGroundMesh(heights, mapWidth: 30f, worldMinY: 0f,
                out Vector3[] vertices, out int[] triangles);

            Assert.AreEqual(8, vertices.Length);
        }

        [Test]
        public void BuildGroundMesh_ProducesSixTrianglesIndicesPerColumnGap()
        {
            var heights = new float[] { 1f, 2f, 3f, 4f };

            TerrainMeshBuilder.BuildGroundMesh(heights, mapWidth: 30f, worldMinY: 0f,
                out Vector3[] vertices, out int[] triangles);

            // 4 columns => 3 gaps => 3 * 6 = 18 triangle indices.
            Assert.AreEqual(18, triangles.Length);
        }

        [Test]
        public void BuildGroundMesh_SurfaceVerticesMatchHeightsAtEvenlySpacedX()
        {
            var heights = new float[] { 0f, 10f };

            TerrainMeshBuilder.BuildGroundMesh(heights, mapWidth: 10f, worldMinY: -5f,
                out Vector3[] vertices, out int[] triangles);

            // Column width = 10 / (2-1) = 10.
            Assert.AreEqual(new Vector3(0f, 0f, 0f), vertices[0]);
            Assert.AreEqual(new Vector3(0f, -5f, 0f), vertices[1]);
            Assert.AreEqual(new Vector3(10f, 10f, 0f), vertices[2]);
            Assert.AreEqual(new Vector3(10f, -5f, 0f), vertices[3]);
        }

        [Test]
        public void BuildGroundMesh_BottomVerticesAllUseWorldMinY()
        {
            var heights = new float[] { 3f, 7f, 2f };

            TerrainMeshBuilder.BuildGroundMesh(heights, mapWidth: 20f, worldMinY: -1f,
                out Vector3[] vertices, out int[] triangles);

            Assert.AreEqual(-1f, vertices[1].y, 0.001f);
            Assert.AreEqual(-1f, vertices[3].y, 0.001f);
            Assert.AreEqual(-1f, vertices[5].y, 0.001f);
        }

        [Test]
        public void BuildGroundMesh_TrianglesReferenceValidVertexIndices()
        {
            var heights = new float[] { 1f, 2f, 3f };

            TerrainMeshBuilder.BuildGroundMesh(heights, mapWidth: 20f, worldMinY: 0f,
                out Vector3[] vertices, out int[] triangles);

            foreach (int index in triangles)
            {
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, vertices.Length);
            }
        }

        [Test]
        public void BuildGroundMesh_TooFewHeightSamples_Throws()
        {
            Assert.Throws<System.ArgumentException>(() =>
                TerrainMeshBuilder.BuildGroundMesh(new float[] { 1f }, 10f, 0f, out _, out _));
        }

        [Test]
        public void BuildGroundMesh_NullHeights_Throws()
        {
            Assert.Throws<System.ArgumentException>(() =>
                TerrainMeshBuilder.BuildGroundMesh(null, 10f, 0f, out _, out _));
        }

        [Test]
        public void BuildGroundMesh_NonPositiveMapWidth_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                TerrainMeshBuilder.BuildGroundMesh(new float[] { 1f, 2f }, 0f, 0f, out _, out _));
        }
    }
}
