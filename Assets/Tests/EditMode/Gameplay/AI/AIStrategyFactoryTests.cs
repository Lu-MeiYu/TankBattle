using NUnit.Framework;
using TankBattle.Core.AI;
using TankBattle.Data;
using TankBattle.Gameplay.AI;
using TankBattle.Tests.EditMode.AI.Fakes;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.AI
{
    public class AIStrategyFactoryTests
    {
        private static AIDifficultyConfig CreateConfig()
        {
            return ScriptableObject.CreateInstance<AIDifficultyConfig>();
        }

        [Test]
        public void Create_ThrowsWhenConfigIsNull()
        {
            var simulator = new FakeParabolicBallistics();
            Assert.Throws<System.ArgumentNullException>(() =>
                AIStrategyFactory.Create(AIDifficulty.Easy, null, simulator, simulator, 40f));
        }

        [Test]
        public void Create_Easy_ReturnsEasyStrategy()
        {
            var config = CreateConfig();
            var simulator = new FakeParabolicBallistics();

            IAIStrategy strategy = AIStrategyFactory.Create(AIDifficulty.Easy, config, simulator, simulator, 40f);

            Assert.IsInstanceOf<EasyAIStrategy>(strategy);
            Assert.AreEqual(AIDifficulty.Easy, strategy.Difficulty);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void Create_Normal_ReturnsNormalStrategy()
        {
            var config = CreateConfig();
            var simulator = new FakeParabolicBallistics();

            IAIStrategy strategy = AIStrategyFactory.Create(AIDifficulty.Normal, config, simulator, simulator, 40f);

            Assert.IsInstanceOf<NormalAIStrategy>(strategy);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void Create_Hard_ReturnsHardStrategy()
        {
            var config = CreateConfig();
            var simulator = new FakeParabolicBallistics();

            IAIStrategy strategy = AIStrategyFactory.Create(AIDifficulty.Hard, config, simulator, simulator, 40f);

            Assert.IsInstanceOf<HardAIStrategy>(strategy);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void Create_UnknownDifficulty_Throws()
        {
            var config = CreateConfig();
            var simulator = new FakeParabolicBallistics();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                AIStrategyFactory.Create((AIDifficulty)999, config, simulator, simulator, 40f));

            Object.DestroyImmediate(config);
        }
    }
}
