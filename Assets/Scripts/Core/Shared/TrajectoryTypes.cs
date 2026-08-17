using UnityEngine;

namespace TankBattle.Core.Shared
{
    /// <summary>逐步彈道模擬中的單一取樣點。</summary>
    public readonly struct TrajectoryPoint
    {
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;
        public readonly float Time;

        public TrajectoryPoint(Vector2 position, Vector2 velocity, float time)
        {
            Position = position;
            Velocity = velocity;
            Time = time;
        }
    }

    /// <summary>
    /// 逐步彈道模擬的可變狀態，供 Gameplay 的 Projectile 每幀呼叫
    /// <c>IBallisticsSimulator.Advance(state, deltaTime, terrain)</c> 更新。
    /// </summary>
    public readonly struct TrajectoryState
    {
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;
        public readonly float ElapsedTime;
        public readonly bool HasEnded;

        public TrajectoryState(Vector2 position, Vector2 velocity, float elapsedTime, bool hasEnded)
        {
            Position = position;
            Velocity = velocity;
            ElapsedTime = elapsedTime;
            HasEnded = hasEnded;
        }
    }
}
