using System;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.AI.Fakes
{
    /// <summary>測試用的可變地形：地表高度由外部傳入的函式決定，用來驗證 AI 是否有讀取地表高度。</summary>
    internal sealed class FakeFunctionTerrainQuery : ITerrainQuery
    {
        private readonly Func<float, float> _heightFunc;
        private readonly Rect _bounds;

        public FakeFunctionTerrainQuery(Func<float, float> heightFunc, float mapWidth = 1000f)
        {
            _heightFunc = heightFunc ?? throw new ArgumentNullException(nameof(heightFunc));
            _bounds = new Rect(-mapWidth, -mapWidth, mapWidth * 2f, mapWidth * 2f);
        }

        public bool IsSolidAt(Vector2 worldPoint) => worldPoint.y <= _heightFunc(worldPoint.x);

        public float GetSurfaceHeight(float x) => _heightFunc(x);

        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            if (fromPoint.y > _heightFunc(fromPoint.x) && toPoint.y <= _heightFunc(toPoint.x))
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
