using System;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// <see cref="ITurnTimer"/> 的預設實作。純邏輯計時器，不依賴 UnityEngine.Time，
    /// 由外層 MonoBehaviour 於每幀呼叫 <see cref="Tick"/> 傳入 deltaTime（US-06 回合限時）。
    /// </summary>
    public sealed class TurnTimer : ITurnTimer
    {
        public float DurationSeconds { get; }

        private float _elapsedSeconds;

        public TurnTimer(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds),
                    "durationSeconds 必須大於 0");
            }

            DurationSeconds = durationSeconds;
            _elapsedSeconds = 0f;
        }

        public float RemainingSeconds => Math.Max(0f, DurationSeconds - _elapsedSeconds);

        public bool HasExpired => _elapsedSeconds >= DurationSeconds;

        public void StartTurn()
        {
            _elapsedSeconds = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "deltaTime 不可為負數");
            }

            _elapsedSeconds += deltaTime;
        }
    }
}
