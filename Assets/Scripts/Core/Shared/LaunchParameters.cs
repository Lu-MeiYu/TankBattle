using UnityEngine;

namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 發射參數：角度（0~180 度）、威力（0~100%）、砲口世界座標、滿威力初速。
    /// 滿威力初速由坦克當前火力/升級狀態決定，由呼叫端（Gameplay/Economy）帶入，
    /// Ballistics 只負責用這個值換算實際初速。
    /// </summary>
    public readonly struct LaunchParameters
    {
        public const float MinAngleDegrees = 0f;
        public const float MaxAngleDegrees = 180f;
        public const float MinPowerPercent = 0f;
        public const float MaxPowerPercent = 100f;

        public readonly float AngleDegrees;
        public readonly float PowerPercent;
        public readonly Vector2 Origin;
        public readonly float MuzzleSpeedAtFullPower;

        public LaunchParameters(float angleDegrees, float powerPercent, Vector2 origin,
            float muzzleSpeedAtFullPower)
        {
            AngleDegrees = angleDegrees;
            PowerPercent = powerPercent;
            Origin = origin;
            MuzzleSpeedAtFullPower = muzzleSpeedAtFullPower;
        }

        /// <summary>將角度/威力夾限在合法範圍內（對應 US-04 的邊界限制）。</summary>
        public static LaunchParameters Clamp(float angleDegrees, float powerPercent, Vector2 origin,
            float muzzleSpeedAtFullPower)
        {
            float clampedAngle = Mathf.Clamp(angleDegrees, MinAngleDegrees, MaxAngleDegrees);
            float clampedPower = Mathf.Clamp(powerPercent, MinPowerPercent, MaxPowerPercent);
            return new LaunchParameters(clampedAngle, clampedPower, origin, muzzleSpeedAtFullPower);
        }
    }
}
