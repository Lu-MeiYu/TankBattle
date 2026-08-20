using System;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// <see cref="IMapScaleCalculator"/> 的預設實作。
    /// 公式：地圖寬度 = 基礎寬度 + (總坦克數 - 1) × 單位間距（Spec 3.8 / US-11）。
    /// 數值以建構子注入，不直接依賴 <c>TankBattle.Data.BalanceConfig</c>，以利 NUnit 測試。
    /// </summary>
    public sealed class MapScaleCalculator : IMapScaleCalculator
    {
        private readonly float _baseWidth;
        private readonly float _unitSpacing;

        public MapScaleCalculator(float baseWidth, float unitSpacing)
        {
            if (baseWidth < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseWidth), "baseWidth 不可為負數");
            }

            if (unitSpacing < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unitSpacing), "unitSpacing 不可為負數");
            }

            _baseWidth = baseWidth;
            _unitSpacing = unitSpacing;
        }

        public float CalculateMapWidth(int totalTankCount)
        {
            if (totalTankCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalTankCount),
                    "totalTankCount 必須至少為 1");
            }

            return _baseWidth + (totalTankCount - 1) * _unitSpacing;
        }
    }
}
