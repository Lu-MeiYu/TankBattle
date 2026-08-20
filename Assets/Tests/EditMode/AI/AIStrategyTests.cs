using System.Threading;
using NUnit.Framework;
using TankBattle.Core.AI;
using TankBattle.Core.Shared;
using TankBattle.Data;
using TankBattle.Tests.EditMode.AI.Fakes;
using UnityEngine;

namespace TankBattle.Tests.EditMode.AI
{
    /// <summary>
    /// 對應 Spec §4、§7 US-08、§8 測試策略對應表中的 AI（難度策略）模組測試。
    /// </summary>
    public class AIStrategyTests
    {
        private const float Gravity = 9.8f;
        private const float MuzzleSpeed = 40f;

        private static AIDifficultySettings NoErrorSettings(int maxIterations = 20, float windAccuracy = 1f)
        {
            return new AIDifficultySettings
            {
                aimAngleErrorDegrees = 0f,
                aimPowerErrorPercent = 0f,
                windAccuracy = windAccuracy,
                maxSearchIterations = maxIterations
            };
        }

        private static AIDifficultySettings ErrorSettings(float angleError, float powerError, int maxIterations = 20)
        {
            return new AIDifficultySettings
            {
                aimAngleErrorDegrees = angleError,
                aimPowerErrorPercent = powerError,
                windAccuracy = 1f,
                maxSearchIterations = maxIterations
            };
        }

        // ---------- SelectTarget ----------

        [Test]
        public void EasyAIStrategy_SelectTarget_ReturnsCandidateAtRandomIndex()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var candidateA = new FakeTankState(1, Faction.Player, new Vector2(1, 0), 100, 100);
            var candidateB = new FakeTankState(2, Faction.AI, new Vector2(2, 0), 50, 100);
            var strategy = new EasyAIStrategy(NoErrorSettings(), new FakeParabolicBallistics(Gravity),
                new FakeParabolicBallistics(Gravity), MuzzleSpeed);

            var random = new FakeRandomSource(0f, 1);
            ITankState selected = strategy.SelectTarget(self, new[] { candidateA, candidateB }, random);

            Assert.AreSame(candidateB, selected);
        }

        [Test]
        public void EasyAIStrategy_SelectTarget_ExcludesDeadAndSelf()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var dead = new FakeTankState(1, Faction.Player, new Vector2(1, 0), 0, 100);
            var alive = new FakeTankState(2, Faction.Player, new Vector2(2, 0), 10, 100);
            var strategy = new EasyAIStrategy(NoErrorSettings(), new FakeParabolicBallistics(Gravity),
                new FakeParabolicBallistics(Gravity), MuzzleSpeed);

            var random = new FakeRandomSource(0f, 0);
            ITankState selected = strategy.SelectTarget(self, new ITankState[] { self, dead, alive }, random);

            Assert.AreSame(alive, selected);
        }

        [Test]
        public void EasyAIStrategy_SelectTarget_ThrowsWhenNoAliveCandidates()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var strategy = new EasyAIStrategy(NoErrorSettings(), new FakeParabolicBallistics(Gravity),
                new FakeParabolicBallistics(Gravity), MuzzleSpeed);

            var random = new FakeRandomSource(0f, 0);
            Assert.Throws<System.InvalidOperationException>(() =>
                strategy.SelectTarget(self, new ITankState[0], random));
        }

        [Test]
        public void NormalAIStrategy_SelectTarget_PrefersLowestHp()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var highHp = new FakeTankState(1, Faction.Player, new Vector2(1, 0), 90, 100);
            var lowHp = new FakeTankState(2, Faction.Player, new Vector2(5, 0), 10, 100);
            var strategy = new NormalAIStrategy(NoErrorSettings(), new FakeParabolicBallistics(Gravity),
                new FakeParabolicBallistics(Gravity), MuzzleSpeed);

            ITankState selected = strategy.SelectTarget(self, new ITankState[] { highHp, lowHp },
                new FakeRandomSource());

            Assert.AreSame(lowHp, selected);
        }

        [Test]
        public void NormalAIStrategy_SelectTarget_TieBreaksByNearestDistance()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var near = new FakeTankState(1, Faction.Player, new Vector2(1, 0), 50, 100);
            var far = new FakeTankState(2, Faction.Player, new Vector2(10, 0), 50, 100);
            var strategy = new NormalAIStrategy(NoErrorSettings(), new FakeParabolicBallistics(Gravity),
                new FakeParabolicBallistics(Gravity), MuzzleSpeed);

            ITankState selected = strategy.SelectTarget(self, new ITankState[] { far, near },
                new FakeRandomSource());

            Assert.AreSame(near, selected);
        }

        [Test]
        public void HardAIStrategy_SelectTarget_PrefersHighestThreatScore()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            // 遠且血量高：威脅低
            var lowThreat = new FakeTankState(1, Faction.Player, new Vector2(50, 0), 100, 100);
            // 近且血量低：威脅高
            var highThreat = new FakeTankState(2, Faction.Player, new Vector2(2, 0), 5, 100);
            var strategy = new HardAIStrategy(NoErrorSettings(), new FakeParabolicBallistics(Gravity),
                new FakeParabolicBallistics(Gravity), MuzzleSpeed);

            ITankState selected = strategy.SelectTarget(self, new ITankState[] { lowThreat, highThreat },
                new FakeRandomSource());

            Assert.AreSame(highThreat, selected);
        }

        // ---------- DecideAim ----------

        [Test]
        public void DecideAim_NoWind_NoError_LandsCloseToTarget()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new HardAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);

            var context = new AimingContext(self, target, WindData.Zero, Gravity, terrain);
            AimResult result = strategy.DecideAim(context, new FakeRandomSource(0f), CancellationToken.None);

            Assert.IsTrue(result.Success);

            var launch = LaunchParameters.Clamp(result.AngleDegrees, result.PowerPercent, self.Position, MuzzleSpeed);
            ImpactInfo impact = ballistics.SimulateToImpact(launch, WindData.Zero, terrain, 10f, 0.01f);

            Assert.That(impact.Point.x, Is.EqualTo(target.Position.x).Within(0.5f));
        }

        [Test]
        public void DecideAim_TargetToTheLeft_LandsCloseToTarget()
        {
            var self = new FakeTankState(0, Faction.AI, new Vector2(30f, 0f), 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(5f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new NormalAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);

            var context = new AimingContext(self, target, WindData.Zero, Gravity, terrain);
            AimResult result = strategy.DecideAim(context, new FakeRandomSource(0f), CancellationToken.None);

            Assert.IsTrue(result.Success);
            Assert.That(result.AngleDegrees, Is.GreaterThan(90f));

            var launch = LaunchParameters.Clamp(result.AngleDegrees, result.PowerPercent, self.Position, MuzzleSpeed);
            ImpactInfo impact = ballistics.SimulateToImpact(launch, WindData.Zero, terrain, 10f, 0.01f);

            Assert.That(impact.Point.x, Is.EqualTo(target.Position.x).Within(0.5f));
        }

        [Test]
        public void DecideAim_WithWind_HigherWindAccuracyLandsCloser()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var wind = new WindData(6f);
            var context = new AimingContext(self, target, wind, Gravity, terrain);

            var easyStrategy = new EasyAIStrategy(NoErrorSettings(windAccuracy: 0f), ballistics, ballistics, MuzzleSpeed);
            var hardStrategy = new HardAIStrategy(NoErrorSettings(windAccuracy: 1f), ballistics, ballistics, MuzzleSpeed);

            AimResult easyResult = easyStrategy.DecideAim(context, new FakeRandomSource(0f), CancellationToken.None);
            AimResult hardResult = hardStrategy.DecideAim(context, new FakeRandomSource(0f), CancellationToken.None);

            // Easy 忽略風力（windAccuracy = 0）搜尋出的角度/威力施加在「有風」的真實世界中，
            // 誤差應大於精確納入風力（windAccuracy = 1）的 Hard 難度。
            var easyLaunch = LaunchParameters.Clamp(easyResult.AngleDegrees, easyResult.PowerPercent, self.Position, MuzzleSpeed);
            var hardLaunch = LaunchParameters.Clamp(hardResult.AngleDegrees, hardResult.PowerPercent, self.Position, MuzzleSpeed);

            ImpactInfo easyImpact = ballistics.SimulateToImpact(easyLaunch, wind, terrain, 10f, 0.01f);
            ImpactInfo hardImpact = ballistics.SimulateToImpact(hardLaunch, wind, terrain, 10f, 0.01f);

            float easyError = Mathf.Abs(easyImpact.Point.x - target.Position.x);
            float hardError = Mathf.Abs(hardImpact.Point.x - target.Position.x);

            Assert.That(hardError, Is.LessThan(easyError));
        }

        [Test]
        public void DecideAim_DifficultyErrorMagnitude_EasyIsLargestHardIsSmallest()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var context = new AimingContext(self, target, WindData.Zero, Gravity, terrain);

            // 讓亂數固定回傳範圍上界，直接放大檢視「誤差設定值」對最終角度/威力的影響。
            var maxOutRandom = new FakeRandomSource(1000f);

            var easy = new EasyAIStrategy(ErrorSettings(15f, 20f), ballistics, ballistics, MuzzleSpeed);
            var normal = new NormalAIStrategy(ErrorSettings(6f, 8f), ballistics, ballistics, MuzzleSpeed);
            var hard = new HardAIStrategy(ErrorSettings(1f, 2f), ballistics, ballistics, MuzzleSpeed);

            AimResult easyResult = easy.DecideAim(context, maxOutRandom, CancellationToken.None);
            AimResult normalResult = normal.DecideAim(context, maxOutRandom, CancellationToken.None);
            AimResult hardResult = hard.DecideAim(context, maxOutRandom, CancellationToken.None);

            Assert.That(easyResult.AngleDegrees, Is.GreaterThan(normalResult.AngleDegrees));
            Assert.That(normalResult.AngleDegrees, Is.GreaterThan(hardResult.AngleDegrees));
        }

        [Test]
        public void DecideAim_AlreadyCancelled_ReturnsFailed()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new NormalAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);
            var context = new AimingContext(self, target, WindData.Zero, Gravity, terrain);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            AimResult result = strategy.DecideAim(context, new FakeRandomSource(0f), cts.Token);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void DecideAim_ThrowsWhenRandomIsNull()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 0f), 100, 100);
            var terrain = new FakeFlatTerrainQuery(0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new NormalAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);
            var context = new AimingContext(self, target, WindData.Zero, Gravity, terrain);

            Assert.Throws<System.ArgumentNullException>(() =>
                strategy.DecideAim(context, null, CancellationToken.None));
        }

        [Test]
        public void HardAIStrategy_ConsidersTerrainHeightAtTarget()
        {
            var self = new FakeTankState(0, Faction.AI, Vector2.zero, 100, 100);
            var target = new FakeTankState(1, Faction.Player, new Vector2(30f, 5f), 100, 100);
            // 地形在 self 附近平坦（y=0），但目標附近抬升到 y=5；Hard 難度應瞄準目標所在的地表高度。
            var terrain = new FakeFunctionTerrainQuery(x => Mathf.Abs(x - target.Position.x) < 3f ? 5f : 0f);
            var ballistics = new FakeParabolicBallistics(Gravity);
            var strategy = new HardAIStrategy(NoErrorSettings(), ballistics, ballistics, MuzzleSpeed);

            var context = new AimingContext(self, target, WindData.Zero, Gravity, terrain);
            AimResult result = strategy.DecideAim(context, new FakeRandomSource(0f), CancellationToken.None);

            var launch = LaunchParameters.Clamp(result.AngleDegrees, result.PowerPercent, self.Position, MuzzleSpeed);
            ImpactInfo impact = ballistics.SimulateToImpact(launch, WindData.Zero, terrain, 10f, 0.01f);

            Assert.That(impact.Point.x, Is.EqualTo(target.Position.x).Within(0.5f));
        }

        [Test]
        public void AIDifficultyConfig_GetSettings_ReturnsSettingsForEachDifficulty()
        {
            var config = ScriptableObject.CreateInstance<AIDifficultyConfig>();

            Assert.AreEqual(config.easy.maxSearchIterations, config.GetSettings(AIDifficulty.Easy).maxSearchIterations);
            Assert.AreEqual(config.normal.maxSearchIterations, config.GetSettings(AIDifficulty.Normal).maxSearchIterations);
            Assert.AreEqual(config.hard.maxSearchIterations, config.GetSettings(AIDifficulty.Hard).maxSearchIterations);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.GetSettings((AIDifficulty)999));

            Object.DestroyImmediate(config);
        }
    }
}
