namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 全專案共用的亂數注入介面。任何 Core 邏輯類別禁止在內部 `new System.Random()`，
    /// 一律透過建構子/方法參數注入本介面的實例，以確保「固定種子 -> 全域可重現」。
    /// 一場戰鬥應由對戰協調層（Gameplay 的 BattleCoordinator）建立「一顆」帶種子的實例，
    /// 分派給 Wind、AI、行動順序、地形生成等模組共用，避免各模組各自建立亂數源造成不可重現。
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>回傳 [minInclusive, maxExclusive) 範圍內的整數。</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>回傳 [minInclusive, maxInclusive] 範圍內的浮點數。</summary>
        float NextFloat(float minInclusive, float maxInclusive);
    }
}
