using System;
using TankBattle.Core.AI;
using TankBattle.Core.Ballistics;
using TankBattle.Data;

namespace TankBattle.Gameplay.AI
{
    /// <summary>
    /// 依難度組出對應的 <see cref="IAIStrategy"/> 具體實例（Phase 2：Gameplay/AIController 的一部分）。
    /// 純 C#、無 MonoBehaviour 依賴，方便 NUnit 覆蓋；Gameplay 層的 <see cref="AIController"/> 只呼叫本工廠。
    /// </summary>
    public static class AIStrategyFactory
    {
        public static IAIStrategy Create(AIDifficulty difficulty, AIDifficultyConfig config,
            IBallisticsSimulator simulator, IBallisticsEstimator estimator, float muzzleSpeedAtFullPower,
            float maxFlightTimeSeconds = 10f, float simulationStepSeconds = 0.02f)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AIDifficultySettings settings = config.GetSettings(difficulty);

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return new EasyAIStrategy(settings, simulator, estimator, muzzleSpeedAtFullPower,
                        maxFlightTimeSeconds, simulationStepSeconds);
                case AIDifficulty.Normal:
                    return new NormalAIStrategy(settings, simulator, estimator, muzzleSpeedAtFullPower,
                        maxFlightTimeSeconds, simulationStepSeconds);
                case AIDifficulty.Hard:
                    return new HardAIStrategy(settings, simulator, estimator, muzzleSpeedAtFullPower,
                        maxFlightTimeSeconds, simulationStepSeconds);
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
        }
    }
}
