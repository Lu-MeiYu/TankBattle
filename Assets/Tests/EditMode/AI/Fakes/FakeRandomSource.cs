using System;
using System.Collections.Generic;
using TankBattle.Core.Shared;

namespace TankBattle.Tests.EditMode.AI.Fakes
{
    /// <summary>
    /// 測試用的可控亂數來源。NextFloat 固定回傳建構時指定的值（預設 0，代表「不加誤差」），
    /// NextInt 依序回傳建構時指定的整數序列（用完後重複回傳最後一個），方便驗證目標選擇邏輯。
    /// </summary>
    internal sealed class FakeRandomSource : IRandomSource
    {
        private readonly float _fixedFloatValue;
        private readonly IReadOnlyList<int> _intSequence;
        private int _intIndex;

        public FakeRandomSource(float fixedFloatValue = 0f, params int[] intSequence)
        {
            _fixedFloatValue = fixedFloatValue;
            _intSequence = intSequence != null && intSequence.Length > 0 ? intSequence : new[] { 0 };
            _intIndex = 0;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            int value = _intSequence[Math.Min(_intIndex, _intSequence.Count - 1)];
            _intIndex++;

            int maxInclusive = maxExclusive - 1;
            if (value < minInclusive)
            {
                return minInclusive;
            }

            return value > maxInclusive ? maxInclusive : value;
        }

        public float NextFloat(float minInclusive, float maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            if (_fixedFloatValue < minInclusive)
            {
                return minInclusive;
            }

            return _fixedFloatValue > maxInclusive ? maxInclusive : _fixedFloatValue;
        }
    }
}
