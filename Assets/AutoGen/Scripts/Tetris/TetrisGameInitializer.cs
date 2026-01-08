using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris
{
    /// <summary>
    /// 俄罗斯方块游戏初始化器
    /// 在运行时自动创建所有必要的游戏对象和UI
    /// 只需将此脚本挂载到场景中的任意对象上即可
    /// </summary>
    public class TetrisGameInitializer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private float boardCellSize = 1f;
        [SerializeField] private int boardWidth = 10;
        [SerializeField] private int boardHeight = 20;

        private void Awake()
        {
            if (autoInitialize)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// 初始化整个游戏
        /// </summary>
        public void InitializeGame()
        {
            Debug.Log("Tetris: Initializing game...");

            // 1. 创建游戏场景对象
            CreateGameScene();

            // 2. 创建UI
            CreateUI();

            // 3. 设置相机
            SetupCamera();

            Debug.Log("Tetris: Game initialized successfully!");
        }

        private void CreateGameScene()
        {
            // 创建或获取游戏根节点
            GameObject gameRoot = GetOrCreate("TetrisGame", null);

            // 创建游戏板
            GameObject gameBoard = GetOrCreate("GameBoard", gameRoot.transform);
            gameBoard.transform.localPosition = new Vector3(-boardWidth / 2f + 0.5f, -boardHeight / 2f + 0.5f, 0);

            // 添加BoardManager
            BoardManager boardManager = gameBoard.GetComponent<BoardManager>();
            if (boardManager == null)
            {
                boardManager = gameBoard.AddComponent<BoardManager>();
            }

            // 创建已放置方块的父对象
            GameObject placedBlocks = GetOrCreate("PlacedBlocks", gameBoard.transform);
            placedBlocks.transform.localPosition = Vector3.zero;

            // 创建预览区域
            GameObject previewArea = GetOrCreate("PreviewArea", gameRoot.transform);
            previewArea.transform.localPosition = new Vector3(8, 5, 0);

            // 创建边框可视化
            CreateBoardVisuals(gameBoard.transform);

            // 添加生成器
            TetrominoSpawner spawner = gameRoot.GetComponent<TetrominoSpawner>();
            if (spawner == null)
            {
                spawner = gameRoot.AddComponent<TetrominoSpawner>();
            }

            // 添加GameManager
            GameManager gameManager = gameRoot.GetComponent<GameManager>();
            if (gameManager == null)
            {
                gameManager = gameRoot.AddComponent<GameManager>();
            }
        }

        private void CreateBoardVisuals(Transform boardParent)
        {
            // 创建边框
            GameObject border = GetOrCreate("BoardBorder", boardParent);
            border.transform.localPosition = new Vector3(boardWidth / 2f - 0.5f, boardHeight / 2f - 0.5f, 0);

            SpriteRenderer borderSR = border.GetComponent<SpriteRenderer>();
            if (borderSR == null)
            {
                borderSR = border.AddComponent<SpriteRenderer>();
            }
            borderSR.sprite = CreateRectSprite(64, 64);
            borderSR.color = new Color(0.4f, 0.4f, 0.5f, 0.8f);
            borderSR.sortingOrder = -10;
            border.transform.localScale = new Vector3(boardWidth + 0.4f, boardHeight + 0.4f, 1);

            // 创建背景
            GameObject background = GetOrCreate("BoardBackground", boardParent);
            background.transform.localPosition = new Vector3(boardWidth / 2f - 0.5f, boardHeight / 2f - 0.5f, 0);

            SpriteRenderer bgSR = background.GetComponent<SpriteRenderer>();
            if (bgSR == null)
            {
                bgSR = background.AddComponent<SpriteRenderer>();
            }
            bgSR.sprite = CreateRectSprite(64, 64);
            bgSR.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            bgSR.sortingOrder = -11;
            background.transform.localScale = new Vector3(boardWidth, boardHeight, 1);

            // 创建网格线
            CreateGridLines(boardParent);
        }

        private void CreateGridLines(Transform parent)
        {
            GameObject gridContainer = GetOrCreate("GridLines", parent);
            gridContainer.transform.localPosition = Vector3.zero;

            // 创建垂直线
            for (int x = 1; x < boardWidth; x++)
            {
                GameObject line = new GameObject($"VLine_{x}");
                line.transform.SetParent(gridContainer.transform);
                line.transform.localPosition = new Vector3(x - 0.5f, boardHeight / 2f - 0.5f, 0);

                SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
                sr.sprite = CreateRectSprite(2, 64);
                sr.color = new Color(0.2f, 0.2f, 0.3f, 0.3f);
                sr.sortingOrder = -9;
                line.transform.localScale = new Vector3(0.02f, boardHeight, 1);
            }

            // 创建水平线
            for (int y = 1; y < boardHeight; y++)
            {
                GameObject line = new GameObject($"HLine_{y}");
                line.transform.SetParent(gridContainer.transform);
                line.transform.localPosition = new Vector3(boardWidth / 2f - 0.5f, y - 0.5f, 0);

                SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
                sr.sprite = CreateRectSprite(64, 2);
                sr.color = new Color(0.2f, 0.2f, 0.3f, 0.3f);
                sr.sortingOrder = -9;
                line.transform.localScale = new Vector3(boardWidth, 0.02f, 1);
            }
        }

        private void CreateUI()
        {
            // 确保有EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 创建Canvas
            GameObject canvasObj = GetOrCreate("TetrisCanvas", null);
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObj.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObj.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 创建开始面板
            GameObject startPanel = CreatePanel("StartPanel", canvasObj.transform, new Color(0.08f, 0.08f, 0.15f, 0.97f));
            SetupStartPanel(startPanel);

            // 创建游戏面板
            GameObject gamePanel = CreatePanel("GamePanel", canvasObj.transform, new Color(0, 0, 0, 0));
            gamePanel.SetActive(false);
            SetupGamePanel(gamePanel);

            // 创建结算面板
            GameObject gameOverPanel = CreatePanel("GameOverPanel", canvasObj.transform, new Color(0.15f, 0.08f, 0.08f, 0.97f));
            gameOverPanel.SetActive(false);
            SetupGameOverPanel(gameOverPanel);

            // 连接GameManager引用
            ConnectGameManagerReferences(startPanel, gamePanel, gameOverPanel);
        }

        private void SetupStartPanel(GameObject panel)
        {
            // 标题
            GameObject titleObj = CreateText("TitleText", panel.transform, "俄罗斯方块", 80, Color.white);
            SetRectTransform(titleObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180));

            // 副标题
            GameObject subTitleObj = CreateText("SubTitleText", panel.transform, "TETRIS", 40, new Color(0.4f, 0.7f, 1f));
            SetRectTransform(subTitleObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100));

            // 开始按钮
            GameObject startBtn = CreateButton("StartButton", panel.transform, "开始游戏", new Color(0.2f, 0.5f, 0.8f));
            SetRectTransform(startBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(320, 90));

            // 操作说明
            string instructions = "操作说明\n\nA / D  -  左右移动\nS  -  加速下落\n空格  -  快速落底\n回车  -  旋转方块";
            GameObject instructionsObj = CreateText("InstructionsText", panel.transform, instructions, 26, new Color(0.7f, 0.7f, 0.8f));
            SetRectTransform(instructionsObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -200));
        }

        private void SetupGamePanel(GameObject panel)
        {
            // 分数显示
            GameObject scoreObj = CreateText("ScoreText", panel.transform, "分数: 0", 42, Color.white);
            RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.pivot = new Vector2(1, 1);
            scoreRect.anchoredPosition = new Vector2(-60, -60);
            scoreRect.sizeDelta = new Vector2(300, 60);

            // 预览标签
            GameObject previewLabel = CreateText("PreviewLabel", panel.transform, "下一个:", 32, new Color(0.8f, 0.8f, 0.9f));
            RectTransform previewRect = previewLabel.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(1, 1);
            previewRect.anchorMax = new Vector2(1, 1);
            previewRect.pivot = new Vector2(1, 1);
            previewRect.anchoredPosition = new Vector2(-60, -140);
            previewRect.sizeDelta = new Vector2(200, 50);
        }

        private void SetupGameOverPanel(GameObject panel)
        {
            // 游戏结束标题
            GameObject gameOverTitle = CreateText("GameOverTitle", panel.transform, "游戏结束", 80, new Color(1f, 0.4f, 0.4f));
            SetRectTransform(gameOverTitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150));

            // 最终分数
            GameObject finalScore = CreateText("FinalScoreText", panel.transform, "最终分数: 0", 52, Color.white);
            SetRectTransform(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40));

            // 重新开始按钮
            GameObject restartBtn = CreateButton("RestartButton", panel.transform, "重新开始", new Color(0.3f, 0.6f, 0.3f));
            SetRectTransform(restartBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(320, 90));
        }

        private void ConnectGameManagerReferences(GameObject startPanel, GameObject gamePanel, GameObject gameOverPanel)
        {
            GameObject gameRoot = GameObject.Find("TetrisGame");
            if (gameRoot == null) return;

            GameManager gm = gameRoot.GetComponent<GameManager>();
            if (gm == null) return;

            // 使用反射设置私有字段（运行时）
            var gmType = typeof(GameManager);

            SetPrivateField(gm, "startPanel", startPanel);
            SetPrivateField(gm, "gamePanel", gamePanel);
            SetPrivateField(gm, "gameOverPanel", gameOverPanel);

            SetPrivateField(gm, "startButton", startPanel.transform.Find("StartButton")?.GetComponent<Button>());
            SetPrivateField(gm, "restartButton", gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>());
            SetPrivateField(gm, "scoreText", gamePanel.transform.Find("ScoreText")?.GetComponent<TMP_Text>());
            SetPrivateField(gm, "finalScoreText", gameOverPanel.transform.Find("FinalScoreText")?.GetComponent<TMP_Text>());

            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard != null)
            {
                SetPrivateField(gm, "boardManager", gameBoard.GetComponent<BoardManager>());
            }
            SetPrivateField(gm, "spawner", gameRoot.GetComponent<TetrominoSpawner>());
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 设置相机位置以显示游戏板
                mainCamera.transform.position = new Vector3(2, 0, -10);
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 12;
                mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            }
        }

        #region Helper Methods

        private GameObject GetOrCreate(string name, Transform parent)
        {
            Transform existing = parent != null
                ? parent.Find(name)
                : GameObject.Find(name)?.transform;

            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject obj = new GameObject(name);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            return obj;
        }

        private GameObject CreatePanel(string name, Transform parent, Color bgColor)
        {
            GameObject panel = GetOrCreate(name, parent);

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = panel.AddComponent<RectTransform>();
            }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (bgColor.a > 0)
            {
                Image image = panel.GetComponent<Image>();
                if (image == null)
                {
                    image = panel.AddComponent<Image>();
                }
                image.color = bgColor;
            }

            return panel;
        }

        private GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 100);

            TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = color;

            return textObj;
        }

        private GameObject CreateButton(string name, Transform parent, string buttonText, Color buttonColor)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 60);

            Image image = buttonObj.AddComponent<Image>();
            image.color = buttonColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonColor * 1.2f;
            colors.pressedColor = buttonColor * 0.8f;
            colors.selectedColor = buttonColor;
            button.colors = colors;

            // 按钮文本
            GameObject textChild = new GameObject("Text");
            textChild.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textChild.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text tmpText = textChild.AddComponent<TextMeshProUGUI>();
            tmpText.text = buttonText;
            tmpText.fontSize = 36;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;

            return buttonObj;
        }

        private void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2? size = null)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            if (size.HasValue)
            {
                rect.sizeDelta = size.Value;
            }
        }

        private Sprite CreateRectSprite(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        }

        #endregion
    }
}
