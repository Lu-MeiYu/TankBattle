using System;
using TankBattle.Core.AI;
using UnityEngine;

namespace TankBattle.Data
{
    /// <summary>
    /// 單一難度的 AI 行為參數（對應 Spec §4.1 難度分級表）。
    /// AimAngleErrorDegrees / AimPowerErrorPercent：最終瞄準結果加上的隨機誤差範圍（±值）。
    /// WindAccuracy：0~1，AI 在計算彈道時實際採用的風力比例（0 = 完全忽略風力，1 = 精確計算）。
    /// MaxSearchIterations：二分搜尋最大迭代次數，確保限時內完成（見 Spec §6 效能需求）。
    /// </summary>
    [Serializable]
    public struct AIDifficultySettings
    {
        [Min(0f)]
        public float aimAngleErrorDegrees;

        [Min(0f)]
        public float aimPowerErrorPercent;

        [Range(0f, 1f)]
        public float windAccuracy;

        [Min(1)]
        public int maxSearchIterations;
    }

    /// <summary>
    /// AI 難度設定檔（由 Agent A3 擁有，不放入 BalanceConfig，見 Docs/SharedContracts.md §2.4）。
    /// Core 邏輯（各 IAIStrategy 實作）以建構子注入 <see cref="AIDifficultySettings"/>，
    /// 不直接引用本 ScriptableObject，以利 NUnit 測試 mock。
    /// </summary>
    [CreateAssetMenu(fileName = "AIDifficultyConfig", menuName = "TankBattle/Data/AIDifficultyConfig")]
    public class AIDifficultyConfig : ScriptableObject
    {
        [Header("Easy")]
        public AIDifficultySettings easy = new AIDifficultySettings
        {
            aimAngleErrorDegrees = 15f,
            aimPowerErrorPercent = 20f,
            windAccuracy = 0.2f,
            maxSearchIterations = 6
        };

        [Header("Normal")]
        public AIDifficultySettings normal = new AIDifficultySettings
        {
            aimAngleErrorDegrees = 6f,
            aimPowerErrorPercent = 8f,
            windAccuracy = 0.7f,
            maxSearchIterations = 10
        };

        [Header("Hard")]
        public AIDifficultySettings hard = new AIDifficultySettings
        {
            aimAngleErrorDegrees = 1f,
            aimPowerErrorPercent = 2f,
            windAccuracy = 1f,
            maxSearchIterations = 16
        };

        public AIDifficultySettings GetSettings(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return easy;
                case AIDifficulty.Normal:
                    return normal;
                case AIDifficulty.Hard:
                    return hard;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
        }
    }
}
