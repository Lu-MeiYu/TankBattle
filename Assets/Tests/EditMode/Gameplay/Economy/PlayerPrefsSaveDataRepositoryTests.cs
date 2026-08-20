using NUnit.Framework;
using TankBattle.Core.Economy;
using TankBattle.Gameplay.Economy;
using UnityEngine;

namespace TankBattle.Tests.EditMode.Gameplay.Economy
{
    /// <summary>
    /// 驗證 <see cref="PlayerPrefsSaveDataRepository"/> 對 PlayerPrefs 的讀寫行為。
    /// 使用真實的 Unity PlayerPrefs API，測試前後清除該 key，避免污染其他測試/使用者資料。
    /// </summary>
    public class PlayerPrefsSaveDataRepositoryTests
    {
        private const string SaveKey = "TankBattle.PlayerSaveData";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Load_WhenKeyMissing_ReturnsDefaultData()
        {
            var repository = new PlayerPrefsSaveDataRepository();

            PlayerSaveData data = repository.Load();

            Assert.AreEqual(0, data.Money);
            Assert.AreEqual(0, data.FirepowerLevel);
            Assert.AreEqual(0, data.MoveSpeedLevel);
        }

        [Test]
        public void Load_WhenJsonCorrupted_ReturnsDefaultDataInsteadOfThrowing()
        {
            PlayerPrefs.SetString(SaveKey, "{ this is not valid json");
            var repository = new PlayerPrefsSaveDataRepository();

            PlayerSaveData data = null;
            Assert.DoesNotThrow(() => data = repository.Load());
            Assert.AreEqual(0, data.Money);
        }

        [Test]
        public void SaveThenLoad_RoundTripsData()
        {
            var repository = new PlayerPrefsSaveDataRepository();
            var original = new PlayerSaveData
            {
                Money = 1234,
                FirepowerLevel = 3,
                MoveSpeedLevel = 2
            };

            repository.Save(original);
            PlayerSaveData loaded = repository.Load();

            Assert.AreEqual(original.Money, loaded.Money);
            Assert.AreEqual(original.FirepowerLevel, loaded.FirepowerLevel);
            Assert.AreEqual(original.MoveSpeedLevel, loaded.MoveSpeedLevel);
        }

        [Test]
        public void Save_ThrowsWhenDataIsNull()
        {
            var repository = new PlayerPrefsSaveDataRepository();
            Assert.Throws<System.ArgumentNullException>(() => repository.Save(null));
        }
    }
}
