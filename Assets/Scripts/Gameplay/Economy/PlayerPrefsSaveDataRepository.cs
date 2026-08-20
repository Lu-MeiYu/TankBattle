using System;
using TankBattle.Core.Economy;
using UnityEngine;

namespace TankBattle.Gameplay.Economy
{
    /// <summary>
    /// <see cref="ISaveDataRepository"/> 的 Unity 平台實作：以 <c>PlayerPrefs</c> + <c>JsonUtility</c>
    /// 做 JSON 序列化存檔（Spec §3.7、§6）。放在 Gameplay 層而非 Core，因為只有這裡允許直接使用
    /// Unity 平台 API（見 Docs/SharedContracts.md §2.3）。NUnit 測試改用 in-memory fake repository。
    /// </summary>
    public sealed class PlayerPrefsSaveDataRepository : ISaveDataRepository
    {
        private const string SaveKey = "TankBattle.PlayerSaveData";

        /// <summary>
        /// key 不存在或 JSON 反序列化失敗時，皆回退到預設值（金錢 0、等級 0），不拋出例外。
        /// </summary>
        public PlayerSaveData Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return new PlayerSaveData();
            }

            string json = PlayerPrefs.GetString(SaveKey);
            if (string.IsNullOrEmpty(json))
            {
                return new PlayerSaveData();
            }

            try
            {
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                return data ?? new PlayerSaveData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerPrefsSaveDataRepository] 存檔 JSON 解析失敗，改用預設值：{ex.Message}");
                return new PlayerSaveData();
            }
        }

        public void Save(PlayerSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}
