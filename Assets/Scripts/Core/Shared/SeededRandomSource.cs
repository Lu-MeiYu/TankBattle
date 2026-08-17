using System;

namespace TankBattle.Core.Shared
{
    /// <summary>
    /// <see cref="IRandomSource"/> 的預設實作，包裝 <see cref="System.Random"/> 並可注入固定種子，
    /// 滿足「所有數值運算需可注入固定亂數種子以利測試重現」的非功能需求。
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                    "maxExclusive 必須大於 minInclusive");
            }

            return _random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat(float minInclusive, float maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive),
                    "maxInclusive 必須大於或等於 minInclusive");
            }

            double sample = _random.NextDouble();
            return (float)(minInclusive + sample * (maxInclusive - minInclusive));
        }
    }
}
