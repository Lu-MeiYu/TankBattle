using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.Tank
{
    /// <summary>測試用假地形：地表高度依注入的函式計算（預設常數高度），不做碰撞細節判斷。</summary>
    internal sealed class FakeTerrainQuery : ITerrainQuery
    {
        private readonly System.Func<float, float> _heightFunction;
        private readonly Rect _bounds;

        public FakeTerrainQuery(float constantHeight = 5f, float mapWidth = 100f)
        {
            _heightFunction = _ => constantHeight;
            _bounds = new Rect(0f, 0f, mapWidth, 50f);
        }

        public FakeTerrainQuery(System.Func<float, float> heightFunction, float mapWidth = 100f)
        {
            _heightFunction = heightFunction;
            _bounds = new Rect(0f, 0f, mapWidth, 50f);
        }

        public bool IsSolidAt(Vector2 worldPoint) => worldPoint.y <= GetSurfaceHeight(worldPoint.x);

        public float GetSurfaceHeight(float x) => _heightFunction(x);

        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            hitPoint = toPoint;
            return IsSolidAt(toPoint);
        }

        public Rect GetWorldBounds() => _bounds;
    }
}
