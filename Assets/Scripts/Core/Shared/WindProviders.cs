namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 正式運行用的風力產生器，依賴 <see cref="IRandomSource"/> 於設定範圍內隨機產生風力。
    /// </summary>
    public sealed class RandomWindProvider : IWindProvider
    {
        private readonly IRandomSource _random;
        private readonly float _minStrength;
        private readonly float _maxStrength;

        public WindData CurrentWind { get; private set; }

        public RandomWindProvider(IRandomSource random, float minStrength, float maxStrength)
        {
            _random = random;
            _minStrength = minStrength;
            _maxStrength = maxStrength;
            CurrentWind = WindData.Zero;
        }

        public WindData GenerateNewWind()
        {
            float value = _random.NextFloat(_minStrength, _maxStrength);
            CurrentWind = new WindData(value);
            return CurrentWind;
        }
    }

    /// <summary>
    /// 測試/可重現情境用的固定風力產生器：每次 <see cref="GenerateNewWind"/> 都回傳同一個固定值。
    /// 對應 US-05「風力值為 0 時，彈道應等同標準拋物線」等測試情境。
    /// </summary>
    public sealed class FixedWindProvider : IWindProvider
    {
        public WindData CurrentWind { get; }

        public FixedWindProvider(WindData fixedWind)
        {
            CurrentWind = fixedWind;
        }

        public WindData GenerateNewWind() => CurrentWind;
    }
}
