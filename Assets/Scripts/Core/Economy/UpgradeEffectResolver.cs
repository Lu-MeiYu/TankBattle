using System;
using TankBattle.Data;

namespace TankBattle.Core.Economy
{
    /// <summary>
    /// 等級 -&gt; 實際效果數值的換算實作（見 Docs/SharedContracts.md §2.3）。
    /// Combat（火力倍率）、Gameplay（移動速度倍率）只消費此介面的結果，不自行重算。
    /// </summary>
    public sealed class UpgradeEffectResolver : IUpgradeEffectResolver
    {
        private readonly EconomyConfig _config;

        public UpgradeEffectResolver(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public float GetFirepowerMultiplier(int firepowerLevel)
        {
            if (firepowerLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firepowerLevel));
            }

            return _config.baseFirepowerMultiplier + firepowerLevel * _config.firepowerMultiplierPerLevel;
        }

        public float GetMoveSpeedMultiplier(int moveSpeedLevel)
        {
            if (moveSpeedLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveSpeedLevel));
            }

            return _config.baseMoveSpeedMultiplier + moveSpeedLevel * _config.moveSpeedMultiplierPerLevel;
        }
    }
}
