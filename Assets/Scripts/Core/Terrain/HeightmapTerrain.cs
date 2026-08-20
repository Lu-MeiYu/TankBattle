using System;
using System.Collections.Generic;
using TankBattle.Core.Shared;
using UnityEngine;

namespace TankBattle.Core.Terrain
{
    /// <summary>
    /// <see cref="ITerrainQuery"/> 與 <see cref="ITerrainCarver"/> 的預設實作（Agent A2，Phase 1）。
    /// 採一維高度陣列（Heightmap）：X 座標範圍固定 [0, MapWidth]（不置中，對應 SharedContracts §1），
    /// 相鄰取樣點間以線性內插取得平滑表面高度；地形視為「從世界下界一路填滿到表面高度」的實心區域。
    /// </summary>
    public sealed class HeightmapTerrain : ITerrainQuery, ITerrainCarver
    {
        private readonly float[] _heights;
        private readonly float _mapWidth;
        private readonly float _worldMinY;
        private readonly float _worldMaxY;
        private readonly float _columnWidth;

        public int Resolution => _heights.Length;

        public HeightmapTerrain(float mapWidth, float[] initialHeights, float worldMinY, float worldMaxY)
        {
            if (mapWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(mapWidth), "mapWidth 必須大於 0");
            }

            if (initialHeights == null || initialHeights.Length < 2)
            {
                throw new ArgumentException("initialHeights 至少需要 2 個取樣點", nameof(initialHeights));
            }

            if (worldMaxY <= worldMinY)
            {
                throw new ArgumentOutOfRangeException(nameof(worldMaxY), "worldMaxY 必須大於 worldMinY");
            }

            _mapWidth = mapWidth;
            _heights = (float[])initialHeights.Clone();
            _worldMinY = worldMinY;
            _worldMaxY = worldMaxY;
            _columnWidth = mapWidth / (initialHeights.Length - 1);
        }

        public bool IsSolidAt(Vector2 worldPoint)
        {
            if (worldPoint.y < _worldMinY)
            {
                return true;
            }

            float surfaceHeight = GetSurfaceHeight(worldPoint.x);
            return worldPoint.y <= surfaceHeight;
        }

        public float GetSurfaceHeight(float x)
        {
            float clampedX = Mathf.Clamp(x, 0f, _mapWidth);
            float position = clampedX / _columnWidth;
            int index0 = Mathf.Clamp(Mathf.FloorToInt(position), 0, _heights.Length - 2);
            float t = position - index0;
            return Mathf.Lerp(_heights[index0], _heights[index0 + 1], t);
        }

        public bool TryGetCollision(Vector2 fromPoint, Vector2 toPoint, out Vector2 hitPoint)
        {
            if (IsSolidAt(fromPoint))
            {
                hitPoint = fromPoint;
                return true;
            }

            float distance = Vector2.Distance(fromPoint, toPoint);
            if (distance <= 0f)
            {
                hitPoint = toPoint;
                return IsSolidAt(toPoint);
            }

            int steps = Mathf.Max(2, Mathf.CeilToInt(distance / (_columnWidth * 0.5f)));
            Vector2 previous = fromPoint;

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 current = Vector2.Lerp(fromPoint, toPoint, t);

                if (IsSolidAt(current))
                {
                    hitPoint = RefineHit(previous, current);
                    return true;
                }

                previous = current;
            }

            hitPoint = toPoint;
            return false;
        }

        /// <summary>在最後一個非實心點與第一個實心點之間二分逼近，取得較精確的命中點。</summary>
        private Vector2 RefineHit(Vector2 lastFree, Vector2 firstSolid)
        {
            Vector2 low = lastFree;
            Vector2 high = firstSolid;

            for (int i = 0; i < 6; i++)
            {
                Vector2 mid = Vector2.Lerp(low, high, 0.5f);
                if (IsSolidAt(mid))
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            return high;
        }

        public Rect GetWorldBounds()
        {
            return new Rect(0f, _worldMinY, _mapWidth, _worldMaxY - _worldMinY);
        }

        public TerrainModificationResult CarveCrater(Vector2 center, float radius)
        {
            if (radius <= 0f)
            {
                return new TerrainModificationResult(Array.Empty<Vector2>(), false);
            }

            float rawMinX = center.x - radius;
            float rawMaxX = center.x + radius;
            bool wasClamped = rawMinX < 0f || rawMaxX > _mapWidth;

            float clampedMinX = Mathf.Clamp(rawMinX, 0f, _mapWidth);
            float clampedMaxX = Mathf.Clamp(rawMaxX, 0f, _mapWidth);

            int indexMin = Mathf.Clamp(Mathf.FloorToInt(clampedMinX / _columnWidth), 0, _heights.Length - 1);
            int indexMax = Mathf.Clamp(Mathf.CeilToInt(clampedMaxX / _columnWidth), 0, _heights.Length - 1);

            float minAffectedY = float.PositiveInfinity;
            float maxAffectedY = float.NegativeInfinity;
            bool anyColumnChanged = false;

            for (int i = indexMin; i <= indexMax; i++)
            {
                float x = i * _columnWidth;
                float dx = x - center.x;
                float dxSquared = dx * dx;
                float radiusSquared = radius * radius;

                if (dxSquared > radiusSquared)
                {
                    continue;
                }

                float craterBottom = center.y - Mathf.Sqrt(radiusSquared - dxSquared);
                craterBottom = Mathf.Max(craterBottom, _worldMinY);

                if (craterBottom < _heights[i])
                {
                    maxAffectedY = Mathf.Max(maxAffectedY, _heights[i]);
                    _heights[i] = craterBottom;
                    minAffectedY = Mathf.Min(minAffectedY, craterBottom);
                    anyColumnChanged = true;
                }
            }

            if (!anyColumnChanged)
            {
                return new TerrainModificationResult(Array.Empty<Vector2>(), wasClamped);
            }

            var bounds = new List<Vector2>
            {
                new Vector2(clampedMinX, minAffectedY),
                new Vector2(clampedMaxX, maxAffectedY)
            };

            return new TerrainModificationResult(bounds, wasClamped);
        }
    }
}
