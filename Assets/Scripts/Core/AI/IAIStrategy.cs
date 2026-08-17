using System.Collections.Generic;
using System.Threading;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.AI
{
    /// <summary>AI 難度分級。</summary>
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>AI 反推角度/威力所需的輸入資料。</summary>
    public readonly struct AimingContext
    {
        public readonly ITankState Self;
        public readonly ITankState Target;
        public readonly WindData Wind;
        public readonly float Gravity;
        public readonly ITerrainQuery Terrain;

        public AimingContext(ITankState self, ITankState target, WindData wind, float gravity,
            ITerrainQuery terrain)
        {
            Self = self;
            Target = target;
            Wind = wind;
            Gravity = gravity;
            Terrain = terrain;
        }
    }

    /// <summary>AI 反推結果。Success = false 時，呼叫端（Turn Flow）應視為跳過本回合。</summary>
    public readonly struct AimResult
    {
        public readonly float AngleDegrees;
        public readonly float PowerPercent;
        public readonly bool Success;

        public AimResult(float angleDegrees, float powerPercent, bool success)
        {
            AngleDegrees = angleDegrees;
            PowerPercent = powerPercent;
            Success = success;
        }

        public static AimResult Failed => new AimResult(0f, 0f, false);
    }

    /// <summary>
    /// AI 策略介面（由 Agent A3 於 Phase 1 實作 Easy/Normal/Hard 三個具體類別）。
    /// SelectTarget 與 DecideAim 皆要求外部注入 <see cref="IRandomSource"/>，
    /// 不得在內部自行建立亂數源，以確保固定種子下決策可重現。
    /// 限時控制採 <see cref="CancellationToken"/>：Turn/Match Flow 呼叫時傳入 token，
    /// AI 定期檢查是否取消，逾時應回傳 <see cref="AimResult.Failed"/>。
    /// </summary>
    public interface IAIStrategy
    {
        AIDifficulty Difficulty { get; }

        ITankState SelectTarget(ITankState self, IReadOnlyList<ITankState> candidates,
            IRandomSource random);

        AimResult DecideAim(AimingContext context, IRandomSource random, CancellationToken cancellationToken);
    }
}
