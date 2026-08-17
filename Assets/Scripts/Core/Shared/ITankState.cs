using UnityEngine;

namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 所有模組（Turn Flow、AI、Combat、UI）共用的唯讀坦克狀態視圖。
    /// 實際可變的 Tank 類別（含 MonoBehaviour 綁定）在 Gameplay 層實作本介面。
    /// Core 邏輯一律只依賴這個介面，不直接依賴 Gameplay 的具體型別。
    /// </summary>
    public interface ITankState
    {
        int TankId { get; }
        Faction Faction { get; }
        Vector2 Position { get; }
        int CurrentHp { get; }
        int MaxHp { get; }
        bool IsAlive { get; }
    }
}
