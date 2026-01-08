using System;

namespace CodexUnity
{
    /// <summary>
    /// 会话状态
    /// </summary>
    [Serializable]
    public class CodexState
    {
        public bool hasActiveThread;
        public string lastRunId;
        public string lastRunOutPath;
        public string model;
        public string effort;
    }

    /// <summary>
    /// 历史记录条目
    /// </summary>
    [Serializable]
    public class HistoryItem
    {
        public string ts;       // ISO8601 时间戳
        public string role;     // "user" 或 "assistant"
        public string text;     // 消息内容
        public string runId;    // 运行 ID（可选）
    }

    /// <summary>
    /// 运行元数据
    /// </summary>
    [Serializable]
    public class RunMeta
    {
        public string runId;
        public string command;
        public string prompt;
        public string model;
        public string effort;
        public string time;         // ISO8601 时间戳
        public bool historyWritten; // 是否已写入历史
    }

    /// <summary>
    /// Reasoning Effort 选项
    /// </summary>
    public enum ReasoningEffort
    {
        minimal,
        low,
        medium,
        high,
        xhigh
    }
}
