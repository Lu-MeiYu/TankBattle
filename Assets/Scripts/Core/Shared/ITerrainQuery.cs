using UnityEngine;

namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 唯讀地形查詢介面，由 Terrain 模組（A2）實作，供 Ballistics、AI、Gameplay 查詢使用。
    /// 本介面不包含任何破壞地形的方法——破壞地形一律經由 <see cref="ITerrainCarver"/>。
    /// </summary>
    public interface ITerrainQuery
    {
        /// <summary>判斷世界座標是否為實心地形。</summary>
        bool IsSolidAt(Vector2 worldPoint);

        /// <summary>給定 x 座標，回傳目前地表高度（y 值）。x 超出地圖邊界時回傳邊界處的高度。</summary>
        float GetSurfaceHeight(float x);

        /// <summary>
        /// 線段碰撞測試：判斷從 fromPoint 到 toPoint 的路徑是否穿越地形，
        /// 供逐步彈道模擬（每個模擬步）呼叫，避免砲彈穿越地形才被偵測到。
        /// </summary>
        bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint);

        /// <summary>地圖世界邊界，供彈道模擬判斷是否出界結束模擬（避免無限模擬）。</summary>
        Rect GetWorldBounds();
    }
}
