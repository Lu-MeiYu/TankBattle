using NUnit.Framework;
using TankBattle.Core.Terrain;
using TankBattle.Gameplay.Terrain;
using UnityEngine;

namespace TankBattle.Tests.PlayMode.Gameplay.Terrain
{
    [TestFixture]
    public class TerrainRendererPlayModeTests
    {
        private GameObject _gameObject;
        private TerrainRenderer _renderer;
        private HeightmapTerrain _terrain;

        [SetUp]
        public void SetUp()
        {
            var heights = new float[21];
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = 5f;
            }
            _terrain = new HeightmapTerrain(20f, heights, worldMinY: 0f, worldMaxY: 30f);

            _gameObject = new GameObject("TestTerrainRenderer");
            _renderer = _gameObject.AddComponent<TerrainRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void Initialize_BuildsMeshWithExpectedVertexCount()
        {
            _renderer.Initialize(_terrain);

            Mesh mesh = _gameObject.GetComponent<MeshFilter>().sharedMesh;

            Assert.IsNotNull(mesh);
            Assert.AreEqual(_terrain.Resolution * 2, mesh.vertexCount);
        }

        [Test]
        public void Initialize_PositionsRendererAtMapLeftEdge()
        {
            _renderer.Initialize(_terrain);

            Assert.AreEqual(0f, _gameObject.transform.position.x, 0.001f);
        }

        [Test]
        public void RebuildMesh_AfterCarveCrater_ReflectsLoweredHeights()
        {
            _renderer.Initialize(_terrain);
            Mesh before = _gameObject.GetComponent<MeshFilter>().sharedMesh;
            float beforeCenterY = before.vertices[(before.vertexCount / 4) * 2].y;

            _terrain.CarveCrater(new Vector2(10f, 5f), 3f);
            _renderer.RebuildMesh();

            Mesh after = _gameObject.GetComponent<MeshFilter>().sharedMesh;
            float afterCenterTopVertexY = after.vertices[10 * 2].y;

            Assert.Less(afterCenterTopVertexY, beforeCenterY);
        }

        [Test]
        public void RebuildMesh_BeforeInitialize_Throws()
        {
            var freshObject = new GameObject("FreshRenderer");
            var freshRenderer = freshObject.AddComponent<TerrainRenderer>();

            Assert.Throws<System.InvalidOperationException>(() => freshRenderer.RebuildMesh());

            Object.DestroyImmediate(freshObject);
        }

        [Test]
        public void Initialize_NullTerrain_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => _renderer.Initialize(null));
        }
    }
}
