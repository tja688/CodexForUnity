using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// 游戏板管理器
    /// 负责管理已放置的方块、检测行消除、边界检测
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 20;
        [SerializeField] private float cellSize = 1f;

        [Header("Visual")]
        [SerializeField] private Transform blocksParent;
        [SerializeField] private SpriteRenderer borderRenderer;

        // 游戏板数据，存储每个格子的方块
        private Transform[,] grid;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            grid = new Transform[width, height];
        }

        /// <summary>
        /// 清空游戏板
        /// </summary>
        public void ClearBoard()
        {
            // 销毁所有已放置的方块
            if (blocksParent != null)
            {
                foreach (Transform child in blocksParent)
                {
                    Destroy(child.gameObject);
                }
            }

            // 重置网格数据
            grid = new Transform[width, height];
        }

        /// <summary>
        /// 检查位置是否在游戏板内
        /// </summary>
        public bool IsInsideBoard(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0;
        }

        /// <summary>
        /// 检查位置是否有效（在边界内且未被占用）
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            // 检查水平边界
            if (pos.x < 0 || pos.x >= width)
                return false;

            // 检查底部边界
            if (pos.y < 0)
                return false;

            // 超过顶部是允许的（方块从上方落下）
            if (pos.y >= height)
                return true;

            // 检查是否被占用
            return grid[pos.x, pos.y] == null;
        }

        /// <summary>
        /// 检查一组位置是否都有效
        /// </summary>
        public bool AreValidPositions(Vector2Int[] positions)
        {
            foreach (var pos in positions)
            {
                if (!IsValidPosition(pos))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 将方块添加到游戏板
        /// </summary>
        public void AddToBoard(Transform[] blocks)
        {
            foreach (var block in blocks)
            {
                if (block == null) continue;

                Vector2Int pos = WorldToGrid(block.position);

                if (pos.y >= 0 && pos.y < height && pos.x >= 0 && pos.x < width)
                {
                    grid[pos.x, pos.y] = block;

                    // 将方块移动到blocksParent下
                    if (blocksParent != null)
                        block.SetParent(blocksParent);
                }
            }
        }

        /// <summary>
        /// 检查并消除满行
        /// </summary>
        public int CheckAndClearLines()
        {
            int linesCleared = 0;

            for (int y = height - 1; y >= 0; y--)
            {
                if (IsLineFull(y))
                {
                    ClearLine(y);
                    MoveDownAbove(y);
                    linesCleared++;
                    y++; // 重新检查当前行（因为上面的行已下移）
                }
            }

            return linesCleared;
        }

        /// <summary>
        /// 检查某行是否已满
        /// </summary>
        private bool IsLineFull(int y)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 清除一行
        /// </summary>
        private void ClearLine(int y)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }

        /// <summary>
        /// 将指定行以上的所有方块下移一行
        /// </summary>
        private void MoveDownAbove(int clearedY)
        {
            for (int y = clearedY + 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] != null)
                    {
                        grid[x, y - 1] = grid[x, y];
                        grid[x, y] = null;
                        grid[x, y - 1].position += Vector3.down * cellSize;
                    }
                }
            }
        }

        /// <summary>
        /// 检查是否游戏结束（顶部被占用）
        /// </summary>
        public bool IsGameOver()
        {
            // 检查顶部两行是否有方块
            for (int x = 0; x < width; x++)
            {
                if (grid[x, height - 1] != null || grid[x, height - 2] != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - transform.position;
            int x = Mathf.RoundToInt(localPos.x / cellSize);
            int y = Mathf.RoundToInt(localPos.y / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return transform.position + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0);
        }

        /// <summary>
        /// 获取生成点位置（顶部中央）
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            return GridToWorld(new Vector2Int(width / 2, height - 1));
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中绘制游戏板边界
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.gray;

            // 绘制边界
            Vector3 bottomLeft = transform.position - new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0);
            Vector3 topRight = bottomLeft + new Vector3(width * cellSize, height * cellSize, 0);

            Gizmos.DrawLine(bottomLeft, new Vector3(bottomLeft.x, topRight.y, 0));
            Gizmos.DrawLine(bottomLeft, new Vector3(topRight.x, bottomLeft.y, 0));
            Gizmos.DrawLine(topRight, new Vector3(bottomLeft.x, topRight.y, 0));
            Gizmos.DrawLine(topRight, new Vector3(topRight.x, bottomLeft.y, 0));

            // 绘制网格
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = bottomLeft + new Vector3(x * cellSize, 0, 0);
                Vector3 end = bottomLeft + new Vector3(x * cellSize, height * cellSize, 0);
                Gizmos.DrawLine(start, end);
            }
            for (int y = 0; y <= height; y++)
            {
                Vector3 start = bottomLeft + new Vector3(0, y * cellSize, 0);
                Vector3 end = bottomLeft + new Vector3(width * cellSize, y * cellSize, 0);
                Gizmos.DrawLine(start, end);
            }
        }
#endif
    }
}
