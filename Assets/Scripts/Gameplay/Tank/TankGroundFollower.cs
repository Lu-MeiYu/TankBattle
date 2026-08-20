using System;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Gameplay.Tank
{
    /// <summary>
    /// 坦克隨地形起伏移動/掉落的純邏輯（Spec 3.6：「若坦克下方地形被炸空，觸發掉落/位置修正邏輯」）。
    /// 每幀呼叫 <see cref="Resolve"/>：若目前位置高於地表，以固定墜落速度逐步下降；
    /// 若目前位置低於或等於地表（例如水平移動後腳下地形改變），立即修正貼齊地表，避免坦克卡入地形。
    /// 不依賴 MonoBehaviour，供 <c>Tank</c> 內部持有並轉發呼叫，方便 NUnit 直接覆蓋。
    /// </summary>
    public sealed class TankGroundFollower
    {
        private readonly float _fallSpeed;

        public TankGroundFollower(float fallSpeed)
        {
            if (fallSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fallSpeed), "fallSpeed 必須大於 0");
            }

            _fallSpeed = fallSpeed;
        }

        /// <summary>回傳修正後的世界座標；X 軸不變，Y 軸依地形貼齊/墜落規則調整。</summary>
        public Vector2 Resolve(Vector2 currentPosition, ITerrainQuery terrain, float deltaTime)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            float surfaceHeight = terrain.GetSurfaceHeight(currentPosition.x);

            if (currentPosition.y <= surfaceHeight)
            {
                // 已貼地或卡入地形（例如水平移動經過較高地形）：立即修正貼齊表面。
                return new Vector2(currentPosition.x, surfaceHeight);
            }

            if (deltaTime <= 0f)
            {
                return currentPosition;
            }

            float fallDistance = _fallSpeed * deltaTime;
            float newY = Mathf.Max(surfaceHeight, currentPosition.y - fallDistance);
            return new Vector2(currentPosition.x, newY);
        }
    }
}
