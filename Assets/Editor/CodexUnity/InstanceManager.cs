using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodexUnity
{
    /// <summary>
    /// 多实例核心管理器 - 负责实例生命周期、Runner 管理、事件广播
    /// </summary>
    public class InstanceManager
    {
        #region 单例

        private static InstanceManager _instance;
        public static InstanceManager Instance => _instance ??= new InstanceManager();

        private InstanceManager()
        {
            Initialize();
        }

        #endregion

        #region 状态

        private InstanceRegistry _registry;
        private readonly Dictionary<string, CodexRunnerInstance> _runners = new();
        private bool _isInitialized;
        private bool _isPollingRegistered;

        #endregion

        #region 事件

        /// <summary>
        /// 实例状态变化事件 (instanceId, newStatus)
        /// </summary>
        public event Action<string, InstanceStatus> OnInstanceStatusChanged;

        /// <summary>
        /// 实例列表变化事件 (新增/删除)
        /// </summary>
        public event Action OnInstanceListChanged;

        /// <summary>
        /// 全局通知事件
        /// </summary>
        public event Action<ToastNotification> OnNotification;

        /// <summary>
        /// 历史项追加事件 (instanceId, item)
        /// </summary>
        public event Action<string, HistoryItem> OnHistoryItemAppended;

        #endregion

        #region 初始化

        private void Initialize()
        {
            if (_isInitialized) return;

            CodexStore.EnsureDirectoriesExist();

            // 检查是否需要迁移
            if (CodexStore.NeedsMigration())
            {
                Debug.Log("[CodexUnity] 检测到旧版数据，开始迁移...");
                CodexStore.MigrateLegacyData();
            }

            // 加载注册表
            _registry = CodexStore.LoadRegistry();

            // 为所有已注册实例创建 Runner
            foreach (var info in _registry.instances)
            {
                GetOrCreateRunner(info.id);
            }

            _isInitialized = true;
            Debug.Log($"[CodexUnity] InstanceManager 初始化完成，共 {_registry.instances.Length} 个实例");
        }

        /// <summary>
        /// Domain Reload 后恢复状态
        /// </summary>
        public void RestoreAllStates()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            foreach (var runner in _runners.Values)
            {
                runner.CheckAndRecoverPendingRun();
            }

            EnsurePollingLoop();
        }

        /// <summary>
        /// Domain Reload 前保存状态
        /// </summary>
        public void SaveAllStates()
        {
            foreach (var runner in _runners.Values)
            {
                runner.SaveState();
            }
            CodexStore.SaveRegistry(_registry);
        }

        #endregion

        #region 实例管理

        /// <summary>
        /// 获取所有实例信息
        /// </summary>
        public IReadOnlyList<InstanceInfo> GetAllInstances()
        {
            return _registry.instances;
        }

        /// <summary>
        /// 获取实例数量
        /// </summary>
        public int InstanceCount => _registry.instances.Length;

        /// <summary>
        /// 获取正在运行的实例数
        /// </summary>
        public int RunningCount => _runners.Values.Count(r => r.State.status == InstanceStatus.Running);

        /// <summary>
        /// 创建新实例
        /// </summary>
        public InstanceInfo CreateInstance(string name = null)
        {
            var instanceId = CodexStore.GenerateInstanceId();
            var timestamp = CodexStore.GetIso8601Timestamp();

            // 自动生成名称
            if (string.IsNullOrEmpty(name))
            {
                var nextNum = _registry.instances.Length + 1;
                name = $"Session #{nextNum}";
            }

            var info = new InstanceInfo
            {
                id = instanceId,
                name = name,
                createdAt = timestamp,
                lastActiveAt = timestamp
            };

            // 更新注册表
            var list = _registry.instances.ToList();
            list.Add(info);
            _registry.instances = list.ToArray();
            _registry.lastActiveInstanceId = instanceId;
            CodexStore.SaveRegistry(_registry);

            // 确保目录存在
            CodexStore.EnsureInstanceDirExists(instanceId);

            // 创建 Runner
            GetOrCreateRunner(instanceId);

            OnInstanceListChanged?.Invoke();
            Debug.Log($"[CodexUnity] 创建实例: {name} ({instanceId})");

            return info;
        }

        /// <summary>
        /// 删除实例
        /// </summary>
        public bool DeleteInstance(string instanceId)
        {
            var info = _registry.instances.FirstOrDefault(i => i.id == instanceId);
            if (info == null)
            {
                Debug.LogWarning($"[CodexUnity] 实例不存在: {instanceId}");
                return false;
            }

            // 如果正在运行，先停止
            if (_runners.TryGetValue(instanceId, out var runner))
            {
                if (runner.State.status == InstanceStatus.Running)
                {
                    runner.KillActiveProcessTree();
                }
                _runners.Remove(instanceId);
            }

            // 从注册表移除
            var list = _registry.instances.ToList();
            list.RemoveAll(i => i.id == instanceId);
            _registry.instances = list.ToArray();

            if (_registry.lastActiveInstanceId == instanceId)
            {
                _registry.lastActiveInstanceId = _registry.instances.FirstOrDefault()?.id;
            }
            CodexStore.SaveRegistry(_registry);

            // 删除实例目录
            CodexStore.DeleteInstanceDir(instanceId);

            OnInstanceListChanged?.Invoke();
            Debug.Log($"[CodexUnity] 删除实例: {info.name} ({instanceId})");

            return true;
        }

        /// <summary>
        /// 获取实例信息
        /// </summary>
        public InstanceInfo GetInstanceInfo(string instanceId)
        {
            return _registry.instances.FirstOrDefault(i => i.id == instanceId);
        }

        /// <summary>
        /// 更新实例信息
        /// </summary>
        public void UpdateInstanceInfo(InstanceInfo info)
        {
            var index = Array.FindIndex(_registry.instances, i => i.id == info.id);
            if (index >= 0)
            {
                _registry.instances[index] = info;
                CodexStore.SaveRegistry(_registry);
            }
        }

        /// <summary>
        /// 设置最后活跃实例
        /// </summary>
        public void SetLastActiveInstance(string instanceId)
        {
            _registry.lastActiveInstanceId = instanceId;

            var info = GetInstanceInfo(instanceId);
            if (info != null)
            {
                info.lastActiveAt = CodexStore.GetIso8601Timestamp();
                UpdateInstanceInfo(info);
            }
        }

        /// <summary>
        /// 获取最后活跃实例 ID
        /// </summary>
        public string GetLastActiveInstanceId()
        {
            return _registry.lastActiveInstanceId;
        }

        #endregion

        #region Runner 管理

        /// <summary>
        /// 获取或创建实例 Runner
        /// </summary>
        public CodexRunnerInstance GetOrCreateRunner(string instanceId)
        {
            if (_runners.TryGetValue(instanceId, out var existing))
            {
                return existing;
            }

            var runner = new CodexRunnerInstance(instanceId);
            runner.OnStatusChanged += status => HandleRunnerStatusChanged(instanceId, status);
            runner.OnHistoryItemAppended += item => HandleHistoryItemAppended(instanceId, item);
            runner.OnNotification += notification => OnNotification?.Invoke(notification);

            _runners[instanceId] = runner;
            EnsurePollingLoop();

            return runner;
        }

        /// <summary>
        /// 获取实例 Runner (如果存在)
        /// </summary>
        public CodexRunnerInstance GetRunner(string instanceId)
        {
            return _runners.GetValueOrDefault(instanceId);
        }

        private void HandleRunnerStatusChanged(string instanceId, InstanceStatus status)
        {
            OnInstanceStatusChanged?.Invoke(instanceId, status);

            // 发送通知
            var info = GetInstanceInfo(instanceId);
            var instanceName = info?.name ?? instanceId;

            if (status == InstanceStatus.Completed)
            {
                OnNotification?.Invoke(ToastNotification.Create(
                    instanceId, instanceName,
                    "任务完成",
                    NotificationType.Success
                ));
            }
            else if (status == InstanceStatus.Error)
            {
                OnNotification?.Invoke(ToastNotification.Create(
                    instanceId, instanceName,
                    "任务出错",
                    NotificationType.Error
                ));
            }
        }

        private void HandleHistoryItemAppended(string instanceId, HistoryItem item)
        {
            OnHistoryItemAppended?.Invoke(instanceId, item);
        }

        #endregion

        #region 轮询调度

        private void EnsurePollingLoop()
        {
            if (_isPollingRegistered) return;

            EditorApplication.update += OnEditorUpdate;
            _isPollingRegistered = true;
        }

        private void OnEditorUpdate()
        {
            foreach (var runner in _runners.Values)
            {
                runner.Update();
            }
        }

        /// <summary>
        /// 取消注册轮询 (仅在必要时调用)
        /// </summary>
        public void UnregisterPolling()
        {
            if (!_isPollingRegistered) return;

            EditorApplication.update -= OnEditorUpdate;
            _isPollingRegistered = false;
        }

        #endregion

        #region 全局操作

        /// <summary>
        /// 停止所有运行中的实例
        /// </summary>
        public void StopAllInstances()
        {
            foreach (var runner in _runners.Values)
            {
                if (runner.State.status == InstanceStatus.Running)
                {
                    runner.KillActiveProcessTree();
                }
            }
        }

        /// <summary>
        /// 检查 Codex CLI 是否可用
        /// </summary>
        public bool CheckCodexAvailable()
        {
            // 使用任意 Runner 检查 (静态方法)
            return CodexRunnerInstance.CheckCodexAvailableStatic();
        }

        #endregion
    }
}
