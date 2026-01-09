using System;

namespace CodexUnity
{
    /// <summary>
    /// 实例注册表 - 存储所有实例的元数据
    /// </summary>
    [Serializable]
    public class InstanceRegistry
    {
        public InstanceInfo[] instances = Array.Empty<InstanceInfo>();
        public string lastActiveInstanceId;
    }

    /// <summary>
    /// 实例基础信息 - 用于大厅列表显示
    /// </summary>
    [Serializable]
    public class InstanceInfo
    {
        public string id;           // GUID
        public string name;         // 用户可自定义名称（默认：Session #1）
        public string colorTag;     // 用户自定义颜色标签 (可选)
        public string createdAt;    // ISO8601
        public string lastActiveAt; // ISO8601
    }

    /// <summary>
    /// 实例运行时状态 - 扩展 CodexState
    /// </summary>
    [Serializable]
    public class InstanceState
    {
        // 实例标识
        public string instanceId;
        public InstanceStatus status = InstanceStatus.Idle;

        // 运行状态 (从 CodexState 迁移)
        public bool debug;
        public string activeRunId;
        public int activePid;
        public long stdoutOffset;
        public long stderrOffset;
        public long eventsOffset;
        public string activeStatus;
        public bool hasActiveThread;
        public string lastRunId;
        public string lastRunOutPath;
        public string model;
        public string effort;
        public bool interruptedByReload;
        public long lastReloadTime;

        // Phase 2: 草稿保存
        public string draftPrompt;

        /// <summary>
        /// 从旧版 CodexState 迁移
        /// </summary>
        public static InstanceState FromLegacyState(CodexState legacy, string instanceId)
        {
            return new InstanceState
            {
                instanceId = instanceId,
                status = string.IsNullOrEmpty(legacy.activeRunId) ? InstanceStatus.Idle : InstanceStatus.Running,
                debug = legacy.debug,
                activeRunId = legacy.activeRunId,
                activePid = legacy.activePid,
                stdoutOffset = legacy.stdoutOffset,
                stderrOffset = legacy.stderrOffset,
                eventsOffset = legacy.eventsOffset,
                activeStatus = legacy.activeStatus,
                hasActiveThread = legacy.hasActiveThread,
                lastRunId = legacy.lastRunId,
                lastRunOutPath = legacy.lastRunOutPath,
                model = legacy.model,
                effort = legacy.effort,
                interruptedByReload = legacy.interruptedByReload,
                lastReloadTime = legacy.lastReloadTime
            };
        }

        /// <summary>
        /// 转换为旧版 CodexState (兼容性)
        /// </summary>
        public CodexState ToLegacyState()
        {
            return new CodexState
            {
                debug = debug,
                activeRunId = activeRunId,
                activePid = activePid,
                stdoutOffset = stdoutOffset,
                stderrOffset = stderrOffset,
                eventsOffset = eventsOffset,
                activeStatus = activeStatus,
                hasActiveThread = hasActiveThread,
                lastRunId = lastRunId,
                lastRunOutPath = lastRunOutPath,
                model = model,
                effort = effort,
                interruptedByReload = interruptedByReload,
                lastReloadTime = lastReloadTime
            };
        }

        /// <summary>
        /// 创建默认状态
        /// </summary>
        public static InstanceState CreateDefault(string instanceId)
        {
            return new InstanceState
            {
                instanceId = instanceId,
                status = InstanceStatus.Idle,
                model = "GPT-5.2-Codex",
                effort = "medium",
                activeStatus = "idle",
                debug = false
            };
        }

        /// <summary>
        /// 应用默认值
        /// </summary>
        public void ApplyDefaults()
        {
            if (string.IsNullOrEmpty(model))
                model = "GPT-5.2-Codex";
            if (string.IsNullOrEmpty(effort))
                effort = "medium";
            if (string.IsNullOrEmpty(activeStatus))
                activeStatus = "idle";
        }
    }

    /// <summary>
    /// 实例状态枚举
    /// </summary>
    public enum InstanceStatus
    {
        Idle,       // 灰色 - 空置
        Running,    // 绿色 - 工作中
        Completed,  // 蓝色 - 已完成
        Error       // 红色 - 错误
    }

    /// <summary>
    /// Toast 通知
    /// </summary>
    [Serializable]
    public class ToastNotification
    {
        public string id;           // 唯一ID
        public string instanceId;   // 来源实例
        public string instanceName; // 实例名称
        public string message;      // 通知内容
        public NotificationType type;
        public float lifetime = 4f; // 显示时长(秒)
        public string createdAt;    // ISO8601

        public static ToastNotification Create(string instanceId, string instanceName, string message, NotificationType type)
        {
            return new ToastNotification
            {
                id = Guid.NewGuid().ToString("N")[..8],
                instanceId = instanceId,
                instanceName = instanceName,
                message = message,
                type = type,
                createdAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")
            };
        }
    }

    /// <summary>
    /// 通知类型
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
