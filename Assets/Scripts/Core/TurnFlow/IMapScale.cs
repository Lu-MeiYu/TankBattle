using System.Collections.Generic;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// 地圖寬度計算（由 Agent A4 於 Phase 1 實作）。
    /// 公式：地圖寬度 = 基礎寬度 + (總坦克數 - 1) × 單位間距（Spec 3.8 / US-11）。
    /// </summary>
    public interface IMapScaleCalculator
    {
        float CalculateMapWidth(int totalTankCount);
    }

    /// <summary>
    /// 坦克初始位置分佈（由 Agent A4 於 Phase 1 實作）。
    /// 只負責水平（X）位置，沿地圖寬度均勻分布並保證彼此間 >= 最小安全間距；
    /// 地形高度（Y 座標）由 Terrain 模組另外決定。
    /// </summary>
    public interface ITankSpawnDistributor
    {
        IReadOnlyList<float> DistributeSpawnPositions(int totalTankCount, float mapWidth,
            float minSafeSpacing);
    }
}
