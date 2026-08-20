using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    /// <summary>測試用的 <see cref="ITurnParticipant"/> 假實作，包裹 <see cref="FakeTankState"/>。</summary>
    internal sealed class FakeTurnParticipant : ITurnParticipant
    {
        public FakeTankState FakeState { get; }
        public TankBattle.Core.Shared.ITankState State => FakeState;

        public FakeTurnParticipant(FakeTankState fakeState)
        {
            FakeState = fakeState;
        }
    }
}
