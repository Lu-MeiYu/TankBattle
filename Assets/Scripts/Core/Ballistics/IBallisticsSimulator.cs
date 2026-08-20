using TankBattle.Core.Shared;

namespace TankBattle.Core.Ballistics
{
    /// <summary>
    /// 正向彈道模擬器（由 Agent A1 於 Phase 1 實作）。
    /// AI 反推角度/威力時，透過此介面反覆試算候選解（正向模擬），
    /// 保證 AI 瞄準的「理論」與實際發射時使用的計算邏輯是同一套。
    /// </summary>
    public interface IBallisticsSimulator
    {
        /// <summary>建立初始模擬狀態。</summary>
        TrajectoryState CreateInitialState(LaunchParameters launch, WindData wind);

        /// <summary>
        /// 推進一個時間步，回傳更新後的狀態；命中地形/出界時 <c>HasEnded</c> 為 true。
        /// 依 Spec 3.3「風力作為水平方向的持續加速度」，<paramref name="wind"/>
        /// 需在每個模擬步都重新套用（而非只在初速上套用一次），因此本介面明確要求
        /// 呼叫端每步都傳入同一次發射所產生的 <see cref="WindData"/>（發射過程中風力不變）。
        /// </summary>
        TrajectoryState Advance(TrajectoryState state, WindData wind, float deltaTime, ITerrainQuery terrain);

        /// <summary>一次算完整條彈道並回傳最終落點，供 AI 試算時使用（不需逐幀播放）。</summary>
        ImpactInfo SimulateToImpact(LaunchParameters launch, WindData wind, ITerrainQuery terrain,
            float maxFlightTime, float simulationStep);
    }

    /// <summary>
    /// 無風力解析解估算器，作為 AI 搜尋的初始猜測值以加速收斂，不保證精確。
    /// </summary>
    public interface IBallisticsEstimator
    {
        /// <summary>回傳建議的角度（度）與威力（百分比）估算值。</summary>
        (float angleDegrees, float powerPercent) EstimateNoWind(UnityEngine.Vector2 shooterPosition,
            UnityEngine.Vector2 targetPosition, float gravity, float muzzleSpeedAtFullPower);
    }
}
