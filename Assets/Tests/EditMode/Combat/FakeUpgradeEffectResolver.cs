using TankBattle.Core.Economy;

namespace TankBattle.Tests.EditMode.Combat
{
    /// <summary>測試用假升級效果解析器：以固定字典模擬等級 -> 火力倍率換算。</summary>
    internal sealed class FakeUpgradeEffectResolver : IUpgradeEffectResolver
    {
        private readonly float _fixedFirepowerMultiplier;

        public FakeUpgradeEffectResolver(float fixedFirepowerMultiplier = 1f)
        {
            _fixedFirepowerMultiplier = fixedFirepowerMultiplier;
        }

        public int LastFirepowerLevelQueried { get; private set; }

        public float GetFirepowerMultiplier(int firepowerLevel)
        {
            LastFirepowerLevelQueried = firepowerLevel;
            return _fixedFirepowerMultiplier;
        }

        public float GetMoveSpeedMultiplier(int moveSpeedLevel) => 1f;
    }
}
