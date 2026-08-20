using TankBattle.Core.Shared;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    /// <summary>測試用的可變 <see cref="ITankState"/> 假實作，供 TurnFlow 測試操控存活狀態。</summary>
    internal sealed class FakeTankState : ITankState
    {
        public int TankId { get; }
        public Faction Faction { get; }
        public UnityEngine.Vector2 Position { get; set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; }
        public bool IsAlive => CurrentHp > 0;

        public FakeTankState(int tankId, Faction faction = Faction.AI, int maxHp = 100)
        {
            TankId = tankId;
            Faction = faction;
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        public void Kill()
        {
            CurrentHp = 0;
        }
    }
}
