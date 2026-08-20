using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.AI.Fakes
{
    /// <summary>測試用的簡易 <see cref="ITankState"/> 實作，所有欄位皆可自由設定。</summary>
    internal sealed class FakeTankState : ITankState
    {
        public FakeTankState(int tankId, Faction faction, Vector2 position, int currentHp, int maxHp)
        {
            TankId = tankId;
            Faction = faction;
            Position = position;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        public int TankId { get; }
        public Faction Faction { get; }
        public Vector2 Position { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public bool IsAlive => CurrentHp > 0;
    }
}
