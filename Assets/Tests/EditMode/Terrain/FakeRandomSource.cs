using System;
using TankBattle.Core.Shared;

namespace TankBattle.Tests.EditMode.Terrain
{
    /// <summary>測試用假亂數源：回傳預先設定好的固定序列，用以驗證邏輯的可重現性。</summary>
    internal sealed class FakeRandomSource : IRandomSource
    {
        private readonly float[] _floatSequence;
        private int _floatIndex;

        public FakeRandomSource(params float[] floatSequence)
        {
            _floatSequence = floatSequence;
        }

        public int NextInt(int minInclusive, int maxExclusive) => minInclusive;

        public float NextFloat(float minInclusive, float maxInclusive)
        {
            if (_floatSequence == null || _floatSequence.Length == 0)
            {
                throw new InvalidOperationException("No fixed float sequence configured.");
            }

            float value = _floatSequence[_floatIndex % _floatSequence.Length];
            _floatIndex++;
            return value;
        }
    }
}
