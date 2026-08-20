using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Gameplay.Tank;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.Tank
{
    [TestFixture]
    public class TankAimStateTests
    {
        [Test]
        public void Constructor_DefaultValues_AreWithinLegalRange()
        {
            var aim = new TankAimState();

            Assert.AreEqual(45f, aim.AngleDegrees, 0.001f);
            Assert.AreEqual(50f, aim.PowerPercent, 0.001f);
        }

        [Test]
        public void SetAim_WithinRange_SetsExactValues()
        {
            var aim = new TankAimState();

            aim.SetAim(90f, 75f);

            Assert.AreEqual(90f, aim.AngleDegrees, 0.001f);
            Assert.AreEqual(75f, aim.PowerPercent, 0.001f);
        }

        [Test]
        public void SetAim_AngleBelowMin_ClampsToMin()
        {
            var aim = new TankAimState();

            aim.SetAim(-30f, 50f);

            Assert.AreEqual(LaunchParameters.MinAngleDegrees, aim.AngleDegrees, 0.001f);
        }

        [Test]
        public void SetAim_AngleAboveMax_ClampsToMax()
        {
            var aim = new TankAimState();

            aim.SetAim(270f, 50f);

            Assert.AreEqual(LaunchParameters.MaxAngleDegrees, aim.AngleDegrees, 0.001f);
        }

        [Test]
        public void SetAim_PowerBelowMin_ClampsToMin()
        {
            var aim = new TankAimState();

            aim.SetAim(45f, -10f);

            Assert.AreEqual(LaunchParameters.MinPowerPercent, aim.PowerPercent, 0.001f);
        }

        [Test]
        public void SetAim_PowerAboveMax_ClampsToMax()
        {
            var aim = new TankAimState();

            aim.SetAim(45f, 150f);

            Assert.AreEqual(LaunchParameters.MaxPowerPercent, aim.PowerPercent, 0.001f);
        }

        [Test]
        public void BuildLaunchParameters_UsesCurrentAimAndGivenOriginAndMuzzleSpeed()
        {
            var aim = new TankAimState();
            aim.SetAim(60f, 80f);
            var origin = new Vector2(3f, 4f);

            LaunchParameters launch = aim.BuildLaunchParameters(origin, 50f);

            Assert.AreEqual(60f, launch.AngleDegrees, 0.001f);
            Assert.AreEqual(80f, launch.PowerPercent, 0.001f);
            Assert.AreEqual(origin, launch.Origin);
            Assert.AreEqual(50f, launch.MuzzleSpeedAtFullPower, 0.001f);
        }

        [Test]
        public void BuildLaunchParameters_NegativeMuzzleSpeed_Throws()
        {
            var aim = new TankAimState();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                aim.BuildLaunchParameters(Vector2.zero, -1f));
        }
    }
}
