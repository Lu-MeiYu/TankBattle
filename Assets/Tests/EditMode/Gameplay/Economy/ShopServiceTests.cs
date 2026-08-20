using NUnit.Framework;
using TankBattle.Core.Economy;
using TankBattle.Data;
using TankBattle.Gameplay.Economy;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.Economy
{
    public class ShopServiceTests
    {
        private static EconomyConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.maxFirepowerLevel = 3;
            config.maxMoveSpeedLevel = 3;
            config.firepowerBaseCost = 100;
            config.firepowerCostGrowth = 2f;
            config.moveSpeedBaseCost = 100;
            config.moveSpeedCostGrowth = 2f;
            config.victoryBonus = 200;
            config.rankBonusPerPlace = 20;
            config.damagePerMoneyRatio = 0.5f;
            config.moneyPerKill = 30;
            return config;
        }

        [Test]
        public void Constructor_ThrowsWhenConfigIsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new ShopService(null, new FakeSaveDataRepository()));
        }

        [Test]
        public void Constructor_ThrowsWhenRepositoryIsNull()
        {
            var config = CreateConfig();
            Assert.Throws<System.ArgumentNullException>(() => new ShopService(config, null));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Constructor_LoadsExistingSaveData()
        {
            var config = CreateConfig();
            var repository = new FakeSaveDataRepository(new PlayerSaveData
            {
                Money = 500,
                FirepowerLevel = 1,
                MoveSpeedLevel = 2
            });

            var service = new ShopService(config, repository);

            Assert.AreEqual(500, service.Economy.CurrentMoney);
            Assert.AreEqual(1, service.Economy.FirepowerLevel);
            Assert.AreEqual(2, service.Economy.MoveSpeedLevel);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryUpgradeFirepower_Success_PersistsAndRaisesEvent()
        {
            var config = CreateConfig();
            var repository = new FakeSaveDataRepository(new PlayerSaveData { Money = 100 });
            var service = new ShopService(config, repository);

            bool eventRaised = false;
            service.OnStateChanged += () => eventRaised = true;

            bool result = service.TryUpgradeFirepower();

            Assert.IsTrue(result);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(1, repository.SaveCallCount);
            Assert.AreEqual(1, service.Economy.FirepowerLevel);
            Assert.AreEqual(0, service.Economy.CurrentMoney);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryUpgradeFirepower_Failure_DoesNotPersistOrRaiseEvent()
        {
            var config = CreateConfig();
            var repository = new FakeSaveDataRepository(new PlayerSaveData { Money = 0 });
            var service = new ShopService(config, repository);

            bool eventRaised = false;
            service.OnStateChanged += () => eventRaised = true;

            bool result = service.TryUpgradeFirepower();

            Assert.IsFalse(result);
            Assert.IsFalse(eventRaised);
            Assert.AreEqual(0, repository.SaveCallCount);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryUpgradeMoveSpeed_Success_PersistsAndRaisesEvent()
        {
            var config = CreateConfig();
            var repository = new FakeSaveDataRepository(new PlayerSaveData { Money = 100 });
            var service = new ShopService(config, repository);

            bool result = service.TryUpgradeMoveSpeed();

            Assert.IsTrue(result);
            Assert.AreEqual(1, repository.SaveCallCount);
            Assert.AreEqual(1, service.Economy.MoveSpeedLevel);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void ApplyBattleResult_PersistsAndReturnsBreakdown()
        {
            var config = CreateConfig();
            var repository = new FakeSaveDataRepository();
            var service = new ShopService(config, repository);

            bool eventRaised = false;
            service.OnStateChanged += () => eventRaised = true;

            var result = new BattleResult(isVictory: true, survivalRank: 1, totalTanks: 1, damageDealt: 0f,
                killCount: 0);
            RewardBreakdown breakdown = service.ApplyBattleResult(result);

            Assert.AreEqual(config.victoryBonus, breakdown.VictoryBonus);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(1, repository.SaveCallCount);
            Assert.AreEqual(service.Economy.CurrentMoney, repository.Load().Money);

            Object.DestroyImmediate(config);
        }
    }
}
