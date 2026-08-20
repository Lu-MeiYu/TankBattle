using System;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Gameplay.Tank
{
    /// <summary>
    /// 坦克瞄準狀態的純邏輯持有者（US-04）。角度/威力在 <see cref="SetAim"/> 當下即被夾限
    /// 在合法範圍內，讓 UI 滑桿可以立即反映被夾限後的數值。不依賴 MonoBehaviour，
    /// 供 <c>Tank</c> 內部持有並轉發呼叫，亦方便 NUnit 直接覆蓋。
    /// </summary>
    public sealed class TankAimState
    {
        public float AngleDegrees { get; private set; }
        public float PowerPercent { get; private set; }

        public TankAimState(float initialAngleDegrees = 45f, float initialPowerPercent = 50f)
        {
            SetAim(initialAngleDegrees, initialPowerPercent);
        }

        /// <summary>設定瞄準角度/威力；超出合法範圍時自動夾限（US-04 Acceptance Criteria）。</summary>
        public void SetAim(float angleDegrees, float powerPercent)
        {
            AngleDegrees = Mathf.Clamp(angleDegrees, LaunchParameters.MinAngleDegrees,
                LaunchParameters.MaxAngleDegrees);
            PowerPercent = Mathf.Clamp(powerPercent, LaunchParameters.MinPowerPercent,
                LaunchParameters.MaxPowerPercent);
        }

        /// <summary>以目前瞄準狀態組出發射參數，供開火時建立 <see cref="LaunchParameters"/>。</summary>
        public LaunchParameters BuildLaunchParameters(Vector2 origin, float muzzleSpeedAtFullPower)
        {
            if (muzzleSpeedAtFullPower < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(muzzleSpeedAtFullPower),
                    "muzzleSpeedAtFullPower 不可為負數");
            }

            return LaunchParameters.Clamp(AngleDegrees, PowerPercent, origin, muzzleSpeedAtFullPower);
        }
    }
}
