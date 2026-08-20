using TankBattle.Core.Economy;

namespace TankBattle.Tests.EditMode.Gameplay.Economy
{
    /// <summary>測試用的記憶體內存檔倉庫，取代真實的 PlayerPrefs 實作。</summary>
    internal sealed class FakeSaveDataRepository : ISaveDataRepository
    {
        private PlayerSaveData _stored;

        public FakeSaveDataRepository(PlayerSaveData initial = null)
        {
            _stored = initial;
        }

        public int SaveCallCount { get; private set; }

        public PlayerSaveData Load()
        {
            return _stored ?? new PlayerSaveData();
        }

        public void Save(PlayerSaveData data)
        {
            SaveCallCount++;
            _stored = data;
        }
    }
}
