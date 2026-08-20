using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.AI.Fakes
{
    /// <summary>測試用的平坦地形：地表高度固定為建構時指定的值（預設 0），不做碰撞細節判斷。</summary>
    internal sealed class FakeFlatTerrainQuery : ITerrainQuery
    {
        private readonly float _groundHeight;
        private readonly Rect _bounds;

        public FakeFlatTerrainQuery(float groundHeight = 0f, float mapWidth = 1000f)
        {
            _groundHeight = groundHeight;
            _bounds = new Rect(-mapWidth, -mapWidth, mapWidth * 2f, mapWidth * 2f);
        }

        public bool IsSolidAt(Vector2 worldPoint) => worldPoint.y <= _groundHeight;

        public float GetSurfaceHeight(float x) => _groundHeight;

        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            if (fromPoint.y > _groundHeight && toPoint.y <= _groundHeight)
            {
                hitPoint = toPoint;
                return true;
            }

            hitPoint = default;
            return false;
        }

        public Rect GetWorldBounds() => _bounds;
    }
}
