using System.Collections.Generic;
using NUnit.Framework;
using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Combat
{
    [TestFixture]
    public class ExplosionResolverTests
    {
        private DamageCalculator _damageCalculator;
        private FakeUpgradeEffectResolver _upgradeResolver;
        private ExplosionResolver _resolver;
        private FakeTerrainCarver _terrain;

        [SetUp]
        public void SetUp()
        {
            _damageCalculator = new DamageCalculator();
            _upgradeResolver = new FakeUpgradeEffectResolver(fixedFirepowerMultiplier: 1.5f);
            _resolver = new ExplosionResolver(_damageCalculator, _upgradeResolver);
            _terrain = new FakeTerrainCarver();
        }

        [Test]
        public void Resolve_AlwaysCarvesTerrainAtRequestCenterAndRadius()
        {
            var request = new ExplosionRequest(new Vector2(5f, 3f), 4f, 20f,
                new List<ITankState>());

            _resolver.Resolve(request, _terrain);

            Assert.AreEqual(1, _terrain.CallCount);
            Assert.AreEqual(new Vector2(5f, 3f), _terrain.LastCenter);
            Assert.AreEqual(4f, _terrain.LastRadius);
        }

        [Test]
        public void Resolve_TankWithinRadius_TakesDamageWithFirepowerMultiplierApplied()
        {
            var tank = new FakeTank(1, Faction.AI, new Vector2(0f, 0f), maxHp: 100);
            var request = new ExplosionRequest(new Vector2(0f, 0f), 10f, 40f,
                new List<ITankState> { tank }, shooterFirepowerLevel: 2);

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(2, _upgradeResolver.LastFirepowerLevelQueried);
            Assert.AreEqual(1, result.Damages.Count);
            // baseDamage 40 * firepower 1.5 at distance 0 => 60 damage.
            Assert.AreEqual(60f, result.Damages[0].DamageApplied, 0.001f);
            Assert.AreEqual(40, tank.CurrentHp);
        }

        [Test]
        public void Resolve_TankOutsideRadius_IsExcludedFromDamageResults()
        {
            var farTank = new FakeTank(1, Faction.AI, new Vector2(100f, 0f));
            var request = new ExplosionRequest(new Vector2(0f, 0f), 5f, 50f,
                new List<ITankState> { farTank });

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(0, result.Damages.Count);
            Assert.AreEqual(100, farTank.CurrentHp);
        }

        [Test]
        public void Resolve_AlreadyDeadTank_IsSkipped()
        {
            var deadTank = new FakeTank(1, Faction.AI, new Vector2(0f, 0f), maxHp: 10);
            deadTank.TakeDamage(999f);
            Assert.IsFalse(deadTank.IsAlive);

            var request = new ExplosionRequest(new Vector2(0f, 0f), 5f, 50f,
                new List<ITankState> { deadTank });

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(0, result.Damages.Count);
        }

        [Test]
        public void Resolve_LethalDamage_ReportsWasEliminatedTrue()
        {
            var tank = new FakeTank(1, Faction.AI, new Vector2(0f, 0f), maxHp: 10);
            var request = new ExplosionRequest(new Vector2(0f, 0f), 5f, 100f,
                new List<ITankState> { tank });

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(1, result.Damages.Count);
            Assert.IsTrue(result.Damages[0].WasEliminated);
            Assert.IsTrue(tank.IsEliminated);
        }

        [Test]
        public void Resolve_MultipleTanksAtDifferentDistances_AppliesDistanceFalloffPerTank()
        {
            var nearTank = new FakeTank(1, Faction.AI, new Vector2(0f, 0f));
            var farTank = new FakeTank(2, Faction.AI, new Vector2(5f, 0f));
            var request = new ExplosionRequest(new Vector2(0f, 0f), 10f, 100f,
                new List<ITankState> { nearTank, farTank });
            var resolverNoFirepowerBonus = new ExplosionResolver(_damageCalculator,
                new FakeUpgradeEffectResolver(1f));

            ExplosionResult result = resolverNoFirepowerBonus.Resolve(request, _terrain);

            Assert.AreEqual(100f, result.Damages[0].DamageApplied, 0.001f);
            Assert.AreEqual(50f, result.Damages[1].DamageApplied, 0.001f);
        }

        [Test]
        public void Resolve_EmptyTankList_ReturnsEmptyDamages()
        {
            var request = new ExplosionRequest(new Vector2(0f, 0f), 5f, 50f,
                new List<ITankState>());

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(0, result.Damages.Count);
        }

        [Test]
        public void Resolve_NullTanksInRange_DoesNotThrowAndReturnsEmptyDamages()
        {
            var request = new ExplosionRequest(new Vector2(0f, 0f), 5f, 50f, null);

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(0, result.Damages.Count);
        }

        [Test]
        public void Resolve_ReturnsTerrainChangeFromCarver()
        {
            var request = new ExplosionRequest(new Vector2(1f, 2f), 3f, 10f,
                new List<ITankState>());

            ExplosionResult result = _resolver.Resolve(request, _terrain);

            Assert.AreEqual(1, result.TerrainChange.RemovedRegionBounds.Count);
        }
    }
}
