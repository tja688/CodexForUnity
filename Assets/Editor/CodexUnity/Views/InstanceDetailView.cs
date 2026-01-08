using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CodexUnity.Views
{
    /// <summary>
    /// 实例详情视图 - 显示单个实例的对话历史和控制
    /// </summary>
    public class InstanceDetailView : VisualElement
    {
        private readonly string _instanceId;
        private CodexRunnerInstance _runner;
        private InstanceInfo _instanceInfo;

        // UI 元素
        private TextField _promptField;
        private TextField _modelField;
        private DropdownField _effortField;
        private Toggle _debugToggle;
        private ScrollView _historyScroll;
        private Label _statusTextLabel;
        private Label _statusMetaLabel;
        private VisualElement _statusBar;
        private HelpBox _statusBox;
        private Button _sendButton;
        private Button _newTaskButton;
        private Button _killButton;
        private Button _backButton;

        private VisualTreeAsset _bubbleTemplate;

        // 消息归并状态
        private ChatBubbleElement _currentAssistantBubble;
        private string _currentRunId;
        private readonly StringBuilder _streamBuffer = new();
        private int _streamLineCount;
        private double _lastScrollTime;

        private bool _codexAvailable;
        private bool _hasGitRepo;

        public event Action OnBackRequested;

        public InstanceDetailView(string instanceId)
        {
            _instanceId = instanceId;
            _runner = InstanceManager.Instance.GetOrCreateRunner(instanceId);
            _instanceInfo = InstanceManager.Instance.GetInstanceInfo(instanceId);

            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            BuildUI();
            BindEvents();
            LoadConversation();
            RefreshRunStatus();
            UpdateSendState();
        }

        private void BuildUI()
        {
            // 加载资源
            var bubbleTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/CodexUnity/UI/ChatBubble.uxml");
            var bubbleStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CodexUnity/UI/ChatBubble.uss");
            var windowStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CodexUnity/UI/CodexWindow.uss");
            _bubbleTemplate = bubbleTemplate;

            if (windowStyle != null)
            {
                styleSheets.Add(windowStyle);
            }
            if (bubbleStyle != null)
            {
                styleSheets.Add(bubbleStyle);
            }

            // 顶部导航栏
            var navBar = new VisualElement();
            navBar.style.flexDirection = FlexDirection.Row;
            navBar.style.alignItems = Align.Center;
            navBar.style.paddingTop = 8;
            navBar.style.paddingBottom = 8;
            navBar.style.paddingLeft = 12;
            navBar.style.paddingRight = 12;
            navBar.style.backgroundColor = new Color(0.133f, 0.133f, 0.157f);
            navBar.style.borderBottomWidth = 1;
            navBar.style.borderBottomColor = new Color(1, 1, 1, 0.06f);

            _backButton = new Button(() => OnBackRequested?.Invoke());
            _backButton.text = "← 返回";
            _backButton.AddToClassList("ghost-button");
            navBar.Add(_backButton);

            var instanceName = new Label(_instanceInfo?.name ?? $"Instance {_instanceId[..8]}");
            instanceName.style.fontSize = 14;
            instanceName.style.unityFontStyleAndWeight = FontStyle.Bold;
            instanceName.style.marginLeft = 12;
            instanceName.style.color = new Color(0.91f, 0.91f, 0.93f);
            navBar.Add(instanceName);

            Add(navBar);

            // 主内容区域
            var mainContent = new VisualElement();
            mainContent.style.flexGrow = 1;
            mainContent.style.flexDirection = FlexDirection.Column;
            mainContent.style.paddingTop = 12;
            mainContent.style.paddingBottom = 12;
            mainContent.style.paddingLeft = 12;
            mainContent.style.paddingRight = 12;
            mainContent.style.backgroundColor = new Color(0.102f, 0.102f, 0.122f);

            // 状态栏
            _statusBar = new VisualElement();
            _statusBar.AddToClassList("status-bar");
            _statusBar.AddToClassList("status-idle");
            _statusBar.style.flexDirection = FlexDirection.Row;
            _statusBar.style.justifyContent = Justify.SpaceBetween;
            _statusBar.style.alignItems = Align.Center;
            _statusBar.style.paddingTop = 8;
            _statusBar.style.paddingBottom = 8;
            _statusBar.style.paddingLeft = 12;
            _statusBar.style.paddingRight = 12;
            _statusBar.style.borderTopLeftRadius = 6;
            _statusBar.style.borderTopRightRadius = 6;
            _statusBar.style.borderBottomLeftRadius = 6;
            _statusBar.style.borderBottomRightRadius = 6;
            _statusBar.style.marginBottom = 8;
            _statusBar.style.backgroundColor = new Color(0.31f, 0.80f, 0.77f, 0.15f);

            _statusTextLabel = new Label("Idle");
            _statusTextLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusTextLabel.style.fontSize = 13;
            _statusTextLabel.style.color = new Color(0.31f, 0.80f, 0.77f);
            _statusBar.Add(_statusTextLabel);

            _statusMetaLabel = new Label("No active run");
            _statusMetaLabel.style.fontSize = 10;
            _statusMetaLabel.style.color = new Color(0.42f, 0.42f, 0.47f);
            _statusBar.Add(_statusMetaLabel);

            mainContent.Add(_statusBar);

            // 历史记录区域
            var historyCard = new VisualElement();
            historyCard.AddToClassList("card");
            historyCard.AddToClassList("history-card");
            historyCard.style.flexGrow = 1;
            historyCard.style.backgroundColor = new Color(0.165f, 0.165f, 0.196f);
            historyCard.style.borderTopLeftRadius = 10;
            historyCard.style.borderTopRightRadius = 10;
            historyCard.style.borderBottomLeftRadius = 10;
            historyCard.style.borderBottomRightRadius = 10;
            historyCard.style.paddingTop = 10;
            historyCard.style.paddingBottom = 10;
            historyCard.style.paddingLeft = 10;
            historyCard.style.paddingRight = 10;
            historyCard.style.marginBottom = 8;

            var historyTitle = new Label("Conversation");
            historyTitle.style.fontSize = 12;
            historyTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            historyTitle.style.marginBottom = 6;
            historyTitle.style.color = new Color(0.91f, 0.91f, 0.93f);
            historyCard.Add(historyTitle);

            _historyScroll = new ScrollView(ScrollViewMode.Vertical);
            _historyScroll.style.flexGrow = 1;
            _historyScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            historyCard.Add(_historyScroll);

            mainContent.Add(historyCard);

            // 输入区域
            var inputCard = new VisualElement();
            inputCard.AddToClassList("card");
            inputCard.AddToClassList("input-card");
            inputCard.style.backgroundColor = new Color(0.165f, 0.165f, 0.196f);
            inputCard.style.borderTopLeftRadius = 10;
            inputCard.style.borderTopRightRadius = 10;
            inputCard.style.borderBottomLeftRadius = 10;
            inputCard.style.borderBottomRightRadius = 10;
            inputCard.style.paddingTop = 10;
            inputCard.style.paddingBottom = 10;
            inputCard.style.paddingLeft = 10;
            inputCard.style.paddingRight = 10;

            var inputTitle = new Label("Prompt");
            inputTitle.style.fontSize = 12;
            inputTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            inputTitle.style.marginBottom = 6;
            inputTitle.style.color = new Color(0.91f, 0.91f, 0.93f);
            inputCard.Add(inputTitle);

            // 输入行
            var inputRow = new VisualElement();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.flexWrap = Wrap.Wrap;
            inputRow.style.marginBottom = 8;

            _modelField = new TextField("Model");
            _modelField.value = _runner.State.model ?? "gpt-5.1-codex-mini";
            _modelField.style.flexGrow = 1;
            _modelField.style.minWidth = 80;
            _modelField.style.marginRight = 8;  // 模拟 gap
            inputRow.Add(_modelField);

            _effortField = new DropdownField("Effort", new List<string> { "minimal", "low", "medium", "high", "xhigh" }, 2);
            _effortField.value = _runner.State.effort ?? "medium";
            _effortField.style.flexGrow = 1;
            _effortField.style.minWidth = 80;
            _effortField.style.marginRight = 8;  // 模拟 gap
            inputRow.Add(_effortField);

            _debugToggle = new Toggle("Debug");
            _debugToggle.value = _runner.State.debug;
            _debugToggle.style.minWidth = 60;
            inputRow.Add(_debugToggle);

            inputCard.Add(inputRow);

            // Prompt 输入框
            _promptField = new TextField("Message");
            _promptField.multiline = true;
            _promptField.style.minHeight = 80;
            _promptField.style.maxHeight = 160;
            _promptField.RegisterValueChangedCallback(_ => UpdateSendState());
            inputCard.Add(_promptField);

            // 按钮行
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.flexWrap = Wrap.Wrap;
            buttonRow.style.marginTop = 8;

            _sendButton = new Button(Send);
            _sendButton.text = "Send";
            _sendButton.AddToClassList("primary-button");
            _sendButton.style.marginRight = 6;  // 模拟 gap
            buttonRow.Add(_sendButton);

            _newTaskButton = new Button(NewTask);
            _newTaskButton.text = "New Task";
            _newTaskButton.AddToClassList("ghost-button");
            _newTaskButton.style.marginRight = 6;  // 模拟 gap
            buttonRow.Add(_newTaskButton);

            _killButton = new Button(KillRun);
            _killButton.text = "Kill";
            _killButton.AddToClassList("danger-button");
            buttonRow.Add(_killButton);

            inputCard.Add(buttonRow);
            mainContent.Add(inputCard);

            // 状态消息框
            _statusBox = new HelpBox();
            _statusBox.style.display = DisplayStyle.None;
            _statusBox.style.marginTop = 4;
            mainContent.Add(_statusBox);

            Add(mainContent);
        }

        private void BindEvents()
        {
            _runner.OnHistoryItemAppended += OnHistoryItemAppended;
            _runner.OnStatusChanged += OnStatusChanged;

            _hasGitRepo = CodexStore.HasGitRepository();
            _codexAvailable = CodexRunnerInstance.CheckCodexAvailableStatic();
        }

        public void Cleanup()
        {
            if (_runner != null)
            {
                _runner.OnHistoryItemAppended -= OnHistoryItemAppended;
                _runner.OnStatusChanged -= OnStatusChanged;
            }
        }

        private void LoadConversation()
        {
            _historyScroll?.Clear();
            _currentAssistantBubble = null;
            _currentRunId = null;
            _streamBuffer.Clear();
            _streamLineCount = 0;

            var historyItems = _runner.LoadHistory();

            ChatBubbleElement lastAssistantBubble = null;
            StringBuilder assistantContent = new StringBuilder();

            foreach (var item in historyItems)
            {
                var kind = GetItemKind(item);

                if (kind == "user")
                {
                    if (lastAssistantBubble != null && assistantContent.Length > 0)
                    {
                        lastAssistantBubble.CompleteStream(GetFinalContent(assistantContent.ToString()), true);
                    }
                    lastAssistantBubble = null;
                    assistantContent.Clear();

                    var userBubble = CreateBubble();
                    userBubble.BindUserMessage(item.text, item.ts);
                    _historyScroll?.Add(userBubble);
                }
                else if (kind == "assistant")
                {
                    if (lastAssistantBubble != null)
                    {
                        assistantContent.AppendLine(item.text);
                    }
                    else
                    {
                        lastAssistantBubble = CreateBubble();
                        lastAssistantBubble.BindSystemMessage(item.text, item.ts);
                        lastAssistantBubble.Bind(item, false, 0);
                        _historyScroll?.Add(lastAssistantBubble);
                    }
                }
                else if (kind == "system")
                {
                    var sysBubble = CreateBubble();
                    sysBubble.BindSystemMessage(item.text, item.ts);
                    _historyScroll?.Add(sysBubble);
                }
            }

            if (lastAssistantBubble != null && assistantContent.Length > 0)
            {
                lastAssistantBubble.CompleteStream(GetFinalContent(assistantContent.ToString()), true);
            }

            ScrollToBottom();
        }

        private void OnHistoryItemAppended(HistoryItem item)
        {
            if (item == null) return;

            var kind = GetItemKind(item);

            if (kind == "user")
            {
                if (_currentAssistantBubble != null && _currentAssistantBubble.IsStreaming)
                {
                    _currentAssistantBubble.CompleteStream(GetFinalContent(_streamBuffer.ToString()), true);
                }

                _currentAssistantBubble = null;
                _streamBuffer.Clear();
                _streamLineCount = 0;

                var userBubble = CreateBubble();
                userBubble.BindUserMessage(item.text, item.ts);
                _historyScroll?.Add(userBubble);
                ScrollToBottom();
                return;
            }

            if (kind == "assistant")
            {
                if (_currentAssistantBubble != null && _currentAssistantBubble.IsStreaming)
                {
                    _currentAssistantBubble.CompleteStream(item.text, true);
                    _currentAssistantBubble = null;
                    _streamBuffer.Clear();
                    _streamLineCount = 0;
                }
                else
                {
                    var bubble = CreateBubble();
                    bubble.Bind(item, false, 0);
                    _historyScroll?.Add(bubble);
                }
                ScrollToBottom();
                return;
            }

            if (kind == "system")
            {
                var sysBubble = CreateBubble();
                sysBubble.BindSystemMessage(item.text, item.ts);
                _historyScroll?.Add(sysBubble);
                ScrollToBottom();
                return;
            }

            // 流式输出
            if (_currentAssistantBubble == null || !_currentAssistantBubble.IsStreaming)
            {
                _currentAssistantBubble = CreateBubble();
                _currentAssistantBubble.BindAssistantStreaming(item.runId, item.ts);
                _historyScroll?.Add(_currentAssistantBubble);
                _currentRunId = item.runId;
                _streamBuffer.Clear();
                _streamLineCount = 0;
            }

            if (!string.IsNullOrEmpty(item.text))
            {
                _streamBuffer.AppendLine(item.text);
                _streamLineCount++;
            }

            _currentAssistantBubble.UpdateStreamContent(_streamBuffer.ToString(), _streamLineCount);

            if (EditorApplication.timeSinceStartup - _lastScrollTime > 0.3)
            {
                _lastScrollTime = EditorApplication.timeSinceStartup;
                ScrollToBottom();
            }
        }

        private void OnStatusChanged(InstanceStatus status)
        {
            RefreshRunStatus();

            if (status != InstanceStatus.Running && _currentAssistantBubble != null && _currentAssistantBubble.IsStreaming)
            {
                var success = status == InstanceStatus.Completed;
                var finalContent = GetFinalContent(_streamBuffer.ToString());

                if (!string.IsNullOrEmpty(_runner.State.lastRunId))
                {
                    var outPath = CodexStore.GetOutPath(_runner.State.lastRunId);
                    if (System.IO.File.Exists(outPath))
                    {
                        try
                        {
                            var outContent = System.IO.File.ReadAllText(outPath, Encoding.UTF8);
                            if (!string.IsNullOrWhiteSpace(outContent))
                            {
                                finalContent = outContent;
                            }
                        }
                        catch { }
                    }
                }

                _currentAssistantBubble.CompleteStream(finalContent, success);
                _currentAssistantBubble = null;
                _streamBuffer.Clear();
                _streamLineCount = 0;
                ScrollToBottom();
            }
        }

        private ChatBubbleElement CreateBubble()
        {
            return new ChatBubbleElement(_bubbleTemplate);
        }

        private void ScrollToBottom()
        {
            if (_historyScroll == null || _historyScroll.contentContainer.childCount == 0) return;
            var last = _historyScroll.contentContainer[_historyScroll.contentContainer.childCount - 1];
            _historyScroll.schedule.Execute(() => _historyScroll.ScrollTo(last)).ExecuteLater(10);
        }

        private void RefreshRunStatus()
        {
            var state = _runner.State;
            var runId = state.activeRunId;
            var pid = state.activePid;
            var status = state.status;

            string statusClass = "status-idle";
            string headline = "Idle";
            Color statusColor = new Color(0.31f, 0.80f, 0.77f);

            switch (status)
            {
                case InstanceStatus.Running:
                    if (pid > 0 && CodexRunnerInstance.IsProcessAlive(pid))
                    {
                        statusClass = "status-running";
                        headline = "Running";
                        statusColor = new Color(0.15f, 0.87f, 0.51f);
                    }
                    else
                    {
                        statusClass = "status-warning";
                        headline = "Warning";
                        statusColor = new Color(0.99f, 0.79f, 0.34f);
                    }
                    break;
                case InstanceStatus.Completed:
                    statusClass = "status-completed";
                    headline = "Completed";
                    statusColor = new Color(0.15f, 0.87f, 0.51f);
                    break;
                case InstanceStatus.Error:
                    statusClass = "status-error";
                    headline = "Error";
                    statusColor = new Color(1f, 0.42f, 0.42f);
                    break;
            }

            _statusBar.style.backgroundColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.15f);
            _statusTextLabel.text = headline;
            _statusTextLabel.style.color = statusColor;

            var outputTime = _runner.LastOutputTime;
            var outputText = outputTime.HasValue ? outputTime.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture) : "--";
            _statusMetaLabel.text = string.IsNullOrEmpty(runId)
                ? "No active run"
                : $"runId {runId[..Math.Min(12, runId.Length)]}  pid {pid}  last output {outputText}";

            _killButton?.SetEnabled(status == InstanceStatus.Running);
            UpdateSendState();
        }

        private void UpdateSendState()
        {
            var isRunning = _runner.IsRunning || _runner.State.status == InstanceStatus.Running;
            var isCompiling = EditorApplication.isCompiling;
            var promptText = _promptField?.value ?? string.Empty;
            var canSend = !isRunning
                          && !isCompiling
                          && !string.IsNullOrWhiteSpace(promptText)
                          && _codexAvailable
                          && _hasGitRepo;

            _sendButton?.SetEnabled(canSend);
        }

        private void SetStatusMessage(string message, HelpBoxMessageType type)
        {
            if (_statusBox == null) return;

            if (string.IsNullOrEmpty(message))
            {
                _statusBox.style.display = DisplayStyle.None;
                return;
            }

            _statusBox.text = message;
            _statusBox.messageType = type;
            _statusBox.style.display = DisplayStyle.Flex;
        }

        private void Send()
        {
            SetStatusMessage(string.Empty, HelpBoxMessageType.Info);

            if (!_hasGitRepo)
            {
                SetStatusMessage("请先在项目根目录执行 git init", HelpBoxMessageType.Error);
                return;
            }

            if (!_codexAvailable)
            {
                SetStatusMessage("codex not found in PATH", HelpBoxMessageType.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_promptField.value))
            {
                SetStatusMessage("请输入 prompt", HelpBoxMessageType.Warning);
                return;
            }

            var prompt = _promptField.value;

            // 添加用户消息到历史
            var userItem = new HistoryItem
            {
                ts = CodexStore.GetIso8601Timestamp(),
                kind = "user",
                role = "user",
                text = prompt,
                source = "ui"
            };
            CodexStore.AppendInstanceHistory(_instanceId, userItem);

            // 创建用户气泡
            var userBubble = CreateBubble();
            userBubble.BindUserMessage(prompt, userItem.ts);
            _historyScroll?.Add(userBubble);
            ScrollToBottom();

            _promptField.value = string.Empty;

            // 更新实例活跃时间
            InstanceManager.Instance.SetLastActiveInstance(_instanceId);

            var resume = _runner.State.hasActiveThread;
            var model = _modelField.value;
            var effort = _effortField.value;

            // 保存设置
            _runner.State.model = model;
            _runner.State.effort = effort;
            _runner.State.debug = _debugToggle.value;
            _runner.SaveState();

            _runner.Execute(prompt, model, effort, resume,
                onComplete: _ =>
                {
                    RefreshRunStatus();
                    SetStatusMessage("运行完成", HelpBoxMessageType.Info);
                },
                onError: error =>
                {
                    SetStatusMessage(error, HelpBoxMessageType.Error);
                    if (_currentAssistantBubble != null && _currentAssistantBubble.IsStreaming)
                    {
                        _currentAssistantBubble.CompleteStream(error, false);
                        _currentAssistantBubble = null;
                    }
                });

            UpdateSendState();
        }

        private void NewTask()
        {
            if (!EditorUtility.DisplayDialog("新建任务",
                "确定要清空当前对话历史并开始新任务吗？",
                "确定", "取消"))
            {
                return;
            }

            _runner.ClearHistory();

            _currentAssistantBubble = null;
            _streamBuffer.Clear();
            _streamLineCount = 0;

            LoadConversation();
            RefreshRunStatus();
            SetStatusMessage("已开始新任务", HelpBoxMessageType.Info);
        }

        private void KillRun()
        {
            if (!EditorUtility.DisplayDialog("强杀进程", "确定要强制终止当前进程吗？", "强杀", "取消"))
            {
                return;
            }

            _runner.KillActiveProcessTree();
            RefreshRunStatus();
        }

        private static string GetItemKind(HistoryItem item)
        {
            if (!string.IsNullOrEmpty(item.kind)) return item.kind;
            if (!string.IsNullOrEmpty(item.role)) return item.role;
            return "event";
        }

        private static string GetFinalContent(string streamContent)
        {
            if (string.IsNullOrWhiteSpace(streamContent))
            {
                return "Task completed.";
            }

            var lines = streamContent.Split('\n');
            var meaningfulLines = new List<string>();

            for (int i = lines.Length - 1; i >= 0 && meaningfulLines.Count < 20; i--)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    meaningfulLines.Insert(0, line);
                }
            }

            return meaningfulLines.Count == 0 ? "Task completed." : string.Join("\n", meaningfulLines);
        }
    }
}
