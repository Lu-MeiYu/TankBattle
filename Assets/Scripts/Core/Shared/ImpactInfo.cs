using UnityEngine;

namespace TankBattle.Core.Shared
{
    /// <summary>彈道結束的命中類型。</summary>
    public enum ImpactType
    {
        Terrain,
        Tank,
        OutOfBounds
    }

    /// <summary>彈道結束時的命中資訊，供 Combat 的 <c>IExplosionResolver</c> 使用。</summary>
    public readonly struct ImpactInfo
    {
        public readonly ImpactType Type;
        public readonly Vector2 Point;
        public readonly float FlightTime;

        public ImpactInfo(ImpactType type, Vector2 point, float flightTime)
        {
            Type = type;
            Point = point;
            FlightTime = flightTime;
        }
    }
}
