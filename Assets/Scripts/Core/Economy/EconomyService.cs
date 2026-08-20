using System;
using TankBattle.Data;

namespace TankBattle.Core.Economy
{
    /// <summary>
    /// 金錢與升級等級的實作（見 Spec §3.7、US-02/US-03/US-10）。
    /// 建構子以純數值/設定資料注入初始狀態，不直接接觸 PlayerPrefs（見 Docs/SharedContracts.md §2.3）。
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private readonly EconomyConfig _config;

        private int _money;
        private int _firepowerLevel;
        private int _moveSpeedLevel;

        public EconomyService(EconomyConfig config, int initialMoney = 0,
            int initialFirepowerLevel = 0, int initialMoveSpeedLevel = 0)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (initialMoney < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialMoney));
            }

            _money = initialMoney;
            _firepowerLevel = Clamp(initialFirepowerLevel, 0, _config.maxFirepowerLevel);
            _moveSpeedLevel = Clamp(initialMoveSpeedLevel, 0, _config.maxMoveSpeedLevel);
        }

        public int CurrentMoney => _money;
        public int FirepowerLevel => _firepowerLevel;
        public int MoveSpeedLevel => _moveSpeedLevel;

        public int GetUpgradeCost(UpgradeType type, int currentLevel)
        {
            if (currentLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel));
            }

            (int baseCost, float growth) = GetCostCurve(type);
            double cost = baseCost * Math.Pow(growth, currentLevel);
            return (int)Math.Round(cost, MidpointRounding.AwayFromZero);
        }

        public bool CanUpgrade(UpgradeType type)
        {
            int currentLevel = GetCurrentLevel(type);
            int maxLevel = GetMaxLevel(type);
            if (currentLevel >= maxLevel)
            {
                return false;
            }

            int cost = GetUpgradeCost(type, currentLevel);
            return _money >= cost;
        }

        public bool TryUpgrade(UpgradeType type)
        {
            if (!CanUpgrade(type))
            {
                return false;
            }

            int currentLevel = GetCurrentLevel(type);
            int cost = GetUpgradeCost(type, currentLevel);
            _money -= cost;
            SetCurrentLevel(type, currentLevel + 1);
            return true;
        }

        public RewardBreakdown AwardMoney(BattleResult result)
        {
            int victoryBonus = result.IsVictory ? _config.victoryBonus : 0;

            int rankBonus = 0;
            if (result.TotalTanks > 0 && result.SurvivalRank >= 1)
            {
                int placesFromLast = Math.Max(0, result.TotalTanks - result.SurvivalRank);
                rankBonus = placesFromLast * _config.rankBonusPerPlace;
            }

            int damageBonus = (int)Math.Round(Math.Max(0f, result.DamageDealt) * _config.damagePerMoneyRatio,
                MidpointRounding.AwayFromZero);
            int killBonus = Math.Max(0, result.KillCount) * _config.moneyPerKill;

            var breakdown = new RewardBreakdown(victoryBonus, rankBonus, damageBonus, killBonus);
            _money += breakdown.TotalAwarded;
            return breakdown;
        }

        private (int baseCost, float growth) GetCostCurve(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Firepower => (_config.firepowerBaseCost, _config.firepowerCostGrowth),
                UpgradeType.MoveSpeed => (_config.moveSpeedBaseCost, _config.moveSpeedCostGrowth),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private int GetCurrentLevel(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Firepower => _firepowerLevel,
                UpgradeType.MoveSpeed => _moveSpeedLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private int GetMaxLevel(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Firepower => _config.maxFirepowerLevel,
                UpgradeType.MoveSpeed => _config.maxMoveSpeedLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private void SetCurrentLevel(UpgradeType type, int level)
        {
            switch (type)
            {
                case UpgradeType.Firepower:
                    _firepowerLevel = level;
                    break;
                case UpgradeType.MoveSpeed:
                    _moveSpeedLevel = level;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
