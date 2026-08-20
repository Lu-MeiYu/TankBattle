using System;
using TankBattle.Core.Economy;
using TankBattle.Data;

namespace TankBattle.Gameplay.Economy
{
    /// <summary>
    /// 商店的 Gameplay 層串接邏輯（Phase 2：對應 SharedContracts §5「商店串接、存讀檔」）。
    /// 純 C#、無 MonoBehaviour 依賴，方便 NUnit 覆蓋；載入存檔 -&gt; 建立 <see cref="EconomyService"/>，
    /// 並在每次升級/戰鬥結算後立即寫回存檔。Phase 3 的商店 UI 只需持有本類別的參考即可運作。
    /// </summary>
    public sealed class ShopService
    {
        private readonly ISaveDataRepository _repository;
        private readonly IEconomyService _economy;
        private readonly IUpgradeEffectResolver _upgradeEffects;

        public ShopService(EconomyConfig config, ISaveDataRepository repository)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            PlayerSaveData saved = _repository.Load() ?? new PlayerSaveData();
            _economy = new EconomyService(config, saved.Money, saved.FirepowerLevel, saved.MoveSpeedLevel);
            _upgradeEffects = new UpgradeEffectResolver(config);
        }

        public IEconomyService Economy => _economy;
        public IUpgradeEffectResolver UpgradeEffects => _upgradeEffects;

        /// <summary>金錢/等級狀態改變（升級成功或戰鬥結算）後觸發，供 UI 更新畫面。</summary>
        public event Action OnStateChanged;

        public bool TryUpgradeFirepower() => TryUpgradeAndPersist(UpgradeType.Firepower);

        public bool TryUpgradeMoveSpeed() => TryUpgradeAndPersist(UpgradeType.MoveSpeed);

        public RewardBreakdown ApplyBattleResult(BattleResult result)
        {
            RewardBreakdown breakdown = _economy.AwardMoney(result);
            Persist();
            OnStateChanged?.Invoke();
            return breakdown;
        }

        private bool TryUpgradeAndPersist(UpgradeType type)
        {
            bool success = _economy.TryUpgrade(type);
            if (success)
            {
                Persist();
                OnStateChanged?.Invoke();
            }

            return success;
        }

        private void Persist()
        {
            _repository.Save(new PlayerSaveData
            {
                Money = _economy.CurrentMoney,
                FirepowerLevel = _economy.FirepowerLevel,
                MoveSpeedLevel = _economy.MoveSpeedLevel
            });
        }
    }
}
