using System;

namespace TankBattle.Core.Economy
{
    /// <summary>玩家存檔資料，透過 JSON 序列化存入 PlayerPrefs。</summary>
    [Serializable]
    public class PlayerSaveData
    {
        public int Money;
        public int FirepowerLevel;
        public int MoveSpeedLevel;
    }

    /// <summary>
    /// 存讀檔介面。實際呼叫 PlayerPrefs + JsonUtility 的實作放在 Scripts/Gameplay
    /// （Unity 平台 API，不放 Core），NUnit 測試以 in-memory fake repository 取代。
    /// Load() 需處理「key 不存在」「JSON 反序列化失敗」兩種情境，皆回退到預設值，不得拋出例外。
    /// </summary>
    public interface ISaveDataRepository
    {
        PlayerSaveData Load();
        void Save(PlayerSaveData data);
    }
}
