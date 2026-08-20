using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Combat
{
    /// <summary>
    /// 測試用假坦克：同時實作 <see cref="ITankState"/> 與 <see cref="ITankHealth"/>，
    /// 模擬 Gameplay 層 Tank MonoBehaviour 內部持有 TankHealth 並轉發呼叫的設計。
    /// </summary>
    internal sealed class FakeTank : ITankState, ITankHealth
    {
        private readonly TankHealth _health;

        public FakeTank(int tankId, Faction faction, Vector2 position, int maxHp = 100)
        {
            TankId = tankId;
            Faction = faction;
            Position = position;
            _health = new TankHealth(maxHp);
            _health.OnEliminated += _ => OnEliminated?.Invoke(this);
        }

        public int TankId { get; }
        public Faction Faction { get; }
        public Vector2 Position { get; }
        public int CurrentHp => _health.CurrentHp;
        public int MaxHp => _health.MaxHp;
        public bool IsAlive => !_health.IsEliminated;

        public bool IsEliminated => _health.IsEliminated;

        public event System.Action<ITankHealth> OnEliminated;

        public void TakeDamage(float rawDamage) => _health.TakeDamage(rawDamage);
    }
}
