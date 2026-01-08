using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CodexUnity.Views
{
    /// <summary>
    /// 控制面板视图 - 显示所有实例列表，支持新建、打开、删除实例
    /// </summary>
    public class ControlPanelView : VisualElement
    {
        private readonly VisualElement _instanceListContainer;
        private readonly Button _newInstanceButton;
        private readonly Label _statusLabel;
        private readonly Dictionary<string, InstanceCardElement> _instanceCards = new();

        public event Action<string> OnInstanceSelected;
        public event Action OnCreateInstance;

        public ControlPanelView()
        {
            // 根容器样式
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;
            style.paddingTop = 12;
            style.paddingBottom = 12;
            style.paddingLeft = 12;
            style.paddingRight = 12;

            // 标题区域
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 16;

            var title = new Label("Codex 控制面板");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.91f, 0.91f, 0.93f);
            header.Add(title);

            _newInstanceButton = new Button(() => OnCreateInstance?.Invoke());
            _newInstanceButton.text = "+ 新建实例";
            _newInstanceButton.AddToClassList("primary-button");
            header.Add(_newInstanceButton);

            Add(header);

            // 状态栏
            _statusLabel = new Label();
            _statusLabel.style.fontSize = 12;
            _statusLabel.style.color = new Color(0.66f, 0.66f, 0.69f);
            _statusLabel.style.marginBottom = 12;
            Add(_statusLabel);

            // 实例列表容器
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _instanceListContainer = new VisualElement();
            _instanceListContainer.style.flexDirection = FlexDirection.Column;
            scrollView.Add(_instanceListContainer);

            Add(scrollView);

            // 订阅事件
            InstanceManager.Instance.OnInstanceListChanged += RefreshInstanceList;
            InstanceManager.Instance.OnInstanceStatusChanged += OnStatusChanged;

            // 初始加载
            RefreshInstanceList();
        }

        public void RefreshInstanceList()
        {
            _instanceListContainer.Clear();
            _instanceCards.Clear();

            var instances = InstanceManager.Instance.GetAllInstances();
            var runningCount = InstanceManager.Instance.RunningCount;

            _statusLabel.text = $"共 {instances.Count} 个实例，{runningCount} 个运行中";

            if (instances.Count == 0)
            {
                var emptyHint = new Label("暂无实例，点击上方按钮创建第一个实例");
                emptyHint.style.color = new Color(0.5f, 0.5f, 0.55f);
                emptyHint.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyHint.style.marginTop = 40;
                _instanceListContainer.Add(emptyHint);
                return;
            }

            foreach (var info in instances)
            {
                var card = new InstanceCardElement(info);
                card.OnSelect += () => OnInstanceSelected?.Invoke(info.id);
                card.OnDelete += () => DeleteInstance(info.id);
                card.OnStop += () => StopInstance(info.id);
                card.OnClear += () => ClearInstance(info.id);

                _instanceCards[info.id] = card;
                card.style.marginTop = _instanceListContainer.childCount > 1 ? 8 : 0;  // 模拟 gap
                _instanceListContainer.Add(card);

                // 更新当前状态
                var runner = InstanceManager.Instance.GetRunner(info.id);
                if (runner != null)
                {
                    card.UpdateStatus(runner.State.status);
                }
            }
        }

        private void OnStatusChanged(string instanceId, InstanceStatus status)
        {
            if (_instanceCards.TryGetValue(instanceId, out var card))
            {
                card.UpdateStatus(status);
            }

            // 更新状态栏
            var runningCount = InstanceManager.Instance.RunningCount;
            var totalCount = InstanceManager.Instance.InstanceCount;
            _statusLabel.text = $"共 {totalCount} 个实例，{runningCount} 个运行中";
        }

        private void DeleteInstance(string instanceId)
        {
            var info = InstanceManager.Instance.GetInstanceInfo(instanceId);
            var name = info?.name ?? instanceId;

            if (UnityEditor.EditorUtility.DisplayDialog(
                "删除实例",
                $"确定要删除实例 \"{name}\" 吗？\n\n此操作将删除该实例的所有历史记录，无法恢复。",
                "删除", "取消"))
            {
                InstanceManager.Instance.DeleteInstance(instanceId);
            }
        }

        private void StopInstance(string instanceId)
        {
            var runner = InstanceManager.Instance.GetRunner(instanceId);
            if (runner != null && runner.IsRunning)
            {
                runner.KillActiveProcessTree();
            }
        }

        private void ClearInstance(string instanceId)
        {
            var info = InstanceManager.Instance.GetInstanceInfo(instanceId);
            var name = info?.name ?? instanceId;

            if (UnityEditor.EditorUtility.DisplayDialog(
                "清空历史",
                $"确定要清空实例 \"{name}\" 的对话历史吗？",
                "确定", "取消"))
            {
                InstanceManager.Instance.ClearInstanceHistory(instanceId);
            }
        }

        public void Cleanup()
        {
            InstanceManager.Instance.OnInstanceListChanged -= RefreshInstanceList;
            InstanceManager.Instance.OnInstanceStatusChanged -= OnStatusChanged;
        }
    }

    /// <summary>
    /// 单个实例卡片 UI 元素
    /// </summary>
    public class InstanceCardElement : VisualElement
    {
        private readonly InstanceInfo _info;
        private readonly VisualElement _statusIndicator;
        private readonly Label _statusLabel;
        private readonly Button _stopButton;

        public event Action OnSelect;
        public event Action OnDelete;
        public event Action OnStop;
        public event Action OnClear;

        public InstanceCardElement(InstanceInfo info)
        {
            _info = info;

            // 卡片样式
            style.backgroundColor = new Color(0.165f, 0.165f, 0.196f);
            style.borderTopLeftRadius = 10;
            style.borderTopRightRadius = 10;
            style.borderBottomLeftRadius = 10;
            style.borderBottomRightRadius = 10;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftColor = new Color(1, 1, 1, 0.06f);
            style.borderRightColor = new Color(1, 1, 1, 0.06f);
            style.borderTopColor = new Color(1, 1, 1, 0.06f);
            style.borderBottomColor = new Color(1, 1, 1, 0.06f);
            style.paddingTop = 12;
            style.paddingBottom = 12;
            style.paddingLeft = 14;
            style.paddingRight = 14;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // 状态指示器
            _statusIndicator = new VisualElement();
            _statusIndicator.style.width = 10;
            _statusIndicator.style.height = 10;
            _statusIndicator.style.borderTopLeftRadius = 5;
            _statusIndicator.style.borderTopRightRadius = 5;
            _statusIndicator.style.borderBottomLeftRadius = 5;
            _statusIndicator.style.borderBottomRightRadius = 5;
            _statusIndicator.style.marginRight = 12;
            _statusIndicator.style.backgroundColor = new Color(0.42f, 0.42f, 0.47f); // 默认灰色
            Add(_statusIndicator);

            // 信息区域
            var infoArea = new VisualElement();
            infoArea.style.flexGrow = 1;
            infoArea.style.flexDirection = FlexDirection.Column;

            var nameLabel = new Label(info.name ?? $"Instance {info.id[..8]}");
            nameLabel.style.fontSize = 14;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = new Color(0.91f, 0.91f, 0.93f);
            infoArea.Add(nameLabel);

            _statusLabel = new Label("空置");
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.color = new Color(0.66f, 0.66f, 0.69f);
            _statusLabel.style.marginTop = 2;
            infoArea.Add(_statusLabel);

            Add(infoArea);

            // 操作按钮区域
            var buttonsArea = new VisualElement();
            buttonsArea.style.flexDirection = FlexDirection.Row;

            _stopButton = new Button(() => OnStop?.Invoke());
            _stopButton.text = "停止";
            _stopButton.AddToClassList("danger-button");
            _stopButton.style.display = DisplayStyle.None;
            _stopButton.style.marginRight = 6;  // 模拟 gap
            buttonsArea.Add(_stopButton);

            var openButton = new Button(() => OnSelect?.Invoke());
            openButton.text = "打开";
            openButton.AddToClassList("primary-button");
            openButton.style.marginRight = 6;
            buttonsArea.Add(openButton);

            var clearButton = new Button(() => OnClear?.Invoke());
            clearButton.text = "清空";
            clearButton.AddToClassList("ghost-button");
            clearButton.style.marginRight = 6;
            buttonsArea.Add(clearButton);

            var deleteButton = new Button(() => OnDelete?.Invoke());
            deleteButton.text = "删除";
            deleteButton.AddToClassList("ghost-button");
            buttonsArea.Add(deleteButton);

            Add(buttonsArea);
        }

        public void UpdateStatus(InstanceStatus status)
        {
            switch (status)
            {
                case InstanceStatus.Idle:
                    _statusIndicator.style.backgroundColor = new Color(0.42f, 0.42f, 0.47f); // 灰色
                    _statusLabel.text = "空置";
                    _stopButton.style.display = DisplayStyle.None;
                    break;

                case InstanceStatus.Running:
                    _statusIndicator.style.backgroundColor = new Color(0.15f, 0.87f, 0.51f); // 绿色
                    _statusLabel.text = "运行中...";
                    _stopButton.style.display = DisplayStyle.Flex;
                    break;

                case InstanceStatus.Completed:
                    _statusIndicator.style.backgroundColor = new Color(0.31f, 0.80f, 0.77f); // 青色
                    _statusLabel.text = "已完成";
                    _stopButton.style.display = DisplayStyle.None;
                    break;

                case InstanceStatus.Error:
                    _statusIndicator.style.backgroundColor = new Color(1f, 0.42f, 0.42f); // 红色
                    _statusLabel.text = "出错";
                    _stopButton.style.display = DisplayStyle.None;
                    break;
            }
        }
    }
}
