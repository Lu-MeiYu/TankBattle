using System.Collections.Generic;
using TankBattle.Core.Terrain;
using UnityEngine;

namespace TankBattle.Gameplay.Map
{
    /// <summary>
    /// <see cref="MapGenerator"/> 產生一場戰鬥地圖的結果：地圖寬度、可查詢/可破壞的地形實例，
    /// 以及每輛坦克的世界座標出生點（含地表高度，供 Gameplay 層直接拿來 Instantiate 坦克）。
    /// </summary>
    public readonly struct MapGenerationResult
    {
        public readonly float MapWidth;
        public readonly HeightmapTerrain Terrain;
        public readonly IReadOnlyList<Vector2> SpawnPositions;

        public MapGenerationResult(float mapWidth, HeightmapTerrain terrain,
            IReadOnlyList<Vector2> spawnPositions)
        {
            MapWidth = mapWidth;
            Terrain = terrain;
            SpawnPositions = spawnPositions;
        }
    }
}
