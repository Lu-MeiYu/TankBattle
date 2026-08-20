using System;
using TankBattle.Core.Economy;
using TankBattle.Data;
using UnityEngine;

namespace TankBattle.Gameplay.Economy
{
    /// <summary>
    /// 掛在場景中的商店進入點（Phase 2）。Awake 時載入存檔並建立 <see cref="ShopService"/>，
    /// 實際商店 UI（Phase 3）只需參考本元件即可呼叫升級/查詢金錢等級。
    /// 本類別刻意保持精簡，重邏輯都在 <see cref="ShopService"/>（可被 NUnit 覆蓋）中。
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private EconomyConfig economyConfig;

        private ShopService _service;

        public IEconomyService Economy => EnsureInitialized().Economy;
        public IUpgradeEffectResolver UpgradeEffects => EnsureInitialized().UpgradeEffects;

        public event Action OnStateChanged
        {
            add => EnsureInitialized().OnStateChanged += value;
            remove
            {
                if (_service != null)
                {
                    _service.OnStateChanged -= value;
                }
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public bool TryUpgradeFirepower() => EnsureInitialized().TryUpgradeFirepower();

        public bool TryUpgradeMoveSpeed() => EnsureInitialized().TryUpgradeMoveSpeed();

        public RewardBreakdown ApplyBattleResult(BattleResult result) => EnsureInitialized().ApplyBattleResult(result);

        private ShopService EnsureInitialized()
        {
            if (_service != null)
            {
                return _service;
            }

            if (economyConfig == null)
            {
                throw new InvalidOperationException("ShopController 需要在 Inspector 指定 EconomyConfig。");
            }

            _service = new ShopService(economyConfig, new PlayerPrefsSaveDataRepository());
            return _service;
        }
    }
}
