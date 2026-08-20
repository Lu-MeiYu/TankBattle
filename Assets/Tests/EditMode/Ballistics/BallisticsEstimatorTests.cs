using NUnit.Framework;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Ballistics
{
    [TestFixture]
    public class BallisticsEstimatorTests
    {
        private const float Gravity = 9.8f;
        private const float Tolerance = 0.5f;

        private static BallisticsEstimator CreateEstimator() => new BallisticsEstimator();

        [Test]
        public void EstimateNoWind_NonPositiveGravity_Throws()
        {
            var estimator = CreateEstimator();
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                estimator.EstimateNoWind(Vector2.zero, new Vector2(10f, 0f), 0f, 20f));
        }

        [Test]
        public void EstimateNoWind_NonPositiveMuzzleSpeed_Throws()
        {
            var estimator = CreateEstimator();
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                estimator.EstimateNoWind(Vector2.zero, new Vector2(10f, 0f), Gravity, 0f));
        }

        [Test]
        public void EstimateNoWind_TargetToTheRightOnLevelGround_PicksFortyFiveDegrees()
        {
            var estimator = CreateEstimator();

            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(40f, 0f), Gravity, 30f);

            Assert.AreEqual(45f, angle, Tolerance);
            Assert.Greater(power, 0f);
            Assert.LessOrEqual(power, 100f);
        }

        [Test]
        public void EstimateNoWind_TargetToTheLeftOnLevelGround_PicksOneThirtyFiveDegrees()
        {
            var estimator = CreateEstimator();

            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(-40f, 0f), Gravity, 30f);

            Assert.AreEqual(135f, angle, Tolerance);
        }

        [Test]
        public void EstimateNoWind_LevelGround_MatchesClassicMaxRangeSpeedFormula()
        {
            var estimator = CreateEstimator();
            float dx = 50f;
            float muzzleSpeed = 40f;

            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(dx, 0f), Gravity, muzzleSpeed);

            // 45 degree level-ground range: R = v^2/g  =>  v = sqrt(g*R)
            float expectedSpeed = Mathf.Sqrt(Gravity * dx);
            float expectedPower = expectedSpeed / muzzleSpeed * 100f;

            Assert.AreEqual(45f, angle, Tolerance);
            Assert.AreEqual(expectedPower, power, 1f);
        }

        [Test]
        public void EstimateNoWind_TargetDirectlyAbove_PicksNinetyDegreesAndVerticalSpeed()
        {
            var estimator = CreateEstimator();
            float dy = 20f;
            float muzzleSpeed = 50f;

            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(0f, dy), Gravity, muzzleSpeed);

            float expectedSpeed = Mathf.Sqrt(2f * Gravity * dy);
            float expectedPower = expectedSpeed / muzzleSpeed * 100f;

            Assert.AreEqual(90f, angle, Tolerance);
            Assert.AreEqual(expectedPower, power, 1f);
        }

        [Test]
        public void EstimateNoWind_TargetDirectlyBelowOrSameHeight_DoesNotThrowAndPicksNinetyDegrees()
        {
            var estimator = CreateEstimator();

            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(0f, -5f), Gravity, 50f);

            Assert.AreEqual(90f, angle, Tolerance);
            Assert.Greater(power, 0f);
        }

        [Test]
        public void EstimateNoWind_UnreachableHeightAtFortyFiveDegrees_FallsBackToFullPower()
        {
            var estimator = CreateEstimator();
            // Very short horizontal distance but very tall height difference: 45 degrees cannot cover it.
            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(1f, 500f), Gravity, 20f);

            Assert.AreEqual(45f, angle, Tolerance);
            Assert.AreEqual(100f, power, Tolerance);
        }

        [Test]
        public void EstimateNoWind_ResultsAreAlwaysClampedToValidRanges()
        {
            var estimator = CreateEstimator();

            (float angle, float power) = estimator.EstimateNoWind(Vector2.zero, new Vector2(1000f, 0f), Gravity, 5f);

            Assert.GreaterOrEqual(angle, LaunchParameters.MinAngleDegrees);
            Assert.LessOrEqual(angle, LaunchParameters.MaxAngleDegrees);
            Assert.GreaterOrEqual(power, LaunchParameters.MinPowerPercent);
            Assert.LessOrEqual(power, LaunchParameters.MaxPowerPercent);
        }

        // ---------- Integration with BallisticsSimulator ----------

        [Test]
        public void EstimateNoWind_FeedIntoSimulator_LandsReasonablyCloseToTargetOnLevelGround()
        {
            var estimator = CreateEstimator();
            var simulator = new BallisticsSimulator(Gravity);
            var terrain = new FlatTerrainQuery(0f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var shooter = new Vector2(0f, 0f);
            var target = new Vector2(60f, 0f);
            float muzzleSpeed = 40f;

            (float angle, float power) = estimator.EstimateNoWind(shooter, target, Gravity, muzzleSpeed);
            var launch = new LaunchParameters(angle, power, shooter, muzzleSpeed);

            ImpactInfo impact = simulator.SimulateToImpact(launch, WindData.Zero, terrain, 30f, 0.001f);

            Assert.AreEqual(target.x, impact.Point.x, 0.5f);
        }

        [Test]
        public void EstimateNoWind_FeedIntoSimulator_LandsReasonablyCloseToElevatedTarget()
        {
            var estimator = CreateEstimator();
            var simulator = new BallisticsSimulator(Gravity);
            var terrain = new FlatTerrainQuery(-1000f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var shooter = new Vector2(0f, 0f);
            var target = new Vector2(30f, 10f);
            float muzzleSpeed = 40f;

            (float angle, float power) = estimator.EstimateNoWind(shooter, target, Gravity, muzzleSpeed);
            var launch = new LaunchParameters(angle, power, shooter, muzzleSpeed);

            TrajectoryState state = simulator.CreateInitialState(launch, WindData.Zero);
            TrajectoryState closest = state;
            float closestDistance = Vector2.Distance(state.Position, target);

            for (int i = 0; i < 3000 && !state.HasEnded; i++)
            {
                state = simulator.Advance(state, WindData.Zero, 0.01f, terrain);
                float distance = Vector2.Distance(state.Position, target);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = state;
                }
            }

            Assert.Less(closestDistance, 1.5f);
        }
    }
}
