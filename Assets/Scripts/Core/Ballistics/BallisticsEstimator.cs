using System;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.Ballistics
{
    /// <summary>
    /// <see cref="IBallisticsEstimator"/> 的正式實作：無風力解析解估算器。
    ///
    /// 演算法：固定選用 45 度（目標在右側）或 135 度（目標在左側）作為初始猜測角度
    /// （平地情況下 45 度為最大射程角，對稱地 135 度提供朝左的最大射程），再以標準拋物線方程
    /// 反解所需初速：
    /// <code>
    /// dy = dx * tan(θ) - (g * dx^2) / (2 * v^2 * cos^2(θ))
    /// =&gt; v^2 = (g * dx^2) / (2 * cos^2(θ) * (dx * tan(θ) - dy))
    /// </code>
    /// 若分母非正（該角度在此距離下無法涵蓋高度差），則退回建議滿威力，交由呼叫端（AI）
    /// 自行以 <see cref="IBallisticsSimulator"/> 做進一步搜尋收斂，本估算器僅提供「夠接近」
    /// 的初始猜測值，不保證精確（符合介面註解）。
    /// </summary>
    public sealed class BallisticsEstimator : IBallisticsEstimator
    {
        private const float AimRightAngleDegrees = 45f;
        private const float AimLeftAngleDegrees = 135f;
        private const float StraightUpAngleDegrees = 90f;
        private const float HorizontalEpsilon = 0.001f;
        private const float DenominatorEpsilon = 0.0001f;

        public (float angleDegrees, float powerPercent) EstimateNoWind(Vector2 shooterPosition,
            Vector2 targetPosition, float gravity, float muzzleSpeedAtFullPower)
        {
            if (gravity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(gravity), "重力必須為正值");
            }

            if (muzzleSpeedAtFullPower <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(muzzleSpeedAtFullPower), "滿威力初速必須為正值");
            }

            float dx = targetPosition.x - shooterPosition.x;
            float dy = targetPosition.y - shooterPosition.y;

            float angleDegrees;
            float requiredSpeed;

            if (Mathf.Abs(dx) < HorizontalEpsilon)
            {
                angleDegrees = StraightUpAngleDegrees;
                requiredSpeed = dy > 0f
                    ? Mathf.Sqrt(2f * gravity * dy)
                    : muzzleSpeedAtFullPower * 0.1f;
            }
            else
            {
                angleDegrees = dx > 0f ? AimRightAngleDegrees : AimLeftAngleDegrees;
                requiredSpeed = SolveSpeedForAngle(dx, dy, gravity, angleDegrees, muzzleSpeedAtFullPower);
            }

            float clampedAngle = Mathf.Clamp(angleDegrees,
                LaunchParameters.MinAngleDegrees, LaunchParameters.MaxAngleDegrees);
            float powerPercent = Mathf.Clamp(requiredSpeed / muzzleSpeedAtFullPower * 100f,
                LaunchParameters.MinPowerPercent, LaunchParameters.MaxPowerPercent);

            return (clampedAngle, powerPercent);
        }

        private static float SolveSpeedForAngle(float dx, float dy, float gravity, float angleDegrees,
            float fallbackSpeed)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float tan = Mathf.Tan(angleRad);
            float cos = Mathf.Cos(angleRad);
            float cos2 = cos * cos;
            float denominator = cos2 * (dx * tan - dy);

            if (denominator <= DenominatorEpsilon)
            {
                // 此角度在該水平距離下無法涵蓋高度差，回傳滿威力供上層以正向模擬繼續搜尋。
                return fallbackSpeed;
            }

            float speedSquared = (gravity * dx * dx) / (2f * denominator);
            return Mathf.Sqrt(Mathf.Max(speedSquared, 0f));
        }
    }
}
