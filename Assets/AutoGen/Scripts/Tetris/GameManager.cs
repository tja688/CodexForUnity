using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris
{
    /// <summary>
    /// 俄罗斯方块游戏主管理器
    /// 负责游戏状态管理、UI切换、分数统计
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;

        [Header("Game References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private TetrominoSpawner spawner;

        private int score;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 绑定按钮事件
            if (startButton != null)
                startButton.onClick.AddListener(StartGame);
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            // 显示开始界面
            ShowStartPanel();
        }

        /// <summary>
        /// 显示开始界面
        /// </summary>
        public void ShowStartPanel()
        {
            isPlaying = false;
            SetPanelActive(startPanel, true);
            SetPanelActive(gamePanel, false);
            SetPanelActive(gameOverPanel, false);
        }

        /// <summary>
        /// 显示游戏界面
        /// </summary>
        public void ShowGamePanel()
        {
            SetPanelActive(startPanel, false);
            SetPanelActive(gamePanel, true);
            SetPanelActive(gameOverPanel, false);
        }

        /// <summary>
        /// 显示结算界面
        /// </summary>
        public void ShowGameOverPanel()
        {
            isPlaying = false;
            SetPanelActive(startPanel, false);
            SetPanelActive(gamePanel, false);
            SetPanelActive(gameOverPanel, true);

            if (finalScoreText != null)
                finalScoreText.text = $"最终分数: {score}";
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            score = 0;
            UpdateScoreUI();
            ShowGamePanel();
            isPlaying = true;

            // 初始化游戏板
            if (boardManager != null)
                boardManager.ClearBoard();

            // 生成第一个方块
            if (spawner != null)
                spawner.SpawnNext();
        }

        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            StartGame();
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        public void GameOver()
        {
            isPlaying = false;
            ShowGameOverPanel();
        }

        /// <summary>
        /// 增加分数（消除行数）
        /// </summary>
        public void AddScore(int linesCleared)
        {
            // 每消除一行+1分
            score += linesCleared;
            UpdateScoreUI();
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"分数: {score}";
        }

        /// <summary>
        /// 请求生成下一个方块
        /// </summary>
        public void RequestNextTetromino()
        {
            if (spawner != null && isPlaying)
                spawner.SpawnNext();
        }
    }
}
