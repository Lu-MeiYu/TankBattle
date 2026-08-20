using System.Collections.Generic;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Data;
using TankBattle.Tests.EditMode.AI.Fakes;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.AI
{
    public class AIControllerTests
    {
        private const float Gravity = 9.8f;

        private static TankConfig CreateTankConfig(float muzzleSpeed = 40f)
        {
            var config = ScriptableObject.CreateInstance<TankConfig>();
            config.muzzleSpeedAtFullPower = muzzleSpeed;
            return config;
        }

        private static AIDifficultyConfig CreateDifficultyConfig()
        {
            return ScriptableObject.CreateInstance<AIDifficultyConfig>();
        }

        [Test]
        public void Initialize_ThrowsWhenDifficultyConfigMissing()
        {
            var go = new GameObject(nameof(Initialize_ThrowsWhenDifficultyConfigMissing));
            try
            {
                var controller = go.AddComponent<TankBattle.Gameplay.AI.AIController>();
                controller.TankConfig = CreateTankConfig();
                var ballistics = new FakeParabolicBallistics(Gravity);

                Assert.Throws<System.InvalidOperationException>(() =>
                    controller.Initialize(ballistics, ballistics, new FakeRandomSource()));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_ThrowsWhenTankConfigMissing()
        {
            var go = new GameObject(nameof(Initialize_ThrowsWhenTankConfigMissing));
            try
            {
                var controller = go.AddComponent<TankBattle.Gameplay.AI.AIController>();
                controller.DifficultyConfig = CreateDifficultyConfig();
                var ballistics = new FakeParabolicBallistics(Gravity);

                Assert.Throws<System.InvalidOperationException>(() =>
                    controller.Initialize(ballistics, ballistics, new FakeRandomSource()));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DecideTurn_ThrowsWhenNotInitialized()
        {
            var go = new GameObject(nameof(DecideTurn_ThrowsWhenNotInitialized));
            try
            {
                var controller = go.AddComponent<TankBattle.Gameplay.AI.AIController>();
                var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);

                Assert.Throws<System.InvalidOperationException>(() =>
                    controller.DecideTurn(self, new ITankState[0], WindData.Zero, Gravity, null));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_ThenDecideTurn_UsesTankConfigMuzzleSpeedAndReturnsSuccessfulAim()
        {
            var go = new GameObject(nameof(Initialize_ThenDecideTurn_UsesTankConfigMuzzleSpeedAndReturnsSuccessfulAim));
            TankConfig tankConfig = CreateTankConfig(muzzleSpeed: 45f);
            AIDifficultyConfig difficultyConfig = CreateDifficultyConfig();
            try
            {
                var controller = go.AddComponent<TankBattle.Gameplay.AI.AIController>();
                controller.TankConfig = tankConfig;
                controller.DifficultyConfig = difficultyConfig;

                var ballistics = new FakeParabolicBallistics(Gravity);
                controller.Initialize(ballistics, ballistics, new FakeRandomSource(0f));

                var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
                var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
                var terrain = new FakeFlatTerrainQuery(0f);

                var result = controller.DecideTurn(self, new ITankState[] { target }, WindData.Zero,
                    Gravity, terrain);

                Assert.AreSame(target, result.Target);
                Assert.IsTrue(result.Aim.Success);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(tankConfig);
                Object.DestroyImmediate(difficultyConfig);
            }
        }
    }
}
