using NUnit.Framework;
using TankBattle.Core.Economy;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Economy
{
    public class UpgradeEffectResolverTests
    {
        private static EconomyConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.baseFirepowerMultiplier = 1f;
            config.firepowerMultiplierPerLevel = 0.5f;
            config.baseMoveSpeedMultiplier = 1f;
            config.moveSpeedMultiplierPerLevel = 0.25f;
            return config;
        }

        [Test]
        public void Constructor_ThrowsWhenConfigIsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => new UpgradeEffectResolver(null));
        }

        [Test]
        public void GetFirepowerMultiplier_LevelZero_ReturnsBaseMultiplier()
        {
            var config = CreateConfig();
            var resolver = new UpgradeEffectResolver(config);

            Assert.AreEqual(1f, resolver.GetFirepowerMultiplier(0));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetFirepowerMultiplier_ScalesLinearlyWithLevel()
        {
            var config = CreateConfig();
            var resolver = new UpgradeEffectResolver(config);

            Assert.AreEqual(2.5f, resolver.GetFirepowerMultiplier(3), 0.0001f);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetFirepowerMultiplier_ThrowsWhenLevelNegative()
        {
            var config = CreateConfig();
            var resolver = new UpgradeEffectResolver(config);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => resolver.GetFirepowerMultiplier(-1));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetMoveSpeedMultiplier_ScalesLinearlyWithLevel()
        {
            var config = CreateConfig();
            var resolver = new UpgradeEffectResolver(config);

            Assert.AreEqual(1.5f, resolver.GetMoveSpeedMultiplier(2), 0.0001f);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetMoveSpeedMultiplier_ThrowsWhenLevelNegative()
        {
            var config = CreateConfig();
            var resolver = new UpgradeEffectResolver(config);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => resolver.GetMoveSpeedMultiplier(-1));

            Object.DestroyImmediate(config);
        }
    }
}
