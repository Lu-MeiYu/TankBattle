using NUnit.Framework;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Ballistics
{
    [TestFixture]
    public class BallisticsSimulatorTests
    {
        private const float Gravity = 9.8f;
        private const float Tolerance = 0.01f;

        private static BallisticsSimulator CreateSimulator(float gravity = Gravity) =>
            new BallisticsSimulator(gravity);

        // ---------- Constructor ----------

        [Test]
        public void Constructor_NegativeGravity_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BallisticsSimulator(-1f));
        }

        [Test]
        public void Constructor_ZeroGravity_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new BallisticsSimulator(0f));
        }

        // ---------- CreateInitialState ----------

        [Test]
        public void CreateInitialState_AngleZero_FullPower_VelocityIsPurelyHorizontal()
        {
            var simulator = CreateSimulator();
            var launch = new LaunchParameters(0f, 100f, Vector2.zero, 10f);

            TrajectoryState state = simulator.CreateInitialState(launch, WindData.Zero);

            Assert.AreEqual(10f, state.Velocity.x, Tolerance);
            Assert.AreEqual(0f, state.Velocity.y, Tolerance);
            Assert.AreEqual(Vector2.zero, state.Position);
            Assert.AreEqual(0f, state.ElapsedTime);
            Assert.IsFalse(state.HasEnded);
        }

        [Test]
        public void CreateInitialState_AngleNinety_HalfPower_VelocityIsPurelyVertical()
        {
            var simulator = CreateSimulator();
            var launch = new LaunchParameters(90f, 50f, Vector2.zero, 20f);

            TrajectoryState state = simulator.CreateInitialState(launch, WindData.Zero);

            Assert.AreEqual(0f, state.Velocity.x, Tolerance);
            Assert.AreEqual(10f, state.Velocity.y, Tolerance);
        }

        [Test]
        public void CreateInitialState_AngleOneEighty_VelocityPointsFullyLeft()
        {
            var simulator = CreateSimulator();
            var launch = new LaunchParameters(180f, 100f, Vector2.zero, 10f);

            TrajectoryState state = simulator.CreateInitialState(launch, WindData.Zero);

            Assert.AreEqual(-10f, state.Velocity.x, Tolerance);
            Assert.AreEqual(0f, state.Velocity.y, Tolerance);
        }

        [Test]
        public void CreateInitialState_OutOfRangeAngleAndPower_AreClampedBeforeUse()
        {
            var simulator = CreateSimulator();
            var launchTooLow = new LaunchParameters(-30f, -20f, Vector2.zero, 10f);
            var launchTooHigh = new LaunchParameters(270f, 150f, Vector2.zero, 10f);

            TrajectoryState clampedLow = simulator.CreateInitialState(launchTooLow, WindData.Zero);
            TrajectoryState clampedHigh = simulator.CreateInitialState(launchTooHigh, WindData.Zero);

            // angle clamped to 0 => velocity fully horizontal; power clamped to 0 => zero speed.
            Assert.AreEqual(Vector2.zero, clampedLow.Velocity);
            // angle clamped to 180 => velocity fully horizontal (negative); power clamped to 100.
            Assert.AreEqual(-10f, clampedHigh.Velocity.x, Tolerance);
        }

        [Test]
        public void CreateInitialState_UsesLaunchOriginAsStartingPosition()
        {
            var simulator = CreateSimulator();
            var origin = new Vector2(3f, 5f);
            var launch = new LaunchParameters(45f, 100f, origin, 10f);

            TrajectoryState state = simulator.CreateInitialState(launch, WindData.Zero);

            Assert.AreEqual(origin, state.Position);
        }

        // ---------- Advance ----------

        [Test]
        public void Advance_ZeroGravityZeroWind_MovesLinearly()
        {
            var simulator = CreateSimulator(0f);
            var state = new TrajectoryState(Vector2.zero, new Vector2(5f, 5f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 2f, null);

            Assert.AreEqual(new Vector2(10f, 10f), next.Position);
            Assert.AreEqual(new Vector2(5f, 5f), next.Velocity);
            Assert.AreEqual(2f, next.ElapsedTime, Tolerance);
        }

        [Test]
        public void Advance_WithGravity_CurvesDownward()
        {
            var simulator = CreateSimulator(10f);
            var state = new TrajectoryState(Vector2.zero, new Vector2(1f, 0f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 1f, null);

            // v_y = 0 + (-10)*1 = -10 ; y = 0 + 0*1 + 0.5*(-10)*1^2 = -5
            Assert.AreEqual(-10f, next.Velocity.y, Tolerance);
            Assert.AreEqual(-5f, next.Position.y, Tolerance);
            Assert.AreEqual(1f, next.Position.x, Tolerance);
        }

        [Test]
        public void Advance_WithWind_AppliesHorizontalAcceleration()
        {
            var simulator = CreateSimulator(0f);
            var state = new TrajectoryState(Vector2.zero, new Vector2(0f, 0f), 0f, false);
            var wind = new WindData(4f);

            TrajectoryState next = simulator.Advance(state, wind, 1f, null);

            // v_x = 0 + 4*1 = 4 ; x = 0 + 0*1 + 0.5*4*1^2 = 2
            Assert.AreEqual(4f, next.Velocity.x, Tolerance);
            Assert.AreEqual(2f, next.Position.x, Tolerance);
        }

        [Test]
        public void Advance_ZeroWind_IsEquivalentToNoHorizontalDrift()
        {
            var simulator = CreateSimulator(9.8f);
            var state = new TrajectoryState(Vector2.zero, new Vector2(3f, 3f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 0.5f, null);

            Assert.AreEqual(3f, next.Velocity.x, Tolerance);
            Assert.AreEqual(1.5f, next.Position.x, Tolerance);
        }

        [Test]
        public void Advance_AlreadyEnded_ReturnsSameStateUnchanged()
        {
            var simulator = CreateSimulator();
            var endedState = new TrajectoryState(new Vector2(1f, 2f), new Vector2(3f, 4f), 5f, true);

            TrajectoryState next = simulator.Advance(endedState, WindData.Zero, 1f, null);

            Assert.AreEqual(endedState.Position, next.Position);
            Assert.AreEqual(endedState.Velocity, next.Velocity);
            Assert.AreEqual(endedState.ElapsedTime, next.ElapsedTime);
            Assert.IsTrue(next.HasEnded);
        }

        [Test]
        public void Advance_NonPositiveDeltaTime_ReturnsSameStateUnchanged()
        {
            var simulator = CreateSimulator();
            var state = new TrajectoryState(Vector2.zero, new Vector2(1f, 1f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 0f, null);
            TrajectoryState nextNegative = simulator.Advance(state, WindData.Zero, -1f, null);

            Assert.AreEqual(state.Position, next.Position);
            Assert.AreEqual(state.Position, nextNegative.Position);
        }

        [Test]
        public void Advance_NoTerrain_NeverEnds()
        {
            var simulator = CreateSimulator(50f);
            var state = new TrajectoryState(new Vector2(0f, 100f), new Vector2(0f, 0f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 10f, null);

            Assert.IsFalse(next.HasEnded);
        }

        [Test]
        public void Advance_CrossesFlatGround_EndsWithInterpolatedHitPoint()
        {
            var simulator = CreateSimulator(10f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-100f, -100f, 200f, 200f));
            var state = new TrajectoryState(new Vector2(0f, 1f), new Vector2(0f, -1f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 1f, terrain);

            Assert.IsTrue(next.HasEnded);
            Assert.AreEqual(0f, next.Position.y, Tolerance);
        }

        [Test]
        public void Advance_StaysAboveGround_DoesNotEnd()
        {
            var simulator = CreateSimulator(1f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-100f, -100f, 200f, 200f));
            var state = new TrajectoryState(new Vector2(0f, 100f), new Vector2(0f, 0f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 0.1f, terrain);

            Assert.IsFalse(next.HasEnded);
        }

        [Test]
        public void Advance_ExitsWorldBounds_EndsAsOutOfBounds()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-5f, -5f, 10f, 10f));
            var state = new TrajectoryState(new Vector2(4f, 0f), new Vector2(10f, 0f), 0f, false);

            TrajectoryState next = simulator.Advance(state, WindData.Zero, 1f, terrain);

            Assert.IsTrue(next.HasEnded);
        }

        // ---------- SimulateToImpact ----------

        [Test]
        public void SimulateToImpact_InvalidSimulationStep_Throws()
        {
            var simulator = CreateSimulator();
            var launch = new LaunchParameters(45f, 100f, Vector2.zero, 10f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                simulator.SimulateToImpact(launch, WindData.Zero, terrain, 10f, 0f));
        }

        [Test]
        public void SimulateToImpact_InvalidMaxFlightTime_Throws()
        {
            var simulator = CreateSimulator();
            var launch = new LaunchParameters(45f, 100f, Vector2.zero, 10f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                simulator.SimulateToImpact(launch, WindData.Zero, terrain, 0f, 0.01f));
        }

        [Test]
        public void SimulateToImpact_ZeroWind_MatchesStandardParabolaRangeFormula()
        {
            var simulator = CreateSimulator(9.8f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var launch = new LaunchParameters(45f, 100f, new Vector2(0f, 0f), 20f);

            ImpactInfo impact = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 30f, 0.001f);

            // R = v^2 * sin(2*theta) / g ; theta=45 => sin(90)=1
            float expectedRange = 20f * 20f / 9.8f;
            Assert.AreEqual(ImpactType.Terrain, impact.Type);
            Assert.AreEqual(expectedRange, impact.Point.x, 0.1f);
        }

        [Test]
        public void SimulateToImpact_ZeroWind_MatchesStandardFlightTimeFormula()
        {
            var simulator = CreateSimulator(9.8f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var launch = new LaunchParameters(60f, 100f, new Vector2(0f, 0f), 15f);

            ImpactInfo impact = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 30f, 0.001f);

            float angleRad = 60f * Mathf.Deg2Rad;
            float expectedFlightTime = 2f * 15f * Mathf.Sin(angleRad) / 9.8f;
            Assert.AreEqual(expectedFlightTime, impact.FlightTime, 0.05f);
        }

        [Test]
        public void SimulateToImpact_DifferentWinds_ProduceDifferentLandingPoints()
        {
            var simulator = CreateSimulator(9.8f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var launch = new LaunchParameters(45f, 80f, new Vector2(0f, 0f), 20f);

            ImpactInfo noWind = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 30f, 0.001f);
            ImpactInfo positiveWind = simulator.SimulateToImpact(launch, new WindData(8f), terrain, 30f, 0.001f);
            ImpactInfo negativeWind = simulator.SimulateToImpact(launch, new WindData(-8f), terrain, 30f, 0.001f);

            Assert.Greater(positiveWind.Point.x, noWind.Point.x);
            Assert.Less(negativeWind.Point.x, noWind.Point.x);
        }

        [Test]
        public void SimulateToImpact_WindZero_EqualsNoWindStandardTrajectory()
        {
            var simulator = CreateSimulator(9.8f);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var launch = new LaunchParameters(30f, 60f, new Vector2(0f, 0f), 25f);

            ImpactInfo a = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 30f, 0.001f);
            ImpactInfo b = simulator.SimulateToImpact(launch, new WindData(0f), terrain, 30f, 0.001f);

            Assert.AreEqual(a.Point.x, b.Point.x, Tolerance);
        }

        [Test]
        public void SimulateToImpact_LeavesSmallBoundsBeforeHittingGround_ReturnsOutOfBounds()
        {
            var simulator = CreateSimulator(9.8f);
            var terrain = new FlatTerrainQuery(-1000f, new Rect(-5f, -1000f, 10f, 2000f));
            var launch = new LaunchParameters(0f, 100f, new Vector2(0f, 0f), 50f);

            ImpactInfo impact = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 30f, 0.001f);

            Assert.AreEqual(ImpactType.OutOfBounds, impact.Type);
        }

        [Test]
        public void SimulateToImpact_NeverEnds_ReturnsOutOfBoundsAtMaxFlightTime()
        {
            var simulator = CreateSimulator(0f);
            var terrain = new NeverCollideTerrainQuery(new Rect(-100000f, -100000f, 200000f, 200000f));
            var launch = new LaunchParameters(0f, 1f, new Vector2(0f, 0f), 1f);

            ImpactInfo impact = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 5f, 0.1f);

            Assert.AreEqual(ImpactType.OutOfBounds, impact.Type);
            Assert.AreEqual(5f, impact.FlightTime, 0.01f);
        }

        [Test]
        public void SimulateToImpact_NullTerrain_RunsUntilMaxFlightTimeWithoutException()
        {
            var simulator = CreateSimulator(9.8f);
            var launch = new LaunchParameters(45f, 100f, new Vector2(0f, 0f), 10f);

            ImpactInfo impact = simulator.SimulateToImpact(launch, WindData.Zero, null, 2f, 0.01f);

            Assert.AreEqual(ImpactType.OutOfBounds, impact.Type);
            Assert.AreEqual(2f, impact.FlightTime, 0.01f);
        }
    }
}
