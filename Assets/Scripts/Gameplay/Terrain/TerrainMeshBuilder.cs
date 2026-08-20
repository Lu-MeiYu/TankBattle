using System;
using UnityEngine;

namespace TankBattle.Gameplay.Terrain
{
    /// <summary>
    /// 依高度陣列建立地形視覺網格頂點/三角形資料的純函式工具（Agent A2，Phase 2：地形渲染）。
    /// 不依賴 MonoBehaviour/GameObject，方便 NUnit 直接驗證頂點與三角形的正確性。
    /// 網格構造：每個取樣欄產生「表面頂點」與「底部頂點」各一個，相鄰欄之間組成一個矩形（兩個三角形），
    /// 形成一條由左到右填滿到世界下界的地形帶狀網格。
    /// </summary>
    public static class TerrainMeshBuilder
    {
        public static void BuildGroundMesh(float[] heights, float mapWidth, float worldMinY,
            out Vector3[] vertices, out int[] triangles)
        {
            if (heights == null || heights.Length < 2)
            {
                throw new ArgumentException("heights 至少需要 2 個取樣點", nameof(heights));
            }

            if (mapWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(mapWidth), "mapWidth 必須大於 0");
            }

            int columns = heights.Length;
            float columnWidth = mapWidth / (columns - 1);

            vertices = new Vector3[columns * 2];
            for (int i = 0; i < columns; i++)
            {
                float x = i * columnWidth;
                vertices[i * 2] = new Vector3(x, heights[i], 0f);
                vertices[i * 2 + 1] = new Vector3(x, worldMinY, 0f);
            }

            triangles = new int[(columns - 1) * 6];
            int t = 0;
            for (int i = 0; i < columns - 1; i++)
            {
                int topLeft = i * 2;
                int bottomLeft = i * 2 + 1;
                int topRight = (i + 1) * 2;
                int bottomRight = (i + 1) * 2 + 1;

                triangles[t++] = topLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomLeft;

                triangles[t++] = bottomLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomRight;
            }
        }
    }
}
