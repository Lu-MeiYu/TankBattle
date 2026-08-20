using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using TankBattle.Gameplay.Projectile;
using UnityEngine;
using UnityEngine.TestTools;

namespace TankBattle.Tests.PlayMode.Projectile
{
    /// <summary>
    /// PlayMode 煙霧測試：驗證 <see cref="TankBattle.Gameplay.Projectile.Projectile"/> 這層薄
    /// MonoBehaviour 外殼確實會在每幀呼叫 <see cref="ProjectileFlightController.Step"/>、同步
    /// transform 位置，並在飛行結束時廣播 <c>OnImpact</c>。詳細的物理/命中判定分支由
    /// EditMode 的 <see cref="TankBattle.Tests.EditMode.Projectile.ProjectileFlightControllerTests"/>
    /// 覆蓋，本測試只驗證 Gameplay 層的整合線路是否正確接上。
    /// </summary>
    public class ProjectilePlayModeTests
    {
        private sealed class StubTankState : ITankState
        {
            public StubTankState(int tankId, Vector2 position)
            {
                TankId = tankId;
                Position = position;
            }

            public int TankId { get; }
            public Faction Faction => Faction.AI;
            public Vector2 Position { get; }
            public int CurrentHp => 100;
            public int MaxHp => 100;
            public bool IsAlive => true;
        }

        private sealed class FlatGround : ITerrainQuery
        {
            private readonly float _groundHeight;
            private readonly Rect _bounds;

            public FlatGround(float groundHeight, Rect bounds)
            {
                _groundHeight = groundHeight;
                _bounds = bounds;
            }

            public bool IsSolidAt(Vector2 worldPoint) => worldPoint.y <= _groundHeight;
            public float GetSurfaceHeight(float x) => _groundHeight;

            public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
            {
                if (fromPoint.y > _groundHeight && toPoint.y <= _groundHeight)
                {
                    hitPoint = new Vector2(toPoint.x, _groundHeight);
                    return true;
                }

                hitPoint = default;
                return false;
            }

            public Rect GetWorldBounds() => _bounds;
        }

        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Launch_TankDirectlyAtMuzzle_RaisesOnImpactWithTankType()
        {
            _gameObject = new GameObject("TestProjectile");
            var projectile = _gameObject.AddComponent<TankBattle.Gameplay.Projectile.Projectile>();

            var simulator = new BallisticsSimulator(9.8f);
            var terrain = new FlatGround(-1000f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(0f, 0f);
            var launch = new LaunchParameters(45f, 60f, origin, 20f);
            var target = new StubTankState(99, origin);
            IReadOnlyList<ITankState> candidates = new List<ITankState> { target };

            ImpactInfo? receivedImpact = null;
            projectile.OnImpact += (_, impact) => receivedImpact = impact;

            projectile.Launch(simulator, launch, WindData.Zero, terrain, 5f, () => candidates, shooterTankId: -1);

            int safetyFrameCount = 0;
            while (receivedImpact == null && safetyFrameCount < 60)
            {
                yield return null;
                safetyFrameCount++;
            }

            Assert.IsNotNull(receivedImpact, "OnImpact 應在數幀內被觸發");
            Assert.AreEqual(ImpactType.Tank, receivedImpact.Value.Type);
        }

        [UnityTest]
        public IEnumerator Launch_SyncsTransformPositionEachFrame()
        {
            _gameObject = new GameObject("TestProjectile2");
            var projectile = _gameObject.AddComponent<TankBattle.Gameplay.Projectile.Projectile>();

            var simulator = new BallisticsSimulator(9.8f);
            var terrain = new FlatGround(-1000f, new Rect(-1000f, -1000f, 2000f, 2000f));
            var origin = new Vector2(1f, 2f);
            var launch = new LaunchParameters(45f, 60f, origin, 20f);

            projectile.Launch(simulator, launch, WindData.Zero, terrain, 0.01f,
                () => System.Array.Empty<ITankState>());

            Assert.AreEqual((Vector3)origin, _gameObject.transform.position);

            yield return null;

            Assert.AreNotEqual((Vector3)origin, _gameObject.transform.position);
        }
    }
}
