using NUnit.Framework;
using TankBattle.Core.Economy;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Economy
{
    /// <summary>
    /// 對應 Spec §3.7、§7 US-02/US-03/US-10、§8 測試策略對應表中的 Economy（金錢/升級）模組測試。
    /// </summary>
    public class EconomyServiceTests
    {
        private static EconomyConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.maxFirepowerLevel = 3;
            config.maxMoveSpeedLevel = 2;
            config.firepowerBaseCost = 100;
            config.firepowerCostGrowth = 2f;
            config.moveSpeedBaseCost = 50;
            config.moveSpeedCostGrowth = 2f;
            config.baseFirepowerMultiplier = 1f;
            config.firepowerMultiplierPerLevel = 0.5f;
            config.baseMoveSpeedMultiplier = 1f;
            config.moveSpeedMultiplierPerLevel = 0.25f;
            config.victoryBonus = 200;
            config.rankBonusPerPlace = 20;
            config.damagePerMoneyRatio = 0.5f;
            config.moneyPerKill = 30;
            return config;
        }

        [Test]
        public void Constructor_ThrowsWhenConfigIsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => new EconomyService(null));
        }

        [Test]
        public void Constructor_ThrowsWhenInitialMoneyIsNegative()
        {
            var config = CreateConfig();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new EconomyService(config, -1));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Constructor_ClampsInitialLevelsToConfiguredRange()
        {
            var config = CreateConfig();
            var service = new EconomyService(config, 0, initialFirepowerLevel: 999, initialMoveSpeedLevel: -5);

            Assert.AreEqual(config.maxFirepowerLevel, service.FirepowerLevel);
            Assert.AreEqual(0, service.MoveSpeedLevel);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetUpgradeCost_GrowsExponentiallyWithLevel()
        {
            var config = CreateConfig();
            var service = new EconomyService(config);

            Assert.AreEqual(100, service.GetUpgradeCost(UpgradeType.Firepower, 0));
            Assert.AreEqual(200, service.GetUpgradeCost(UpgradeType.Firepower, 1));
            Assert.AreEqual(400, service.GetUpgradeCost(UpgradeType.Firepower, 2));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetUpgradeCost_ThrowsWhenLevelNegative()
        {
            var config = CreateConfig();
            var service = new EconomyService(config);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                service.GetUpgradeCost(UpgradeType.Firepower, -1));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryUpgrade_SucceedsWhenAffordableAndBelowMaxLevel()
        {
            var config = CreateConfig();
            var service = new EconomyService(config, initialMoney: 100);

            bool result = service.TryUpgrade(UpgradeType.Firepower);

            Assert.IsTrue(result);
            Assert.AreEqual(1, service.FirepowerLevel);
            Assert.AreEqual(0, service.CurrentMoney);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryUpgrade_FailsWhenMoneyInsufficient_LevelAndMoneyUnchanged()
        {
            var config = CreateConfig();
            var service = new EconomyService(config, initialMoney: 50);

            bool result = service.TryUpgrade(UpgradeType.Firepower);

            Assert.IsFalse(result);
            Assert.AreEqual(0, service.FirepowerLevel);
            Assert.AreEqual(50, service.CurrentMoney);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryUpgrade_FailsWhenAlreadyAtMaxLevel()
        {
            var config = CreateConfig();
            var service = new EconomyService(config, initialMoney: 100000,
                initialFirepowerLevel: config.maxFirepowerLevel);

            bool result = service.TryUpgrade(UpgradeType.Firepower);

            Assert.IsFalse(result);
            Assert.AreEqual(config.maxFirepowerLevel, service.FirepowerLevel);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void CanUpgrade_MoveSpeed_ReflectsMoneyAndLevelCap()
        {
            var config = CreateConfig();
            var service = new EconomyService(config, initialMoney: 49);

            Assert.IsFalse(service.CanUpgrade(UpgradeType.MoveSpeed));

            var service2 = new EconomyService(config, initialMoney: 50);
            Assert.IsTrue(service2.CanUpgrade(UpgradeType.MoveSpeed));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void AwardMoney_VictoryGrantsVictoryBonus()
        {
            var config = CreateConfig();
            var service = new EconomyService(config);

            var result = new BattleResult(isVictory: true, survivalRank: 1, totalTanks: 4,
                damageDealt: 0f, killCount: 0);
            RewardBreakdown breakdown = service.AwardMoney(result);

            // 冠軍（第1名/共4輛）除了勝利獎勵，仍會依名次獲得 rankBonusPerPlace * (4-1) 的名次獎勵。
            int expectedRankBonus = config.rankBonusPerPlace * 3;
            Assert.AreEqual(config.victoryBonus, breakdown.VictoryBonus);
            Assert.AreEqual(config.victoryBonus + expectedRankBonus, breakdown.TotalAwarded);
            Assert.AreEqual(config.victoryBonus + expectedRankBonus, service.CurrentMoney);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void AwardMoney_DefeatGrantsNoVictoryBonusButStillGrantsRankAndDamageBonus()
        {
            var config = CreateConfig();
            var service = new EconomyService(config);

            // 4 輛坦克，玩家存活名次第 2（僅次於冠軍），造成 40 傷害、0 擊殺。
            var result = new BattleResult(isVictory: false, survivalRank: 2, totalTanks: 4,
                damageDealt: 40f, killCount: 0);
            RewardBreakdown breakdown = service.AwardMoney(result);

            Assert.AreEqual(0, breakdown.VictoryBonus);
            Assert.AreEqual(config.rankBonusPerPlace * (4 - 2), breakdown.RankBonus);
            Assert.AreEqual(20, breakdown.DamageBonus);
            Assert.AreEqual(0, breakdown.KillBonus);
            Assert.Greater(breakdown.TotalAwarded, 0);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void AwardMoney_KillsAndDamageAccumulateIntoTotal()
        {
            var config = CreateConfig();
            var service = new EconomyService(config);

            var result = new BattleResult(isVictory: false, survivalRank: 4, totalTanks: 4,
                damageDealt: 100f, killCount: 2);
            RewardBreakdown breakdown = service.AwardMoney(result);

            Assert.AreEqual(0, breakdown.RankBonus);
            Assert.AreEqual(50, breakdown.DamageBonus);
            Assert.AreEqual(60, breakdown.KillBonus);
            Assert.AreEqual(breakdown.VictoryBonus + breakdown.RankBonus + breakdown.DamageBonus + breakdown.KillBonus,
                breakdown.TotalAwarded);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void AwardMoney_AccumulatesAcrossMultipleBattles()
        {
            var config = CreateConfig();
            var service = new EconomyService(config);

            service.AwardMoney(new BattleResult(true, 1, 4, 0f, 0));
            int afterFirst = service.CurrentMoney;
            service.AwardMoney(new BattleResult(false, 3, 4, 10f, 1));

            Assert.Greater(service.CurrentMoney, afterFirst);

            Object.DestroyImmediate(config);
        }
    }
}
