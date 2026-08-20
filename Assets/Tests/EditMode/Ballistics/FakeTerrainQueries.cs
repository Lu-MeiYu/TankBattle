using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Ballistics
{
    /// <summary>
    /// 測試用的假地形：一條水平地面（y = groundHeight）搭配矩形世界邊界。
    /// 僅供 Ballistics 單元測試使用，不代表 A2 的實際 Terrain 實作。
    /// </summary>
    internal sealed class FlatTerrainQuery : ITerrainQuery
    {
        private readonly float _groundHeight;
        private readonly Rect _bounds;

        public FlatTerrainQuery(float groundHeight, Rect bounds)
        {
            _groundHeight = groundHeight;
            _bounds = bounds;
        }

        public bool IsSolidAt(Vector2 worldPoint) => worldPoint.y <= _groundHeight;

        public float GetSurfaceHeight(float x) => _groundHeight;

        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            bool fromAbove = fromPoint.y > _groundHeight;
            bool toAtOrBelow = toPoint.y <= _groundHeight;

            if (fromAbove && toAtOrBelow)
            {
                float denom = fromPoint.y - toPoint.y;
                float t = denom > 0f ? (fromPoint.y - _groundHeight) / denom : 0f;
                t = Mathf.Clamp01(t);
                hitPoint = Vector2.Lerp(fromPoint, toPoint, t);
                return true;
            }

            hitPoint = default;
            return false;
        }

        public Rect GetWorldBounds() => _bounds;
    }

    /// <summary>永遠不會碰撞的假地形，僅用於邊界外測試（例如驗證出界結束模擬）。</summary>
    internal sealed class NeverCollideTerrainQuery : ITerrainQuery
    {
        private readonly Rect _bounds;

        public NeverCollideTerrainQuery(Rect bounds)
        {
            _bounds = bounds;
        }

        public bool IsSolidAt(Vector2 worldPoint) => false;

        public float GetSurfaceHeight(float x) => _bounds.yMin;

        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            hitPoint = default;
            return false;
        }

        public Rect GetWorldBounds() => _bounds;
    }
}
