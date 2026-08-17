using System;

namespace TankBattle.Core.Combat
{
    /// <summary>
    /// 坦克血量/淘汰介面（由 Agent A2 於 Phase 1 實作，例如 <c>TankHealth</c> 純 C# class）。
    /// Gameplay 層的 Tank MonoBehaviour 內部持有一個實例並轉發呼叫，不直接繼承。
    /// </summary>
    public interface ITankHealth
    {
        int MaxHp { get; }
        int CurrentHp { get; }
        bool IsEliminated { get; }

        /// <summary>
        /// 扣除傷害。內部 clamp 到 0；跨越 0 時觸發一次淘汰事件（冪等，重複呼叫不重複觸發）。
        /// </summary>
        void TakeDamage(float rawDamage);

        /// <summary>
        /// 淘汰事件。由 Turn/Match Flow 訂閱以從行動順序移除；訂閱者應只登記/移除，
        /// 不應在事件處理中反過來遍歷仍在使用中的集合。
        /// </summary>
        event Action<ITankHealth> OnEliminated;
    }
}
