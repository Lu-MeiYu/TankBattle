using System;
using System.Collections.Generic;
using TankBattle.Core.Shared;
using TankBattle.Core.Terrain;
using TankBattle.Core.TurnFlow;
using UnityEngine;

namespace TankBattle.Gameplay.Map
{
    /// <summary>
    /// 依 AI 數量產生本場戰鬥的地圖（Agent A4，Phase 2，對應 Spec 3.6/3.8、US-11）。
    /// 組合 Phase 1 的 <see cref="IMapScaleCalculator"/>（地圖寬度）、
    /// <see cref="ITankSpawnDistributor"/>（坦克水平分布）與 Terrain 模組（A2）的
    /// <see cref="ITerrainGenerator"/>／<see cref="HeightmapTerrain"/>（地形高度與坑洞查詢/破壞）。
    /// 本類別不依賴 MonoBehaviour，純粹是資料轉換，以利 EditMode 測試覆蓋。
    /// </summary>
    public sealed class MapGenerator
    {
        private readonly IMapScaleCalculator _mapScaleCalculator;
        private readonly ITankSpawnDistributor _spawnDistributor;
        private readonly ITerrainGenerator _terrainGenerator;

        public MapGenerator(IMapScaleCalculator mapScaleCalculator, ITankSpawnDistributor spawnDistributor,
            ITerrainGenerator terrainGenerator)
        {
            _mapScaleCalculator = mapScaleCalculator ?? throw new ArgumentNullException(nameof(mapScaleCalculator));
            _spawnDistributor = spawnDistributor ?? throw new ArgumentNullException(nameof(spawnDistributor));
            _terrainGenerator = terrainGenerator ?? throw new ArgumentNullException(nameof(terrainGenerator));
        }

        /// <summary>
        /// 產生地圖。<paramref name="tankSpawnClearance"/> 是坦克出生點相對地表的垂直淨空高度，
        /// 避免坦克一出生就卡進地形內。
        /// </summary>
        public MapGenerationResult Generate(int totalTankCount, float minSafeSpacing,
            int terrainResolution, TerrainGenerationSettings terrainSettings, float worldMinY,
            float worldMaxY, float tankSpawnClearance, IRandomSource random)
        {
            if (totalTankCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalTankCount), "totalTankCount 必須至少為 1");
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            float mapWidth = _mapScaleCalculator.CalculateMapWidth(totalTankCount);
            float[] heights = _terrainGenerator.GenerateHeights(terrainResolution, terrainSettings, random);
            var terrain = new HeightmapTerrain(mapWidth, heights, worldMinY, worldMaxY);

            IReadOnlyList<float> spawnXPositions = _spawnDistributor.DistributeSpawnPositions(
                totalTankCount, mapWidth, minSafeSpacing);

            var spawnPositions = new List<Vector2>(spawnXPositions.Count);
            foreach (float x in spawnXPositions)
            {
                float surfaceY = terrain.GetSurfaceHeight(x);
                spawnPositions.Add(new Vector2(x, surfaceY + tankSpawnClearance));
            }

            return new MapGenerationResult(mapWidth, terrain, spawnPositions);
        }
    }
}
