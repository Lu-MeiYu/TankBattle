using System;
using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.TurnFlow
{
    /// <summary>
    /// 測試用的假坦克，同時實作 <see cref="ITankState"/> 與 <see cref="ITankHealth"/>，
    /// 兩者共用同一份血量狀態——如同真正的 Tank MonoBehaviour（內部持有一個 TankHealth 並轉發）,
    /// 確保 <see cref="CurrentHp"/> 歸零時，State.IsAlive 與 Health.OnEliminated 同步反映。
    /// </summary>
    internal sealed class FakeBattleTank : ITankState, ITankHealth
    {
        public int TankId { get; }
        public Faction Faction { get; }
        public Vector2 Position { get; set; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;
        public bool IsEliminated { get; private set; }

        public event Action<ITankHealth> OnEliminated;

        public FakeBattleTank(int tankId, Faction faction = Faction.AI, int maxHp = 100)
        {
            TankId = tankId;
            Faction = faction;
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        public void TakeDamage(float rawDamage)
        {
            if (IsEliminated || rawDamage <= 0f)
            {
                return;
            }

            CurrentHp = Math.Max(0, CurrentHp - (int)Math.Ceiling(rawDamage));

            if (CurrentHp <= 0)
            {
                IsEliminated = true;
                OnEliminated?.Invoke(this);
            }
        }

        /// <summary>測試專用：直接殺死並觸發淘汰事件。</summary>
        public void Kill()
        {
            TakeDamage(CurrentHp);
        }
    }
}
