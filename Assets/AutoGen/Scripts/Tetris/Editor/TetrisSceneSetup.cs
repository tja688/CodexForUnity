using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris.Editor
{
    /// <summary>
    /// 俄罗斯方块场景设置工具
    /// 在Unity编辑器中运行以自动配置整个游戏场景
    /// </summary>
    public class TetrisSceneSetup : EditorWindow
    {
        [MenuItem("Tools/Tetris/Setup Scene")]
        public static void ShowWindow()
        {
            GetWindow<TetrisSceneSetup>("Tetris Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("俄罗斯方块场景设置", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. 创建方块预制体", GUILayout.Height(30)))
            {
                CreateBlockPrefab();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. 设置游戏场景", GUILayout.Height(30)))
            {
                SetupGameScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("3. 设置UI界面", GUILayout.Height(30)))
            {
                SetupUI();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("一键完成全部设置", GUILayout.Height(40)))
            {
                CreateBlockPrefab();
                SetupGameScene();
                SetupUI();
                Debug.Log("俄罗斯方块场景设置完成！");
            }
        }

        private static void CreateBlockPrefab()
        {
            // 确保目录存在
            string prefabDir = "Assets/AutoGen/Prefabs/Tetris";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                AssetDatabase.CreateFolder("Assets/AutoGen/Prefabs", "Tetris");
            }

            // 创建方块GameObject
            GameObject block = new GameObject("Block");
            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();

            // 使用Unity内置的白色方形sprite
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = Color.white;

            // 保存为预制体
            string prefabPath = "Assets/AutoGen/Prefabs/Tetris/Block.prefab";
            PrefabUtility.SaveAsPrefabAsset(block, prefabPath);
            DestroyImmediate(block);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"方块预制体已创建: {prefabPath}");
        }

        private static void SetupGameScene()
        {
            // 创建或获取游戏根节点
            GameObject gameRoot = GetOrCreateGameObject("TetrisGame", null);

            // 创建游戏板
            GameObject gameBoard = GetOrCreateGameObject("GameBoard", gameRoot.transform);
            gameBoard.transform.localPosition = new Vector3(-4.5f, -9.5f, 0);

            // 添加BoardManager组件
            if (gameBoard.GetComponent<BoardManager>() == null)
            {
                gameBoard.AddComponent<BoardManager>();
            }

            // 创建已放置方块的父对象
            GameObject placedBlocks = GetOrCreateGameObject("PlacedBlocks", gameBoard.transform);
            placedBlocks.transform.localPosition = Vector3.zero;

            // 创建预览区域
            GameObject previewArea = GetOrCreateGameObject("PreviewArea", gameRoot.transform);
            previewArea.transform.localPosition = new Vector3(8, 5, 0);

            // 创建边框
            GameObject border = GetOrCreateGameObject("BoardBorder", gameBoard.transform);
            border.transform.localPosition = new Vector3(4.5f, 9.5f, 0);
            border.transform.localScale = new Vector3(10.5f, 20.5f, 1);

            SpriteRenderer borderSR = border.GetComponent<SpriteRenderer>();
            if (borderSR == null)
            {
                borderSR = border.AddComponent<SpriteRenderer>();
            }
            borderSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            borderSR.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            borderSR.sortingOrder = -10;

            // 创建内部填充背景
            GameObject bgFill = GetOrCreateGameObject("BoardBackground", gameBoard.transform);
            bgFill.transform.localPosition = new Vector3(4.5f, 9.5f, 0);
            bgFill.transform.localScale = new Vector3(10f, 20f, 1);

            SpriteRenderer bgSR = bgFill.GetComponent<SpriteRenderer>();
            if (bgSR == null)
            {
                bgSR = bgFill.AddComponent<SpriteRenderer>();
            }
            bgSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bgSR.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            bgSR.sortingOrder = -11;

            // 添加生成器
            if (gameRoot.GetComponent<TetrominoSpawner>() == null)
            {
                gameRoot.AddComponent<TetrominoSpawner>();
            }

            // 设置BoardManager引用
            BoardManager boardManager = gameBoard.GetComponent<BoardManager>();
            SerializedObject boardSO = new SerializedObject(boardManager);
            boardSO.FindProperty("blocksParent").objectReferenceValue = placedBlocks.transform;
            boardSO.ApplyModifiedProperties();

            // 设置Spawner引用
            TetrominoSpawner spawner = gameRoot.GetComponent<TetrominoSpawner>();
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("boardManager").objectReferenceValue = boardManager;
            spawnerSO.FindProperty("previewParent").objectReferenceValue = previewArea.transform;

            // 加载方块预制体
            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AutoGen/Prefabs/Tetris/Block.prefab");
            if (blockPrefab != null)
            {
                spawnerSO.FindProperty("blockPrefab").objectReferenceValue = blockPrefab;
            }
            spawnerSO.ApplyModifiedProperties();

            Debug.Log("游戏场景设置完成！");
        }

        private static void SetupUI()
        {
            // 确保有EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 创建Canvas
            GameObject canvasObj = GetOrCreateGameObject("TetrisCanvas", null);
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
            GameObject startPanel = CreateUIPanel("StartPanel", canvasObj.transform, new Color(0.1f, 0.1f, 0.2f, 0.95f));
            SetupStartPanel(startPanel);

            // 创建游戏面板
            GameObject gamePanel = CreateUIPanel("GamePanel", canvasObj.transform, new Color(0, 0, 0, 0));
            gamePanel.SetActive(false);
            SetupGamePanel(gamePanel);

            // 创建结算面板
            GameObject gameOverPanel = CreateUIPanel("GameOverPanel", canvasObj.transform, new Color(0.2f, 0.1f, 0.1f, 0.95f));
            gameOverPanel.SetActive(false);
            SetupGameOverPanel(gameOverPanel);

            // 创建GameManager
            GameObject gameRoot = GameObject.Find("TetrisGame");
            if (gameRoot == null)
            {
                gameRoot = new GameObject("TetrisGame");
            }

            GameManager gameManager = gameRoot.GetComponent<GameManager>();
            if (gameManager == null)
            {
                gameManager = gameRoot.AddComponent<GameManager>();
            }

            // 设置GameManager引用
            SerializedObject gmSO = new SerializedObject(gameManager);
            gmSO.FindProperty("startPanel").objectReferenceValue = startPanel;
            gmSO.FindProperty("gamePanel").objectReferenceValue = gamePanel;
            gmSO.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            gmSO.FindProperty("startButton").objectReferenceValue = startPanel.transform.Find("StartButton")?.GetComponent<Button>();
            gmSO.FindProperty("restartButton").objectReferenceValue = gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>();
            gmSO.FindProperty("scoreText").objectReferenceValue = gamePanel.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            gmSO.FindProperty("finalScoreText").objectReferenceValue = gameOverPanel.transform.Find("FinalScoreText")?.GetComponent<TMP_Text>();

            // 获取BoardManager和Spawner引用
            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard != null)
            {
                gmSO.FindProperty("boardManager").objectReferenceValue = gameBoard.GetComponent<BoardManager>();
            }
            gmSO.FindProperty("spawner").objectReferenceValue = gameRoot.GetComponent<TetrominoSpawner>();

            gmSO.ApplyModifiedProperties();

            Debug.Log("UI界面设置完成！");
        }

        private static void SetupStartPanel(GameObject panel)
        {
            // 标题
            GameObject titleObj = CreateTextObject("TitleText", panel.transform, "俄罗斯方块", 72);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);

            // 副标题
            GameObject subTitleObj = CreateTextObject("SubTitleText", panel.transform, "TETRIS", 36);
            RectTransform subRect = subTitleObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, 80);
            subTitleObj.GetComponent<TMP_Text>().color = new Color(0.5f, 0.8f, 1f);

            // 开始按钮
            GameObject startBtn = CreateButton("StartButton", panel.transform, "开始游戏");
            RectTransform startBtnRect = startBtn.GetComponent<RectTransform>();
            startBtnRect.anchoredPosition = new Vector2(0, -50);
            startBtnRect.sizeDelta = new Vector2(300, 80);

            // 操作说明
            string instructions = "操作说明:\nA/D - 左右移动\nS - 加速下落\n空格 - 快速落底\n回车 - 旋转";
            GameObject instructionsObj = CreateTextObject("InstructionsText", panel.transform, instructions, 24);
            RectTransform insRect = instructionsObj.GetComponent<RectTransform>();
            insRect.anchoredPosition = new Vector2(0, -200);
            instructionsObj.GetComponent<TMP_Text>().color = new Color(0.7f, 0.7f, 0.7f);
        }

        private static void SetupGamePanel(GameObject panel)
        {
            // 分数显示
            GameObject scoreObj = CreateTextObject("ScoreText", panel.transform, "分数: 0", 36);
            RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.pivot = new Vector2(1, 1);
            scoreRect.anchoredPosition = new Vector2(-50, -50);

            // 预览标签
            GameObject previewLabel = CreateTextObject("PreviewLabel", panel.transform, "下一个:", 28);
            RectTransform previewRect = previewLabel.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(1, 1);
            previewRect.anchorMax = new Vector2(1, 1);
            previewRect.pivot = new Vector2(1, 1);
            previewRect.anchoredPosition = new Vector2(-50, -150);
        }

        private static void SetupGameOverPanel(GameObject panel)
        {
            // 游戏结束标题
            GameObject gameOverTitle = CreateTextObject("GameOverTitle", panel.transform, "游戏结束", 72);
            RectTransform titleRect = gameOverTitle.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);
            gameOverTitle.GetComponent<TMP_Text>().color = new Color(1f, 0.3f, 0.3f);

            // 最终分数
            GameObject finalScore = CreateTextObject("FinalScoreText", panel.transform, "最终分数: 0", 48);
            RectTransform scoreRect = finalScore.GetComponent<RectTransform>();
            scoreRect.anchoredPosition = new Vector2(0, 30);

            // 重新开始按钮
            GameObject restartBtn = CreateButton("RestartButton", panel.transform, "重新开始");
            RectTransform restartRect = restartBtn.GetComponent<RectTransform>();
            restartRect.anchoredPosition = new Vector2(0, -100);
            restartRect.sizeDelta = new Vector2(300, 80);
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Color bgColor)
        {
            GameObject panel = GetOrCreateGameObject(name, parent);

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = panel.AddComponent<RectTransform>();
            }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            if (image == null && bgColor.a > 0)
            {
                image = panel.AddComponent<Image>();
                image.color = bgColor;
            }
            else if (image != null)
            {
                image.color = bgColor;
            }

            return panel;
        }

        private static GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
        {
            // 先检查是否已存在
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
            tmpText.color = Color.white;

            return textObj;
        }

        private static GameObject CreateButton(string name, Transform parent, string buttonText)
        {
            // 先检查是否已存在
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
            image.color = new Color(0.2f, 0.5f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.4f, 0.7f);
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
            tmpText.fontSize = 32;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;

            return buttonObj;
        }

        private static GameObject GetOrCreateGameObject(string name, Transform parent)
        {
            Transform existing = parent != null ? parent.Find(name) : GameObject.Find(name)?.transform;

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
    }
}
