using System.Collections.Generic;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Combat
{
    /// <summary>測試用假地形破壞器：只記錄呼叫參數並回傳固定結果，不做真正的地形運算。</summary>
    internal sealed class FakeTerrainCarver : ITerrainCarver
    {
        public Vector2? LastCenter { get; private set; }
        public float? LastRadius { get; private set; }
        public int CallCount { get; private set; }

        public TerrainModificationResult CarveCrater(Vector2 center, float radius)
        {
            LastCenter = center;
            LastRadius = radius;
            CallCount++;
            return new TerrainModificationResult(new List<Vector2> { center }, false);
        }
    }
}
