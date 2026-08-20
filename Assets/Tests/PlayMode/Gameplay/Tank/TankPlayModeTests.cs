using System.Collections;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Data;
using UnityEngine;
using UnityEngine.TestTools;
using Faction = TankBattle.Core.Shared.Faction;
using TankComponent = TankBattle.Gameplay.Tank.Tank;

namespace TankBattle.Tests.PlayMode.Gameplay.Tank
{
    /// <summary>測試用假地形：地表高度固定，world bounds 足夠寬，供 PlayMode 測試使用。</summary>
    internal sealed class FlatTerrainQuery : ITerrainQuery
    {
        private readonly float _height;
        private readonly Rect _bounds;

        public FlatTerrainQuery(float height = 5f, float mapWidth = 100f)
        {
            _height = height;
            _bounds = new Rect(0f, 0f, mapWidth, 50f);
        }

        public bool IsSolidAt(Vector2 worldPoint) => worldPoint.y <= _height;
        public float GetSurfaceHeight(float x) => _height;
        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            hitPoint = toPoint;
            return false;
        }
        public Rect GetWorldBounds() => _bounds;
    }

    [TestFixture]
    public class TankPlayModeTests
    {
        private GameObject _gameObject;
        private TankComponent _tank;
        private TankConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TankConfig>();
            _config.baseMaxHp = 100;
            _config.muzzleSpeedAtFullPower = 40f;
            _config.baseFirepowerDamage = 25f;
            _config.explosionRadius = 2f;
            _config.baseMoveSpeed = 3f;
            _config.barrelLength = 1f;
            _config.fallSpeed = 10f;

            _gameObject = new GameObject("TestTank");
            _tank = _gameObject.AddComponent<TankComponent>();

            var configField = typeof(TankComponent).GetField("config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_tank, _config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsIdentity_AndDoesNotImmediatelySnapToGround()
        {
            _gameObject.transform.position = new Vector3(10f, 999f, 0f);
            var terrain = new FlatTerrainQuery(height: 7f);

            _tank.Initialize(1, Faction.Player, terrain);

            Assert.AreEqual(1, _tank.TankId);
            Assert.AreEqual(Faction.Player, _tank.Faction);
            Assert.AreEqual(100, _tank.MaxHp);
            Assert.AreEqual(100, _tank.CurrentHp);
            Assert.IsTrue(_tank.IsAlive);
            // Initialize 不應立即傳送坦克，出生點的落地由 Update 的 TankGroundFollower 逐幀處理。
            Assert.AreEqual(999f, _tank.Position.y, 0.001f);
        }

        [Test]
        public void SnapToGround_ImmediatelyMovesToSurfaceHeight()
        {
            _gameObject.transform.position = new Vector3(10f, 999f, 0f);
            var terrain = new FlatTerrainQuery(height: 7f);
            _tank.Initialize(1, Faction.Player, terrain);

            _tank.SnapToGround();

            Assert.AreEqual(7f, _tank.Position.y, 0.001f);
        }

        [Test]
        public void Initialize_MissingConfig_Throws()
        {
            var configField = typeof(TankComponent).GetField("config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_tank, null);

            Assert.Throws<System.InvalidOperationException>(() =>
                _tank.Initialize(1, Faction.Player, new FlatTerrainQuery()));
        }

        [Test]
        public void TakeDamage_ReducesHpAndEventuallyEliminates()
        {
            _tank.Initialize(1, Faction.AI, new FlatTerrainQuery());
            bool eliminatedFired = false;
            _tank.OnEliminated += _ => eliminatedFired = true;

            _tank.TakeDamage(150f);

            Assert.AreEqual(0, _tank.CurrentHp);
            Assert.IsFalse(_tank.IsAlive);
            Assert.IsTrue(eliminatedFired);
        }

        [Test]
        public void SetAim_ClampsAngleAndPower()
        {
            _tank.Initialize(1, Faction.Player, new FlatTerrainQuery());

            _tank.SetAim(-20f, 200f);

            Assert.AreEqual(0f, _tank.AngleDegrees, 0.001f);
            Assert.AreEqual(100f, _tank.PowerPercent, 0.001f);
        }

        [Test]
        public void Fire_RaisesOnFireRequestedWithCurrentAimAndMuzzleSpeed()
        {
            _gameObject.transform.position = new Vector3(5f, 7f, 0f);
            _tank.Initialize(1, Faction.Player, new FlatTerrainQuery(height: 7f));
            _tank.SetAim(60f, 80f);

            LaunchParameters? captured = null;
            _tank.OnFireRequested += (shooter, launch) => captured = launch;

            _tank.Fire();

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(60f, captured.Value.AngleDegrees, 0.001f);
            Assert.AreEqual(80f, captured.Value.PowerPercent, 0.001f);
            Assert.AreEqual(40f, captured.Value.MuzzleSpeedAtFullPower, 0.001f);
        }

        [Test]
        public void Fire_AfterEliminated_DoesNotRaiseOnFireRequested()
        {
            _tank.Initialize(1, Faction.Player, new FlatTerrainQuery());
            _tank.TakeDamage(999f);

            bool fired = false;
            _tank.OnFireRequested += (_, __) => fired = true;

            _tank.Fire();

            Assert.IsFalse(fired);
        }

        [Test]
        public void Move_ClampsWithinTerrainWorldBounds()
        {
            var terrain = new FlatTerrainQuery(height: 5f, mapWidth: 20f);
            _gameObject.transform.position = new Vector3(19f, 5f, 0f);
            _tank.Initialize(1, Faction.Player, terrain);

            _tank.Move(1f, deltaTime: 10f);

            Assert.LessOrEqual(_tank.Position.x, 20f);
        }

        [Test]
        public void Move_AfterEliminated_DoesNotMove()
        {
            var terrain = new FlatTerrainQuery(height: 5f);
            _gameObject.transform.position = new Vector3(5f, 5f, 0f);
            _tank.Initialize(1, Faction.Player, terrain);
            _tank.TakeDamage(999f);

            _tank.Move(1f, 1f);

            Assert.AreEqual(5f, _tank.Position.x, 0.001f);
        }

        [Test]
        public void UninitializedTank_MethodCalls_ThrowInvalidOperationException()
        {
            Assert.Throws<System.InvalidOperationException>(() => _tank.TakeDamage(10f));
            Assert.Throws<System.InvalidOperationException>(() => _tank.SetAim(45f, 50f));
            Assert.Throws<System.InvalidOperationException>(() => _tank.Fire());
            Assert.Throws<System.InvalidOperationException>(() => _tank.Move(1f, 0.1f));
        }

        [UnityTest]
        public IEnumerator Update_WhenAboveGround_GraduallyFalls()
        {
            var terrain = new FlatTerrainQuery(height: 0f);
            _gameObject.transform.position = new Vector3(1f, 50f, 0f);
            _tank.Initialize(1, Faction.Player, terrain);

            float initialY = _tank.Position.y;
            yield return null;

            Assert.Less(_tank.Position.y, initialY);
        }
    }
}
