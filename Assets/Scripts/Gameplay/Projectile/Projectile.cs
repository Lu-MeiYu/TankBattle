using System;
using System.Collections.Generic;
using TankBattle.Core.Ballistics;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Gameplay.Projectile
{
    /// <summary>
    /// 砲彈的 MonoBehaviour 外殼（Phase 2，Agent A1）。刻意保持「薄」：所有物理/命中判定邏輯
    /// 都委派給 <see cref="ProjectileFlightController"/>（純 C#，可在 EditMode 完整測試），
    /// 本類別只負責每幀呼叫 <c>Step</c>、同步 <see cref="Transform.position"/>，
    /// 以及在飛行結束時對外廣播 <see cref="OnImpact"/>。
    /// 由呼叫端（Gameplay 的 BattleCoordinator/TurnManager，Phase 2 由 Agent A4 負責）
    /// 於 Instantiate 後呼叫 <see cref="Launch"/> 開始飛行。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        /// <summary>飛行結束時觸發，攜帶命中資訊供呼叫端呼叫 <c>IExplosionResolver.Resolve</c>。</summary>
        public event Action<Projectile, ImpactInfo> OnImpact;

        private ProjectileFlightController _flightController;
        private Func<IReadOnlyList<ITankState>> _candidateTanksProvider;
        private bool _isFlying;

        /// <summary>目前飛行狀態，尚未呼叫 <see cref="Launch"/> 前為 null。</summary>
        public ProjectileFlightController FlightController => _flightController;

        /// <summary>
        /// 開始一次發射。呼叫後每幀 <see cref="Update"/> 會自動推進彈道並同步位置，
        /// 飛行結束時觸發一次 <see cref="OnImpact"/>（之後不再推進，元件仍存留供呼叫端決定是否銷毀）。
        /// </summary>
        /// <param name="simulator">正向彈道模擬器。</param>
        /// <param name="launch">發射參數。</param>
        /// <param name="wind">本次發射固定的風力。</param>
        /// <param name="terrain">地形查詢。</param>
        /// <param name="tankHitRadius">坦克命中判定半徑。</param>
        /// <param name="candidateTanksProvider">
        /// 每幀取得候選坦克清單的委派（延遲查詢，避免持有可能過期的集合快照）。
        /// </param>
        /// <param name="shooterTankId">發射者坦克 Id，飛行過程中排除自身，避免剛發射就命中自己。</param>
        public void Launch(IBallisticsSimulator simulator, LaunchParameters launch, WindData wind,
            ITerrainQuery terrain, float tankHitRadius,
            Func<IReadOnlyList<ITankState>> candidateTanksProvider, int? shooterTankId = null)
        {
            _flightController = new ProjectileFlightController(simulator, launch, wind, terrain,
                tankHitRadius, shooterTankId);
            _candidateTanksProvider = candidateTanksProvider;
            _isFlying = true;
            transform.position = launch.Origin;
        }

        private void Update()
        {
            if (!_isFlying || _flightController == null)
            {
                return;
            }

            IReadOnlyList<ITankState> candidates = _candidateTanksProvider?.Invoke();
            bool ended = _flightController.Step(Time.deltaTime, candidates);
            transform.position = _flightController.CurrentState.Position;

            if (ended)
            {
                _isFlying = false;
                OnImpact?.Invoke(this, _flightController.Impact);
            }
        }
    }
}
