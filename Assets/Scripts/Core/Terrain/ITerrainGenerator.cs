namespace TankBattle.Core.Terrain
{
    /// <summary>
    /// 地形隨機生成參數（US-11、Spec 3.8：地圖高度與地形起伏樣式為固定範圍內的隨機生成）。
    /// 純資料結構，由 Gameplay 層自 <c>TerrainConfig</c>（ScriptableObject）轉出後注入。
    /// </summary>
    public readonly struct TerrainGenerationSettings
    {
        public readonly float MinHeight;
        public readonly float MaxHeight;
        public readonly float MaxStepPerColumn;

        public TerrainGenerationSettings(float minHeight, float maxHeight, float maxStepPerColumn)
        {
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            MaxStepPerColumn = maxStepPerColumn;
        }
    }

    /// <summary>
    /// 地形高度資料的隨機生成器介面（由 Terrain 模組 A2 實作）。
    /// 亂數一律透過 <see cref="Shared.IRandomSource"/> 注入，禁止內部自行 new System.Random。
    /// </summary>
    public interface ITerrainGenerator
    {
        /// <summary>產生長度為 resolution 的高度陣列，對應地圖從 0 到 mapWidth 的均勻取樣點。</summary>
        float[] GenerateHeights(int resolution, TerrainGenerationSettings settings,
            Shared.IRandomSource random);
    }
}
