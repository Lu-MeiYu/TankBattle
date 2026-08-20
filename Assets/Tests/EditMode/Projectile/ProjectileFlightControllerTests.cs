using NUnit.Framework;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Gameplay.Projectile;
using TankBattle.Tests.EditMode.AI.Fakes;
using TankBattle.Tests.EditMode.Ballistics;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Projectile
{
    [TestFixture]
    public class ProjectileFlightControllerTests
    {
        private const float Gravity = 9.8f;

        private static BallisticsSimulator CreateSimulator() => new BallisticsSimulator(Gravity);

        private static LaunchParameters CreateLaunch(float angle = 45f, float power = 60f,
            Vector2? origin = null, float muzzleSpeed = 20f) =>
            new LaunchParameters(angle, power, origin ?? Vector2.zero, muzzleSpeed);

        // ---------- Constructor ----------

        [Test]
        public void Constructor_NullSimulator_Throws()
        {
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));

            Assert.Throws<System.ArgumentNullException>(() =>
                new ProjectileFlightController(null, CreateLaunch(), WindData.Zero, terrain, 1f));
        }

        [Test]
        public void Constructor_NegativeTankHitRadius_Throws()
        {
            var simulator = CreateSimulator();
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new ProjectileFlightController(simulator, CreateLaunch(), WindData.Zero, terrain, -1f));
        }

        [Test]
        public void Constructor_InitializesCurrentStateFromSimulator()
        {
            var simulator = CreateSimulator();
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(3f, 4f);
            var launch = CreateLaunch(origin: origin);

            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 1f);

            Assert.AreEqual(origin, controller.CurrentState.Position);
            Assert.IsFalse(controller.HasEnded);
        }

        // ---------- Step: basic flight ----------

        [Test]
        public void Step_NonPositiveDeltaTime_ReturnsFalseAndDoesNotChangeState()
        {
            var simulator = CreateSimulator();
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var controller = new ProjectileFlightController(simulator, CreateLaunch(), WindData.Zero, terrain, 1f);
            Vector2 before = controller.CurrentState.Position;

            bool ended = controller.Step(0f, null);
            bool endedNegative = controller.Step(-0.1f, null);

            Assert.IsFalse(ended);
            Assert.IsFalse(endedNegative);
            Assert.AreEqual(before, controller.CurrentState.Position);
        }

        [Test]
        public void Step_WhileFlying_AdvancesPositionAndDoesNotEnd()
        {
            var simulator = CreateSimulator();
            var terrain = new FlatTerrainQuery(-1000f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var controller = new ProjectileFlightController(simulator, CreateLaunch(), WindData.Zero, terrain, 1f);
            Vector2 before = controller.CurrentState.Position;

            bool ended = controller.Step(0.05f, null);

            Assert.IsFalse(ended);
            Assert.IsFalse(controller.HasEnded);
            Assert.AreNotEqual(before, controller.CurrentState.Position);
        }

        [Test]
        public void Step_NullCandidateTanks_DoesNotThrow()
        {
            var simulator = CreateSimulator();
            var terrain = new FlatTerrainQuery(-1000f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var controller = new ProjectileFlightController(simulator, CreateLaunch(), WindData.Zero, terrain, 1f);

            Assert.DoesNotThrow(() => controller.Step(0.05f, null));
        }

        // ---------- Step: terrain / out-of-bounds ----------

        [Test]
        public void Step_HitsFlatGround_EndsWithTerrainImpact()
        {
            var simulator = CreateSimulator();
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var launch = CreateLaunch(angle: 45f, power: 40f, origin: new Vector2(0f, 5f));
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 0.01f);

            bool ended = false;
            for (int i = 0; i < 2000 && !ended; i++)
            {
                ended = controller.Step(0.01f, null);
            }

            Assert.IsTrue(ended);
            Assert.IsTrue(controller.HasEnded);
            Assert.AreEqual(ImpactType.Terrain, controller.Impact.Type);
        }

        [Test]
        public void Step_LeavesWorldBounds_EndsWithOutOfBoundsImpact()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-2f, -2f, 4f, 4f));
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: Vector2.zero, muzzleSpeed: 50f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 0.01f);

            bool ended = controller.Step(1f, null);

            Assert.IsTrue(ended);
            Assert.AreEqual(ImpactType.OutOfBounds, controller.Impact.Type);
        }

        // ---------- Step: tank collision ----------

        [Test]
        public void Step_TankWithinRadius_EndsWithTankImpact()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: origin, muzzleSpeed: 10f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 5f);

            var target = new FakeTankState(2, Faction.AI, new Vector2(1f, 0f), 100, 100);
            var candidates = new System.Collections.Generic.List<ITankState> { target };

            bool ended = controller.Step(0.1f, candidates);

            Assert.IsTrue(ended);
            Assert.AreEqual(ImpactType.Tank, controller.Impact.Type);
            Assert.AreEqual(target.Position, controller.Impact.Point);
        }

        [Test]
        public void Step_ExcludedShooterTank_IsIgnoredEvenWithinRadius()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: origin, muzzleSpeed: 10f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 5f,
                excludedTankId: 1);

            var shooter = new FakeTankState(1, Faction.Player, origin, 100, 100);
            var candidates = new System.Collections.Generic.List<ITankState> { shooter };

            bool ended = controller.Step(0.1f, candidates);

            Assert.IsFalse(ended);
        }

        [Test]
        public void Step_DeadTank_IsIgnored()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: origin, muzzleSpeed: 10f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 5f);

            var deadTank = new FakeTankState(3, Faction.AI, new Vector2(1f, 0f), 0, 100);
            var candidates = new System.Collections.Generic.List<ITankState> { deadTank };

            bool ended = controller.Step(0.1f, candidates);

            Assert.IsFalse(ended);
        }

        [Test]
        public void Step_MultipleTanksInRadius_HitsNearestOne()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: origin, muzzleSpeed: 10f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 10f);

            var farTank = new FakeTankState(10, Faction.AI, new Vector2(5f, 0f), 100, 100);
            var nearTank = new FakeTankState(11, Faction.AI, new Vector2(1f, 0f), 100, 100);
            var candidates = new System.Collections.Generic.List<ITankState> { farTank, nearTank };

            controller.Step(0.1f, candidates);

            Assert.AreEqual(ImpactType.Tank, controller.Impact.Type);
            Assert.AreEqual(nearTank.Position, controller.Impact.Point);
        }

        // ---------- Idempotency after ending ----------

        [Test]
        public void Step_AfterEnded_ReturnsTrueAndDoesNotChangeStateOrImpact()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: origin, muzzleSpeed: 10f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 5f);

            var target = new FakeTankState(2, Faction.AI, new Vector2(1f, 0f), 100, 100);
            var candidates = new System.Collections.Generic.List<ITankState> { target };

            controller.Step(0.1f, candidates);
            Vector2 positionAfterEnd = controller.CurrentState.Position;
            ImpactInfo impactAfterEnd = controller.Impact;

            bool endedAgain = controller.Step(0.1f, candidates);

            Assert.IsTrue(endedAgain);
            Assert.AreEqual(positionAfterEnd, controller.CurrentState.Position);
            Assert.AreEqual(impactAfterEnd.Type, controller.Impact.Type);
            Assert.AreEqual(impactAfterEnd.Point, controller.Impact.Point);
        }

        // ---------- Priority: tank collision checked before terrain/out-of-bounds ----------

        [Test]
        public void Step_TankAndTerrainCollisionSameStep_PrioritizesTankImpact()
        {
            var simulator = CreateSimulator(0f);
            // Ground exactly where the tank sits, so both a tank-hit and a terrain-hit are possible.
            var terrain = new FlatTerrainQuery(-0.5f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = CreateLaunch(angle: 0f, power: 100f, origin: origin, muzzleSpeed: 10f);
            var controller = new ProjectileFlightController(simulator, launch, WindData.Zero, terrain, 5f);

            var target = new FakeTankState(2, Faction.AI, new Vector2(1f, 0f), 100, 100);
            var candidates = new System.Collections.Generic.List<ITankState> { target };

            controller.Step(0.1f, candidates);

            Assert.AreEqual(ImpactType.Tank, controller.Impact.Type);
        }

        private static BallisticsSimulator CreateSimulator(float gravity) => new BallisticsSimulator(gravity);
    }
}
