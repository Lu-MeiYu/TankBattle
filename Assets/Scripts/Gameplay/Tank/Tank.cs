using System;
using TankBattle.Core.Combat;
using TankBattle.Core.Shared;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Gameplay.Tank
{
    /// <summary>
    /// 坦克的 Gameplay MonoBehaviour（Agent A2，Phase 2，對應 Spec 5.2 的 <c>Scripts/Gameplay/Tank</c>）。
    /// 同時實作 <see cref="ITankState"/>（唯讀狀態，供 TurnFlow/AI/Combat 消費）與
    /// <see cref="ITankHealth"/>（扣血/淘汰，內部持有 <see cref="TankHealth"/> 並轉發呼叫，不繼承）。
    /// 對外提供「設定瞄準角度/威力並開火」的最小 API（<see cref="SetAim"/> + <see cref="Fire"/> 觸發
    /// <see cref="OnFireRequested"/> 事件），同時供玩家輸入與 A3 未來的 AIController 使用；
    /// 實際建立砲彈由監聽 <see cref="OnFireRequested"/> 的發射協調層（Phase 2 其他 Agent 的 Gameplay 模組）負責。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Tank : MonoBehaviour, ITankState, ITankHealth
    {
        [SerializeField] private TankConfig config;
        [SerializeField] private Transform muzzleTransform;

        private TankHealth _health;
        private TankAimState _aimState;
        private TankGroundFollower _groundFollower;
        private ITerrainQuery _terrain;
        private float _moveSpeedMultiplier = 1f;
        private bool _isInitialized;

        public int TankId { get; private set; }
        public Faction Faction { get; private set; }
        public Vector2 Position => transform.position;
        public int CurrentHp => _health?.CurrentHp ?? 0;
        public int MaxHp => _health?.MaxHp ?? 0;
        public bool IsAlive => _health != null && !_health.IsEliminated;
        public bool IsEliminated => _health != null && _health.IsEliminated;

        /// <summary>發射方當前的火力等級，供發射協調層查詢後填入 ExplosionRequest.ShooterFirepowerLevel。</summary>
        public int FirepowerLevel { get; private set; }

        /// <summary>基礎爆炸傷害（未乘上火力倍率），供發射協調層建立 ExplosionRequest.BaseDamage。</summary>
        public float BaseFirepowerDamage => config != null ? config.baseFirepowerDamage : 0f;

        /// <summary>爆炸半徑，供發射協調層建立 ExplosionRequest.Radius。</summary>
        public float ExplosionRadius => config != null ? config.explosionRadius : 0f;

        /// <summary>目前有效移動速度（基準值 × Economy 移動速度倍率）。</summary>
        public float MoveSpeed => config != null ? config.baseMoveSpeed * _moveSpeedMultiplier : 0f;

        public float AngleDegrees => _aimState?.AngleDegrees ?? 0f;
        public float PowerPercent => _aimState?.PowerPercent ?? 0f;

        public event Action<ITankHealth> OnEliminated;

        /// <summary>Tank 準備開火時觸發；由發射協調層監聽後建立 <c>Projectile</c> 並呼叫 Launch。</summary>
        public event Action<Tank, LaunchParameters> OnFireRequested;

        /// <summary>
        /// 由外部（BattleCoordinator/場景初始化流程）在 Instantiate 後呼叫一次，設定本場戰鬥的初始狀態。
        /// </summary>
        /// <param name="tankId">坦克唯一 Id。</param>
        /// <param name="faction">所屬陣營。</param>
        /// <param name="terrain">地形查詢，供貼地/掉落修正使用。</param>
        /// <param name="moveSpeedMultiplier">
        /// Economy 的 <c>IUpgradeEffectResolver.GetMoveSpeedMultiplier</c> 查出後帶入的移動速度倍率，
        /// Tank 本身不反查 Economy 服務。
        /// </param>
        /// <param name="firepowerLevel">
        /// Economy 的目前火力等級，Tank 只負責保存，實際傷害倍率換算交由發射協調層透過
        /// <c>IUpgradeEffectResolver.GetFirepowerMultiplier</c> 完成。
        /// </param>
        public void Initialize(int tankId, Faction faction, ITerrainQuery terrain,
            float moveSpeedMultiplier = 1f, int firepowerLevel = 0)
        {
            if (config == null)
            {
                throw new InvalidOperationException("Tank 需要指定 TankConfig。");
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            TankId = tankId;
            Faction = faction;
            _terrain = terrain;
            _moveSpeedMultiplier = moveSpeedMultiplier;
            FirepowerLevel = firepowerLevel;

            _health = new TankHealth(config.baseMaxHp);
            _health.OnEliminated += HandleHealthEliminated;

            _aimState = new TankAimState();
            _groundFollower = new TankGroundFollower(config.fallSpeed);

            // 刻意不在此立即貼齊地表：出生點可能刻意高於地表一段淨空高度
            // （見 Gameplay/Map 的 MapGenerator.tankSpawnClearance），讓坦克在戰鬥開始後
            // 自然「落地」，與 Spec 3.6 的掉落/位置修正邏輯一致，而非瞬間傳送。
            _isInitialized = true;
        }

        public void TakeDamage(float rawDamage)
        {
            EnsureInitialized();
            _health.TakeDamage(rawDamage);
        }

        /// <summary>設定瞄準角度/威力，供玩家輸入與 AIController 共用（US-04）。</summary>
        public void SetAim(float angleDegrees, float powerPercent)
        {
            EnsureInitialized();
            _aimState.SetAim(angleDegrees, powerPercent);
        }

        /// <summary>依目前瞄準狀態觸發 <see cref="OnFireRequested"/>；已淘汰的坦克呼叫此方法為 no-op。</summary>
        public void Fire()
        {
            EnsureInitialized();

            if (_health.IsEliminated)
            {
                return;
            }

            LaunchParameters launch = _aimState.BuildLaunchParameters(GetMuzzlePosition(),
                config.muzzleSpeedAtFullPower);
            OnFireRequested?.Invoke(this, launch);
        }

        /// <summary>水平移動；已淘汰的坦克呼叫此方法為 no-op。方向會夾限在 [-1, 1]。</summary>
        public void Move(float direction, float deltaTime)
        {
            EnsureInitialized();

            if (_health.IsEliminated || deltaTime <= 0f)
            {
                return;
            }

            float clampedDirection = Mathf.Clamp(direction, -1f, 1f);
            float deltaX = clampedDirection * MoveSpeed * deltaTime;

            Vector2 current = transform.position;
            float newX = current.x + deltaX;

            Rect bounds = _terrain.GetWorldBounds();
            newX = Mathf.Clamp(newX, bounds.xMin, bounds.xMax);

            transform.position = new Vector2(newX, current.y);
        }

        /// <summary>立即貼齊目前 X 座標下的地表高度（初始化/傳送用）。</summary>
        public void SnapToGround()
        {
            EnsureInitialized();
            Vector2 current = transform.position;
            float surfaceHeight = _terrain.GetSurfaceHeight(current.x);
            transform.position = new Vector2(current.x, surfaceHeight);
        }

        private Vector2 GetMuzzlePosition()
        {
            if (muzzleTransform != null)
            {
                return muzzleTransform.position;
            }

            float radians = _aimState.AngleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            return (Vector2)transform.position + direction * config.barrelLength;
        }

        private void Update()
        {
            if (!_isInitialized || _health.IsEliminated)
            {
                return;
            }

            transform.position = _groundFollower.Resolve(transform.position, _terrain, Time.deltaTime);
        }

        private void HandleHealthEliminated(ITankHealth health)
        {
            OnEliminated?.Invoke(this);
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Tank 尚未初始化，請先呼叫 Initialize。");
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnEliminated -= HandleHealthEliminated;
            }
        }
    }
}
