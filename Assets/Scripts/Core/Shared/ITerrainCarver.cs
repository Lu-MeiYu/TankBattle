using System.Collections.Generic;
using UnityEngine;

namespace TankBattle.Core.Shared
{
    /// <summary>破壞地形後，實際受影響的範圍回報，供 Gameplay 渲染層只重繪受影響區段。</summary>
    public readonly struct TerrainModificationResult
    {
        public readonly IReadOnlyList<Vector2> RemovedRegionBounds;
        public readonly bool WasClampedToMapBounds;

        public TerrainModificationResult(IReadOnlyList<Vector2> removedRegionBounds,
            bool wasClampedToMapBounds)
        {
            RemovedRegionBounds = removedRegionBounds;
            WasClampedToMapBounds = wasClampedToMapBounds;
        }
    }

    /// <summary>
    /// 唯一的地形破壞入口，由 Terrain 模組（A2）實作。
    /// 邊界情況約定：超出地圖邊界的部分直接 clamp（不環繞、不拋例外）；
    /// 半徑 &lt;= 0 視為 no-op；重複挖除既有坑洞範圍應取聯集，不報錯。
    /// </summary>
    public interface ITerrainCarver
    {
        TerrainModificationResult CarveCrater(Vector2 center, float radius);
    }
}
