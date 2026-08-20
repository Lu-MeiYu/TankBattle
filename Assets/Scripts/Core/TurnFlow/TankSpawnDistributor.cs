using System;
using System.Collections.Generic;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// <see cref="ITankSpawnDistributor"/> 的預設實作。
    /// 將地圖寬度均分為 totalTankCount 個區段，坦克落於各區段中點，確保均勻分布且彼此間距
    /// 恆等於區段寬度；若區段寬度小於 minSafeSpacing（表示地圖規模設定與坦克數量不相容），
    /// 則丟出例外提醒呼叫端修正 <c>BalanceConfig</c> 設定，而非產生不安全的重疊位置。
    /// </summary>
    public sealed class TankSpawnDistributor : ITankSpawnDistributor
    {
        public IReadOnlyList<float> DistributeSpawnPositions(int totalTankCount, float mapWidth,
            float minSafeSpacing)
        {
            if (totalTankCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalTankCount),
                    "totalTankCount 必須至少為 1");
            }

            if (mapWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(mapWidth), "mapWidth 必須大於 0");
            }

            if (minSafeSpacing < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minSafeSpacing),
                    "minSafeSpacing 不可為負數");
            }

            float segmentWidth = mapWidth / totalTankCount;
            if (segmentWidth < minSafeSpacing)
            {
                throw new InvalidOperationException(
                    $"mapWidth={mapWidth} 無法讓 {totalTankCount} 輛坦克維持最小安全間距 " +
                    $"{minSafeSpacing}（目前每格僅 {segmentWidth}）");
            }

            var positions = new List<float>(totalTankCount);
            for (int i = 0; i < totalTankCount; i++)
            {
                positions.Add((i + 0.5f) * segmentWidth);
            }

            return positions.AsReadOnly();
        }
    }
}
