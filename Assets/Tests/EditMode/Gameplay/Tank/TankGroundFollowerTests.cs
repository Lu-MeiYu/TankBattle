using NUnit.Framework;
using TankBattle.Gameplay.Tank;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.Tank
{
    [TestFixture]
    public class TankGroundFollowerTests
    {
        [Test]
        public void Constructor_NonPositiveFallSpeed_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TankGroundFollower(0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TankGroundFollower(-1f));
        }

        [Test]
        public void Resolve_NullTerrain_Throws()
        {
            var follower = new TankGroundFollower(10f);

            Assert.Throws<System.ArgumentNullException>(() =>
                follower.Resolve(Vector2.zero, null, 0.1f));
        }

        [Test]
        public void Resolve_AlreadyOnSurface_StaysAtSurfaceHeight()
        {
            var follower = new TankGroundFollower(10f);
            var terrain = new FakeTerrainQuery(constantHeight: 5f);

            Vector2 result = follower.Resolve(new Vector2(1f, 5f), terrain, 0.1f);

            Assert.AreEqual(5f, result.y, 0.001f);
        }

        [Test]
        public void Resolve_EmbeddedBelowSurface_SnapsUpImmediately()
        {
            var follower = new TankGroundFollower(10f);
            var terrain = new FakeTerrainQuery(constantHeight: 5f);

            Vector2 result = follower.Resolve(new Vector2(1f, 2f), terrain, 0.1f);

            Assert.AreEqual(5f, result.y, 0.001f);
        }

        [Test]
        public void Resolve_AboveSurface_FallsByFallSpeedTimesDeltaTime()
        {
            var follower = new TankGroundFollower(fallSpeed: 10f);
            var terrain = new FakeTerrainQuery(constantHeight: 5f);

            Vector2 result = follower.Resolve(new Vector2(1f, 20f), terrain, 0.5f);

            // Falls 10 * 0.5 = 5 units: 20 -> 15.
            Assert.AreEqual(15f, result.y, 0.001f);
        }

        [Test]
        public void Resolve_FallingPastSurface_ClampsExactlyToSurfaceHeight()
        {
            var follower = new TankGroundFollower(fallSpeed: 100f);
            var terrain = new FakeTerrainQuery(constantHeight: 5f);

            Vector2 result = follower.Resolve(new Vector2(1f, 6f), terrain, 1f);

            Assert.AreEqual(5f, result.y, 0.001f);
        }

        [Test]
        public void Resolve_ZeroDeltaTime_AboveSurface_DoesNotMove()
        {
            var follower = new TankGroundFollower(fallSpeed: 10f);
            var terrain = new FakeTerrainQuery(constantHeight: 5f);

            Vector2 result = follower.Resolve(new Vector2(1f, 20f), terrain, 0f);

            Assert.AreEqual(20f, result.y, 0.001f);
        }

        [Test]
        public void Resolve_XCoordinateIsPreserved()
        {
            var follower = new TankGroundFollower(fallSpeed: 10f);
            var terrain = new FakeTerrainQuery(constantHeight: 5f);

            Vector2 result = follower.Resolve(new Vector2(42f, 20f), terrain, 0.1f);

            Assert.AreEqual(42f, result.x, 0.001f);
        }

        [Test]
        public void Resolve_VaryingTerrainHeight_UsesHeightAtCurrentX()
        {
            var follower = new TankGroundFollower(fallSpeed: 10f);
            var terrain = new FakeTerrainQuery(x => x < 5f ? 2f : 8f);

            Vector2 leftResult = follower.Resolve(new Vector2(1f, 1f), terrain, 0.1f);
            Vector2 rightResult = follower.Resolve(new Vector2(10f, 1f), terrain, 0.1f);

            Assert.AreEqual(2f, leftResult.y, 0.001f);
            Assert.AreEqual(8f, rightResult.y, 0.001f);
        }
    }
}
