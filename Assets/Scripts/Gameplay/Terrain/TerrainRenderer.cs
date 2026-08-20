using System;
using TankBattle.Core.Terrain;
using UnityEngine;

namespace TankBattle.Gameplay.Terrain
{
    /// <summary>
    /// 地形視覺渲染（Agent A2，Phase 2：地形渲染）。以 <see cref="MeshFilter"/>/<see cref="MeshRenderer"/>
    /// 依 <see cref="HeightmapTerrain"/> 目前的高度資料建立網格；破壞地形（<c>CarveCrater</c>）後，
    /// 由呼叫端（發射協調層，收到 <c>ExplosionResult.TerrainChange</c> 後）呼叫 <see cref="RebuildMesh"/>
    /// 重新產生網格反映最新地形。本類別只做視覺呈現，不做任何地形查詢/破壞邏輯（一律委派給
    /// <see cref="HeightmapTerrain"/>，避免視覺層與邏輯層的地形資料不同步）。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class TerrainRenderer : MonoBehaviour
    {
        [SerializeField] private Material groundMaterial;

        private HeightmapTerrain _terrain;
        private Mesh _mesh;
        private MeshFilter _meshFilter;

        /// <summary>由外部（地圖生成流程，A4）在地圖產生後呼叫一次，綁定要渲染的地形資料。</summary>
        public void Initialize(HeightmapTerrain terrain)
        {
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));

            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh { name = "TerrainMesh" };
            _meshFilter.sharedMesh = _mesh;

            if (groundMaterial != null)
            {
                GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            }

            RebuildMesh();
        }

        /// <summary>重新取樣 <see cref="HeightmapTerrain"/> 目前的高度資料並重建網格。</summary>
        public void RebuildMesh()
        {
            if (_terrain == null)
            {
                throw new InvalidOperationException("TerrainRenderer 尚未初始化，請先呼叫 Initialize。");
            }

            Rect bounds = _terrain.GetWorldBounds();
            float[] heights = SampleHeights(bounds.width, _terrain.Resolution);

            TerrainMeshBuilder.BuildGroundMesh(heights, bounds.width, bounds.yMin,
                out Vector3[] vertices, out int[] triangles);

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            transform.position = new Vector3(bounds.xMin, 0f, transform.position.z);
        }

        private float[] SampleHeights(float mapWidth, int resolution)
        {
            var heights = new float[resolution];
            float columnWidth = mapWidth / (resolution - 1);

            for (int i = 0; i < resolution; i++)
            {
                heights[i] = _terrain.GetSurfaceHeight(i * columnWidth);
            }

            return heights;
        }
    }
}
