using System;
using System.Collections.Generic;
using CodexUnity.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CodexUnity
{
    /// <summary>
    /// Codex Unity 主窗口 - 视图路由器
    /// 管理 ControlPanel（大厅）和 InstanceDetailView（实例详情）之间的导航
    /// </summary>
    public class CodexWindow : EditorWindow
    {
        private enum ViewState
        {
            ControlPanel,
            InstanceDetail
        }

        // EditorPrefs keys for Domain Reload persistence
        private const string PrefKeyCurrentView = "CodexUnity_CurrentView";
        private const string PrefKeyCurrentInstanceId = "CodexUnity_CurrentInstanceId";

        private ViewState _currentView = ViewState.ControlPanel;
        private string _currentInstanceId;

        private VisualElement _rootContainer;
        private ControlPanelView _controlPanel;
        private NotificationOverlay _notificationOverlay;
        private readonly Dictionary<string, InstanceDetailView> _detailViewCache = new();

        private bool _wasCompiling;

        [MenuItem("Tools/Codex")]
        public static void ShowWindow()
        {
            var window = GetWindow<CodexWindow>("Codex");
            window.minSize = new Vector2(380, 500);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            // 保存视图状态
            EditorPrefs.SetInt(PrefKeyCurrentView, (int)_currentView);
            EditorPrefs.SetString(PrefKeyCurrentInstanceId, _currentInstanceId ?? "");

            EditorApplication.update -= OnEditorUpdate;
            Cleanup();
        }

        private void OnEditorUpdate()
        {
            var isCompiling = EditorApplication.isCompiling;
            if (_wasCompiling && !isCompiling)
            {
                // 编译完成后刷新
                Repaint();
            }
            _wasCompiling = isCompiling;
        }


        public void CreateGUI()
        {
            rootVisualElement.Clear();
            _detailViewCache.Clear();

            // 加载样式
            var windowStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CodexUnity/UI/CodexWindow.uss");
            var bubbleStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CodexUnity/UI/ChatBubble.uss");

            if (windowStyle != null)
            {
                rootVisualElement.styleSheets.Add(windowStyle);
            }
            if (bubbleStyle != null)
            {
                rootVisualElement.styleSheets.Add(bubbleStyle);
            }

            // 根容器
            _rootContainer = new VisualElement();
            _rootContainer.style.flexGrow = 1;
            _rootContainer.style.backgroundColor = new Color(0.102f, 0.102f, 0.122f);
            rootVisualElement.Add(_rootContainer);

            // 创建控制面板
            _controlPanel = new ControlPanelView();
            _controlPanel.OnInstanceSelected += NavigateToInstance;
            _controlPanel.OnCreateInstance += CreateNewInstance;
            _rootContainer.Add(_controlPanel);

            // 创建通知浮层
            _notificationOverlay = new NotificationOverlay();
            rootVisualElement.Add(_notificationOverlay);

            // 恢复视图状态 (Domain Reload 后)
            var savedView = (ViewState)EditorPrefs.GetInt(PrefKeyCurrentView, 0);
            var savedInstanceId = EditorPrefs.GetString(PrefKeyCurrentInstanceId, "");

            if (savedView == ViewState.InstanceDetail

                && !string.IsNullOrEmpty(savedInstanceId)

                && InstanceManager.Instance.GetInstanceInfo(savedInstanceId) != null)
            {
                // 恢复到之前的实例详情视图
                NavigateToInstance(savedInstanceId);
            }
            else
            {
                _currentView = ViewState.ControlPanel;
            }
        }

        private void Cleanup()
        {
            _controlPanel?.Cleanup();
            _notificationOverlay?.Cleanup();

            foreach (var view in _detailViewCache.Values)
            {
                view.Cleanup();
            }
            _detailViewCache.Clear();
        }

        private void CreateNewInstance()
        {
            var instance = InstanceManager.Instance.CreateInstance();
            NavigateToInstance(instance.id);
        }

        private void NavigateToInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;

            // 隐藏控制面板
            _controlPanel.style.display = DisplayStyle.None;

            // 隐藏所有已缓存的详情视图
            foreach (var view in _detailViewCache.Values)
            {
                view.style.display = DisplayStyle.None;
            }

            // 获取或创建详情视图
            if (!_detailViewCache.TryGetValue(instanceId, out var detailView))
            {
                detailView = new InstanceDetailView(instanceId);
                detailView.OnBackRequested += NavigateToControlPanel;
                _detailViewCache[instanceId] = detailView;
                _rootContainer.Add(detailView);
            }

            detailView.style.display = DisplayStyle.Flex;
            _currentInstanceId = instanceId;
            _currentView = ViewState.InstanceDetail;

            // 更新最后活跃实例
            InstanceManager.Instance.SetLastActiveInstance(instanceId);
        }

        private void NavigateToControlPanel()
        {
            // 隐藏所有详情视图
            foreach (var view in _detailViewCache.Values)
            {
                view.style.display = DisplayStyle.None;
            }

            // 显示控制面板
            _controlPanel.style.display = DisplayStyle.Flex;
            _controlPanel.RefreshInstanceList();

            _currentInstanceId = null;
            _currentView = ViewState.ControlPanel;
        }

        private void OnFocus()
        {
            // 刷新当前视图
            if (_currentView == ViewState.ControlPanel)
            {
                _controlPanel?.RefreshInstanceList();
            }
        }
    }
}
