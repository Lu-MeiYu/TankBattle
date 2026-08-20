using System;
using UnityEngine;

namespace TankBattle.Core.Combat
{
    /// <summary>
    /// <see cref="ITankHealth"/> 的預設實作（Agent A2，Phase 1）。純 C# class，不依賴
    /// MonoBehaviour；Gameplay 層的 Tank 內部持有一個實例並轉發呼叫（forwarding），不繼承本類別。
    /// </summary>
    public sealed class TankHealth : ITankHealth
    {
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsEliminated { get; private set; }

        public event Action<ITankHealth> OnEliminated;

        public TankHealth(int maxHp)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp 必須大於 0");
            }

            MaxHp = maxHp;
            CurrentHp = maxHp;
            IsEliminated = false;
        }

        public void TakeDamage(float rawDamage)
        {
            if (IsEliminated)
            {
                // 冪等：已淘汰後重複呼叫不應再變動狀態或重複觸發事件。
                return;
            }

            if (rawDamage <= 0f)
            {
                return;
            }

            int damage = Mathf.CeilToInt(rawDamage);
            CurrentHp = Mathf.Max(0, CurrentHp - damage);

            if (CurrentHp <= 0)
            {
                IsEliminated = true;
                OnEliminated?.Invoke(this);
            }
        }
    }
}
