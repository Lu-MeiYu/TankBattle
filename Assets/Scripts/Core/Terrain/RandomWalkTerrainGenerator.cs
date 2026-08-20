using System;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.Terrain
{
    /// <summary>
    /// <see cref="ITerrainGenerator"/> 的預設實作：隨機漫步（random walk）產生高度陣列，
    /// 每一欄高度 = 前一欄高度 + 注入亂數源產生的隨機增量，並 clamp 在設定範圍內，
    /// 形成自然起伏但不會出現斷崖式跳動的地形（相鄰欄高度差 &lt;= MaxStepPerColumn）。
    /// 固定種子的 <see cref="IRandomSource"/> 可確保生成結果可重現，利於測試。
    /// </summary>
    public sealed class RandomWalkTerrainGenerator : ITerrainGenerator
    {
        public float[] GenerateHeights(int resolution, TerrainGenerationSettings settings,
            IRandomSource random)
        {
            if (resolution < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), "resolution 至少需要 2");
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            float min = Mathf.Min(settings.MinHeight, settings.MaxHeight);
            float max = Mathf.Max(settings.MinHeight, settings.MaxHeight);
            float maxStep = Mathf.Max(0f, settings.MaxStepPerColumn);

            var heights = new float[resolution];
            heights[0] = random.NextFloat(min, max);

            for (int i = 1; i < resolution; i++)
            {
                float delta = random.NextFloat(-maxStep, maxStep);
                heights[i] = Mathf.Clamp(heights[i - 1] + delta, min, max);
            }

            return heights;
        }
    }
}
