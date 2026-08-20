using UnityEngine;

namespace TankBattle.Data
{
    /// <summary>
    /// Terrain 模組（A2）獨立設定，命名慣例 `TerrainConfig`（見 Docs/SharedContracts.md §4）。
    /// 只放地形生成/破壞相關的可調參數，供 Gameplay 層讀取後轉為
    /// <see cref="TankBattle.Core.Terrain.TerrainGenerationSettings"/> 注入 Core 邏輯，
    /// Core 本身不直接引用本 ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "TankBattle/Data/TerrainConfig")]
    public class TerrainConfig : ScriptableObject
    {
        [Header("Heightmap Resolution")]
        [Min(2)]
        public int resolution = 64;

        [Header("Generation Range")]
        public float minHeight = 2f;
        public float maxHeight = 10f;
        [Min(0f)]
        public float maxStepPerColumn = 1.5f;

        [Header("World Vertical Bounds")]
        public float worldMinY = 0f;
        public float worldMaxY = 30f;

        [Header("Explosion")]
        [Min(0f)]
        public float defaultCraterRadius = 2f;
    }
}
