using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// 俄罗斯方块形状类型
    /// </summary>
    public enum TetrominoType
    {
        I, // 长条
        O, // 方块
        T, // T形
        S, // S形
        Z, // Z形
        J, // J形
        L  // L形
    }

    /// <summary>
    /// 俄罗斯方块控制器
    /// 负责方块的移动、旋转和下落逻辑
    /// </summary>
    public class Tetromino : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float normalFallInterval = 1f;
        [SerializeField] private float softDropInterval = 0.05f;
        [SerializeField] private float moveRepeatDelay = 0.15f;

        private BoardManager board;
        private Transform[] blocks;
        private TetrominoType type;
        private float fallTimer;
        private float moveTimer;
        private bool isLocked;
        private int rotationState;

        // 每种方块的形状定义（相对于中心点的偏移）
        private static readonly Dictionary<TetrominoType, Vector2Int[,]> ShapeData = new Dictionary<TetrominoType, Vector2Int[,]>
        {
            // I形 - 4种旋转状态
            { TetrominoType.I, new Vector2Int[,] {
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }
            }},
            // O形 - 不旋转
            { TetrominoType.O, new Vector2Int[,] {
                { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }
            }},
            // T形
            { TetrominoType.T, new Vector2Int[,] {
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) },
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, -1) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) }
            }},
            // S形
            { TetrominoType.S, new Vector2Int[,] {
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1) },
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1) }
            }},
            // Z形
            { TetrominoType.Z, new Vector2Int[,] {
                { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
                { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }
            }},
            // J形
            { TetrominoType.J, new Vector2Int[,] {
                { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1) },
                { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }
            }},
            // L形
            { TetrominoType.L, new Vector2Int[,] {
                { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
                { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, -1) },
                { new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
                { new Vector2Int(-1, 1), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }
            }}
        };

        // 每种方块的颜色
        public static readonly Dictionary<TetrominoType, Color> TypeColors = new Dictionary<TetrominoType, Color>
        {
            { TetrominoType.I, new Color(0f, 1f, 1f) },      // 青色
            { TetrominoType.O, new Color(1f, 1f, 0f) },      // 黄色
            { TetrominoType.T, new Color(0.5f, 0f, 0.5f) },  // 紫色
            { TetrominoType.S, new Color(0f, 1f, 0f) },      // 绿色
            { TetrominoType.Z, new Color(1f, 0f, 0f) },      // 红色
            { TetrominoType.J, new Color(0f, 0f, 1f) },      // 蓝色
            { TetrominoType.L, new Color(1f, 0.5f, 0f) }     // 橙色
        };

        public TetrominoType Type => type;

        /// <summary>
        /// 初始化方块
        /// </summary>
        public void Initialize(TetrominoType tetrominoType, Transform[] blockTransforms, BoardManager boardManager)
        {
            type = tetrominoType;
            blocks = blockTransforms;
            board = boardManager;
            rotationState = 0;
            isLocked = false;
            fallTimer = 0f;
            moveTimer = 0f;
        }

        private void Update()
        {
            if (isLocked || !GameManager.Instance.IsPlaying) return;

            HandleInput();
            HandleFall();
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 左移 (A)
            if (Input.GetKeyDown(KeyCode.A))
            {
                TryMove(Vector2Int.left);
                moveTimer = 0f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                moveTimer += Time.deltaTime;
                if (moveTimer >= moveRepeatDelay)
                {
                    TryMove(Vector2Int.left);
                    moveTimer = 0f;
                }
            }

            // 右移 (D)
            if (Input.GetKeyDown(KeyCode.D))
            {
                TryMove(Vector2Int.right);
                moveTimer = 0f;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                moveTimer += Time.deltaTime;
                if (moveTimer >= moveRepeatDelay)
                {
                    TryMove(Vector2Int.right);
                    moveTimer = 0f;
                }
            }

            // 旋转 (Enter/Return)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                TryRotate();
            }

            // 快速下落 (Space) - 直接落到底部
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HardDrop();
            }
        }

        /// <summary>
        /// 处理自动下落
        /// </summary>
        private void HandleFall()
        {
            // S键加速下落
            float interval = Input.GetKey(KeyCode.S) ? softDropInterval : normalFallInterval;

            fallTimer += Time.deltaTime;
            if (fallTimer >= interval)
            {
                fallTimer = 0f;
                if (!TryMove(Vector2Int.down))
                {
                    Lock();
                }
            }
        }

        /// <summary>
        /// 尝试移动
        /// </summary>
        private bool TryMove(Vector2Int direction)
        {
            Vector2Int[] newPositions = GetBlockPositions(direction);

            if (board.AreValidPositions(newPositions))
            {
                transform.position += new Vector3(direction.x, direction.y, 0) * board.CellSize;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试旋转
        /// </summary>
        private void TryRotate()
        {
            if (type == TetrominoType.O) return; // O形不旋转

            int newRotation = (rotationState + 1) % 4;
            Vector2Int[] newPositions = GetPositionsForRotation(newRotation);

            // 尝试直接旋转
            if (board.AreValidPositions(newPositions))
            {
                ApplyRotation(newRotation);
                return;
            }

            // 尝试墙踢（Wall Kick）
            Vector2Int[] kicks = { Vector2Int.left, Vector2Int.right, Vector2Int.up,
                                   new Vector2Int(-2, 0), new Vector2Int(2, 0) };

            foreach (var kick in kicks)
            {
                Vector2Int[] kickedPositions = GetPositionsForRotation(newRotation, kick);
                if (board.AreValidPositions(kickedPositions))
                {
                    transform.position += new Vector3(kick.x, kick.y, 0) * board.CellSize;
                    ApplyRotation(newRotation);
                    return;
                }
            }
        }

        /// <summary>
        /// 应用旋转
        /// </summary>
        private void ApplyRotation(int newRotation)
        {
            rotationState = newRotation;
            UpdateBlockPositions();
        }

        /// <summary>
        /// 更新方块视觉位置以匹配当前旋转状态
        /// </summary>
        private void UpdateBlockPositions()
        {
            var shape = ShapeData[type];
            for (int i = 0; i < 4; i++)
            {
                Vector2Int offset = shape[rotationState, i];
                blocks[i].localPosition = new Vector3(offset.x, offset.y, 0) * board.CellSize;
            }
        }

        /// <summary>
        /// 获取当前方块的网格位置
        /// </summary>
        private Vector2Int[] GetBlockPositions(Vector2Int moveOffset = default)
        {
            Vector2Int[] positions = new Vector2Int[4];
            Vector2Int center = board.WorldToGrid(transform.position) + moveOffset;
            var shape = ShapeData[type];

            for (int i = 0; i < 4; i++)
            {
                positions[i] = center + shape[rotationState, i];
            }
            return positions;
        }

        /// <summary>
        /// 获取指定旋转状态的方块位置
        /// </summary>
        private Vector2Int[] GetPositionsForRotation(int rotation, Vector2Int offset = default)
        {
            Vector2Int[] positions = new Vector2Int[4];
            Vector2Int center = board.WorldToGrid(transform.position) + offset;
            var shape = ShapeData[type];

            for (int i = 0; i < 4; i++)
            {
                positions[i] = center + shape[rotation, i];
            }
            return positions;
        }

        /// <summary>
        /// 快速下落到底部
        /// </summary>
        private void HardDrop()
        {
            while (TryMove(Vector2Int.down)) { }
            Lock();
        }

        /// <summary>
        /// 锁定方块
        /// </summary>
        private void Lock()
        {
            isLocked = true;

            // 分离方块并添加到游戏板
            List<Transform> blockList = new List<Transform>(blocks);
            foreach (var block in blockList)
            {
                block.SetParent(null);
            }
            board.AddToBoard(blocks);

            // 检查消除
            int linesCleared = board.CheckAndClearLines();
            if (linesCleared > 0)
            {
                GameManager.Instance.AddScore(linesCleared);
            }

            // 检查游戏结束
            if (board.IsGameOver())
            {
                GameManager.Instance.GameOver();
            }
            else
            {
                GameManager.Instance.RequestNextTetromino();
            }

            // 销毁Tetromino控制器（方块已分离）
            Destroy(gameObject);
        }

        /// <summary>
        /// 获取指定类型的形状数据
        /// </summary>
        public static Vector2Int[] GetShapeOffsets(TetrominoType tetrominoType, int rotation = 0)
        {
            var shape = ShapeData[tetrominoType];
            Vector2Int[] offsets = new Vector2Int[4];
            for (int i = 0; i < 4; i++)
            {
                offsets[i] = shape[rotation, i];
            }
            return offsets;
        }
    }
}
