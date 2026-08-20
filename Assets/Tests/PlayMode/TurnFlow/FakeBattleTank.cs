using System;
using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.PlayMode.TurnFlow
{
    /// <summary>
    /// PlayMode 測試專用的假坦克（與 EditMode 的
    /// TankBattle.Tests.EditMode.Gameplay.TurnFlow.FakeBattleTank 邏輯相同，
    /// 因 EditMode/PlayMode 為不同組件無法共用 internal 類別而重複定義一份最小實作）。
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

        public void Kill()
        {
            TakeDamage(CurrentHp);
        }
    }
}
