using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// 俄罗斯方块生成器
    /// 负责生成方块和管理预览
    /// </summary>
    public class TetrominoSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GameObject blockPrefab;

        [Header("Preview")]
        [SerializeField] private Transform previewParent;
        [SerializeField] private float previewScale = 0.6f;

        private TetrominoType? nextType;
        private List<GameObject> previewBlocks = new List<GameObject>();

        private void Start()
        {
            // 自动查找BoardManager（如果未设置）
            if (boardManager == null)
            {
                boardManager = FindObjectOfType<BoardManager>();
            }

            // 预生成下一个方块类型
            nextType = GetRandomType();
        }

        /// <summary>
        /// 生成下一个方块
        /// </summary>
        public void SpawnNext()
        {
            if (boardManager == null)
            {
                boardManager = FindObjectOfType<BoardManager>();
                if (boardManager == null)
                {
                    Debug.LogError("TetrominoSpawner: BoardManager not found!");
                    return;
                }
            }

            // 使用预览的方块类型
            TetrominoType typeToSpawn = nextType ?? GetRandomType();

            // 预生成下一个
            nextType = GetRandomType();
            UpdatePreview();

            // 创建方块
            SpawnTetromino(typeToSpawn);
        }

        /// <summary>
        /// 生成指定类型的方块
        /// </summary>
        private void SpawnTetromino(TetrominoType type)
        {
            // 创建父对象
            GameObject tetrominoObj = new GameObject($"Tetromino_{type}");
            tetrominoObj.transform.position = boardManager.GetSpawnPosition();

            // 创建4个方块
            Transform[] blocks = new Transform[4];
            Color color = Tetromino.TypeColors[type];
            Vector2Int[] offsets = Tetromino.GetShapeOffsets(type);

            for (int i = 0; i < 4; i++)
            {
                GameObject block = CreateBlock();
                block.transform.SetParent(tetrominoObj.transform);
                block.transform.localPosition = new Vector3(offsets[i].x, offsets[i].y, 0) * boardManager.CellSize;

                // 设置颜色
                var sr = block.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = color;
                }

                blocks[i] = block.transform;
            }

            // 添加控制器
            Tetromino controller = tetrominoObj.AddComponent<Tetromino>();
            controller.Initialize(type, blocks, boardManager);
        }

        /// <summary>
        /// 创建单个方块
        /// </summary>
        private GameObject CreateBlock()
        {
            if (blockPrefab != null)
            {
                return Instantiate(blockPrefab);
            }

            // 如果没有预制体，运行时动态创建
            GameObject block = new GameObject("Block");
            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = BlockUnit.GetSharedBlockSprite();
            return block;
        }

        /// <summary>
        /// 更新预览显示
        /// </summary>
        private void UpdatePreview()
        {
            // 清除旧预览
            foreach (var block in previewBlocks)
            {
                if (block != null)
                    Destroy(block);
            }
            previewBlocks.Clear();

            if (!nextType.HasValue) return;

            // 如果没有预览父对象，使用默认位置
            Transform previewRoot = previewParent;
            if (previewRoot == null)
            {
                // 尝试查找预览区域
                GameObject previewObj = GameObject.Find("PreviewArea");
                if (previewObj != null)
                {
                    previewRoot = previewObj.transform;
                }
                else
                {
                    // 创建一个默认的预览父对象
                    previewObj = new GameObject("PreviewArea");
                    previewObj.transform.position = new Vector3(8, 5, 0);
                    previewRoot = previewObj.transform;
                    previewParent = previewRoot;
                }
            }

            // 创建新预览
            TetrominoType type = nextType.Value;
            Color color = Tetromino.TypeColors[type];
            Vector2Int[] offsets = Tetromino.GetShapeOffsets(type);

            // 计算中心偏移以居中显示
            Vector2 center = CalculateCenter(offsets);
            float cellSize = boardManager != null ? boardManager.CellSize : 1f;

            for (int i = 0; i < 4; i++)
            {
                GameObject block = CreateBlock();
                block.transform.SetParent(previewRoot);
                Vector3 pos = new Vector3(
                    (offsets[i].x - center.x) * cellSize * previewScale,
                    (offsets[i].y - center.y) * cellSize * previewScale,
                    0
                );
                block.transform.localPosition = pos;
                block.transform.localScale = Vector3.one * previewScale;

                var sr = block.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = color;
                    sr.sortingOrder = 100; // 确保预览在前面
                }

                previewBlocks.Add(block);
            }
        }

        /// <summary>
        /// 计算形状的中心点
        /// </summary>
        private Vector2 CalculateCenter(Vector2Int[] offsets)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var offset in offsets)
            {
                minX = Mathf.Min(minX, offset.x);
                maxX = Mathf.Max(maxX, offset.x);
                minY = Mathf.Min(minY, offset.y);
                maxY = Mathf.Max(maxY, offset.y);
            }

            return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        }

        /// <summary>
        /// 获取随机方块类型
        /// </summary>
        private TetrominoType GetRandomType()
        {
            int count = System.Enum.GetValues(typeof(TetrominoType)).Length;
            return (TetrominoType)Random.Range(0, count);
        }

        /// <summary>
        /// 清除预览
        /// </summary>
        public void ClearPreview()
        {
            foreach (var block in previewBlocks)
            {
                if (block != null)
                    Destroy(block);
            }
            previewBlocks.Clear();
            nextType = null;
        }
    }
}
