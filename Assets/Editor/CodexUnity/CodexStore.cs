using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CodexUnity
{
    /// <summary>
    /// 负责读写状态/历史/路径管理 - 支持多实例
    /// </summary>
    public static class CodexStore
    {
        private static string _projectRoot;
        private static string _codexUnityDir;
        private static string _runsDir;
        private static string _instancesDir;
        private static string _registryFilePath;

        // 旧版路径 (用于迁移检测)
        private static string _legacyStateFilePath;
        private static string _legacyHistoryFilePath;

        #region 路径属性

        public static string ProjectRoot
        {
            get
            {
                if (string.IsNullOrEmpty(_projectRoot))
                {
                    _projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                }
                return _projectRoot;
            }
        }

        public static string CodexUnityDir
        {
            get
            {
                if (string.IsNullOrEmpty(_codexUnityDir))
                {
                    _codexUnityDir = Path.Combine(ProjectRoot, "Library", "CodexUnity");
                }
                return _codexUnityDir;
            }
        }

        public static string RunsDir
        {
            get
            {
                if (string.IsNullOrEmpty(_runsDir))
                {
                    _runsDir = Path.Combine(CodexUnityDir, "runs");
                }
                return _runsDir;
            }
        }

        public static string InstancesDir
        {
            get
            {
                if (string.IsNullOrEmpty(_instancesDir))
                {
                    _instancesDir = Path.Combine(CodexUnityDir, "instances");
                }
                return _instancesDir;
            }
        }

        public static string RegistryFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_registryFilePath))
                {
                    _registryFilePath = Path.Combine(CodexUnityDir, "instances.json");
                }
                return _registryFilePath;
            }
        }

        // 旧版路径 (兼容性)
        public static string LegacyStateFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_legacyStateFilePath))
                {
                    _legacyStateFilePath = Path.Combine(CodexUnityDir, "state.json");
                }
                return _legacyStateFilePath;
            }
        }

        public static string LegacyHistoryFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_legacyHistoryFilePath))
                {
                    _legacyHistoryFilePath = Path.Combine(CodexUnityDir, "history.jsonl");
                }
                return _legacyHistoryFilePath;
            }
        }

        // 保持向后兼容的别名
        public static string StateFilePath => LegacyStateFilePath;
        public static string HistoryFilePath => LegacyHistoryFilePath;

        #endregion

        #region 目录管理

        /// <summary>
        /// 确保基础目录存在
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(CodexUnityDir))
            {
                Directory.CreateDirectory(CodexUnityDir);
            }
            if (!Directory.Exists(RunsDir))
            {
                Directory.CreateDirectory(RunsDir);
            }
            if (!Directory.Exists(InstancesDir))
            {
                Directory.CreateDirectory(InstancesDir);
            }
        }

        /// <summary>
        /// 获取实例目录路径
        /// </summary>
        public static string GetInstanceDir(string instanceId)
        {
            return Path.Combine(InstancesDir, instanceId);
        }

        /// <summary>
        /// 确保实例目录存在
        /// </summary>
        public static void EnsureInstanceDirExists(string instanceId)
        {
            var dir = GetInstanceDir(instanceId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// 获取实例状态文件路径
        /// </summary>
        public static string GetInstanceStatePath(string instanceId)
        {
            return Path.Combine(GetInstanceDir(instanceId), "state.json");
        }

        /// <summary>
        /// 获取实例历史文件路径
        /// </summary>
        public static string GetInstanceHistoryPath(string instanceId)
        {
            return Path.Combine(GetInstanceDir(instanceId), "history.jsonl");
        }

        #endregion

        #region Run 目录管理 (保持不变)

        /// <summary>
        /// 获取运行目录路径
        /// </summary>
        public static string GetRunDir(string runId)
        {
            return Path.Combine(RunsDir, runId);
        }

        /// <summary>
        /// 获取输出文件路径
        /// </summary>
        public static string GetOutPath(string runId)
        {
            return Path.Combine(GetRunDir(runId), "out.txt");
        }

        public static string GetStdoutPath(string runId)
        {
            return Path.Combine(GetRunDir(runId), "stdout.log");
        }

        public static string GetStderrPath(string runId)
        {
            return Path.Combine(GetRunDir(runId), "stderr.log");
        }

        public static string GetEventsPath(string runId)
        {
            return Path.Combine(GetRunDir(runId), "events.jsonl");
        }

        /// <summary>
        /// 获取元数据文件路径
        /// </summary>
        public static string GetMetaPath(string runId)
        {
            return Path.Combine(GetRunDir(runId), "meta.json");
        }

        #endregion

        #region 实例注册表

        /// <summary>
        /// 加载实例注册表
        /// </summary>
        public static InstanceRegistry LoadRegistry()
        {
            EnsureDirectoriesExist();

            if (!File.Exists(RegistryFilePath))
            {
                return new InstanceRegistry();
            }

            try
            {
                var json = File.ReadAllText(RegistryFilePath, Encoding.UTF8);
                var registry = JsonUtility.FromJson<InstanceRegistry>(json);
                return registry ?? new InstanceRegistry();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取 instances.json 失败: {e.Message}");
                return new InstanceRegistry();
            }
        }

        /// <summary>
        /// 保存实例注册表
        /// </summary>
        public static void SaveRegistry(InstanceRegistry registry)
        {
            EnsureDirectoriesExist();
            var json = JsonUtility.ToJson(registry, true);
            File.WriteAllText(RegistryFilePath, json, Encoding.UTF8);
        }

        #endregion

        #region 实例状态读写

        /// <summary>
        /// 加载实例状态
        /// </summary>
        public static InstanceState LoadInstanceState(string instanceId)
        {
            var path = GetInstanceStatePath(instanceId);
            if (!File.Exists(path))
            {
                return InstanceState.CreateDefault(instanceId);
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var state = JsonUtility.FromJson<InstanceState>(json);
                if (state != null)
                {
                    state.instanceId = instanceId; // 确保 ID 一致
                    state.ApplyDefaults();
                    return state;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取实例状态失败 ({instanceId}): {e.Message}");
            }

            return InstanceState.CreateDefault(instanceId);
        }

        /// <summary>
        /// 保存实例状态
        /// </summary>
        public static void SaveInstanceState(InstanceState state)
        {
            EnsureInstanceDirExists(state.instanceId);
            var path = GetInstanceStatePath(state.instanceId);
            var json = JsonUtility.ToJson(state, true);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        #endregion

        #region 实例历史读写

        /// <summary>
        /// 加载实例历史记录
        /// </summary>
        public static List<HistoryItem> LoadInstanceHistory(string instanceId)
        {
            var history = new List<HistoryItem>();
            var path = GetInstanceHistoryPath(instanceId);

            if (!File.Exists(path))
            {
                return history;
            }

            try
            {
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var item = JsonUtility.FromJson<HistoryItem>(line);
                    if (item != null)
                    {
                        history.Add(NormalizeHistoryItem(item));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取实例历史失败 ({instanceId}): {e.Message}");
            }

            return history;
        }

        /// <summary>
        /// 追加实例历史记录
        /// </summary>
        public static void AppendInstanceHistory(string instanceId, HistoryItem item)
        {
            EnsureInstanceDirExists(instanceId);
            var path = GetInstanceHistoryPath(instanceId);
            var json = JsonUtility.ToJson(item);
            File.AppendAllText(path, json + "\n", Encoding.UTF8);
        }

        /// <summary>
        /// 清空实例历史记录
        /// </summary>
        public static void ClearInstanceHistory(string instanceId)
        {
            var path = GetInstanceHistoryPath(instanceId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 删除实例目录
        /// </summary>
        public static void DeleteInstanceDir(string instanceId)
        {
            var dir = GetInstanceDir(instanceId);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        #endregion

        #region 旧版兼容 API (保持现有代码可用)

        /// <summary>
        /// 读取状态 - 兼容旧版 API
        /// </summary>
        public static CodexState LoadState()
        {
            if (!File.Exists(LegacyStateFilePath))
            {
                return CreateDefaultState();
            }

            try
            {
                var json = File.ReadAllText(LegacyStateFilePath, Encoding.UTF8);
                var state = JsonUtility.FromJson<CodexState>(json);
                return ApplyStateDefaults(state);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取 state.json 失败: {e.Message}");
                return CreateDefaultState();
            }
        }

        /// <summary>
        /// 保存状态 - 兼容旧版 API
        /// </summary>
        public static void SaveState(CodexState state)
        {
            EnsureDirectoriesExist();
            var json = JsonUtility.ToJson(state, true);
            File.WriteAllText(LegacyStateFilePath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 读取历史记录 - 兼容旧版 API
        /// </summary>
        public static List<HistoryItem> LoadHistory()
        {
            var history = new List<HistoryItem>();

            if (!File.Exists(LegacyHistoryFilePath))
            {
                return history;
            }

            try
            {
                var lines = File.ReadAllLines(LegacyHistoryFilePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var item = JsonUtility.FromJson<HistoryItem>(line);
                    if (item != null)
                    {
                        history.Add(NormalizeHistoryItem(item));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取 history.jsonl 失败: {e.Message}");
            }

            return history;
        }

        /// <summary>
        /// 追加历史记录 - 兼容旧版 API
        /// </summary>
        public static void AppendHistory(HistoryItem item)
        {
            EnsureDirectoriesExist();
            var json = JsonUtility.ToJson(item);
            File.AppendAllText(LegacyHistoryFilePath, json + "\n", Encoding.UTF8);
        }

        /// <summary>
        /// 清空历史记录 - 兼容旧版 API
        /// </summary>
        public static void ClearHistory()
        {
            if (File.Exists(LegacyHistoryFilePath))
            {
                File.Delete(LegacyHistoryFilePath);
            }
        }

        #endregion

        #region RunMeta 与其他工具方法

        /// <summary>
        /// 读取运行元数据
        /// </summary>
        public static RunMeta LoadRunMeta(string runId)
        {
            var metaPath = GetMetaPath(runId);
            if (!File.Exists(metaPath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(metaPath, Encoding.UTF8);
                return JsonUtility.FromJson<RunMeta>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取 meta.json 失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存运行元数据
        /// </summary>
        public static void SaveRunMeta(RunMeta meta)
        {
            var runDir = GetRunDir(meta.runId);
            if (!Directory.Exists(runDir))
            {
                Directory.CreateDirectory(runDir);
            }

            var metaPath = GetMetaPath(meta.runId);
            var json = JsonUtility.ToJson(meta, true);
            File.WriteAllText(metaPath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 检查 Git 仓库是否存在
        /// </summary>
        public static bool HasGitRepository()
        {
            var gitDir = Path.Combine(ProjectRoot, ".git");
            return Directory.Exists(gitDir);
        }

        /// <summary>
        /// 生成运行 ID
        /// </summary>
        public static string GenerateRunId()
        {
            return $"{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        /// <summary>
        /// 生成实例 ID
        /// </summary>
        public static string GenerateInstanceId()
        {
            return Guid.NewGuid().ToString("N")[..12];
        }

        /// <summary>
        /// 获取 ISO8601 时间戳
        /// </summary>
        public static string GetIso8601Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }

        public static List<string> ReadNewLines(string path, ref long offset, ref string partialLineBuffer)
        {
            var lines = new List<string>();
            if (!File.Exists(path))
            {
                return lines;
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (offset > stream.Length)
                {
                    offset = stream.Length;
                }
                stream.Seek(offset, SeekOrigin.Begin);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var chunk = reader.ReadToEnd();
                offset = stream.Position;

                if (string.IsNullOrEmpty(chunk))
                {
                    return lines;
                }

                var combined = (partialLineBuffer ?? string.Empty) + chunk;
                var segments = combined.Split('\n');
                var endsWithNewline = combined.EndsWith("\n", StringComparison.Ordinal);
                var lastIndex = segments.Length - 1;

                for (var i = 0; i < segments.Length; i++)
                {
                    if (i == lastIndex && !endsWithNewline)
                    {
                        partialLineBuffer = segments[i];
                        break;
                    }

                    var line = segments[i].TrimEnd('\r');
                    if (line.Length > 0)
                    {
                        lines.Add(line);
                    }
                }

                if (endsWithNewline)
                {
                    partialLineBuffer = string.Empty;
                }
            }
            catch (IOException)
            {
                return lines;
            }

            return lines;
        }

        #endregion

        #region 数据迁移

        /// <summary>
        /// 检查是否需要迁移旧版数据
        /// </summary>
        public static bool NeedsMigration()
        {
            // 如果已有 instances.json，则不需要迁移
            if (File.Exists(RegistryFilePath))
            {
                return false;
            }

            // 如果存在旧版 state.json 或 history.jsonl，需要迁移
            return File.Exists(LegacyStateFilePath) || File.Exists(LegacyHistoryFilePath);
        }

        /// <summary>
        /// 执行数据迁移
        /// </summary>
        public static string MigrateLegacyData()
        {
            EnsureDirectoriesExist();

            // 创建默认实例
            var instanceId = GenerateInstanceId();
            var instanceDir = GetInstanceDir(instanceId);
            Directory.CreateDirectory(instanceDir);

            // 迁移 state.json
            if (File.Exists(LegacyStateFilePath))
            {
                var destPath = GetInstanceStatePath(instanceId);
                try
                {
                    var legacyJson = File.ReadAllText(LegacyStateFilePath, Encoding.UTF8);
                    var legacyState = JsonUtility.FromJson<CodexState>(legacyJson);
                    var newState = InstanceState.FromLegacyState(legacyState ?? new CodexState(), instanceId);
                    var newJson = JsonUtility.ToJson(newState, true);
                    File.WriteAllText(destPath, newJson, Encoding.UTF8);

                    // 备份并删除旧文件
                    File.Move(LegacyStateFilePath, LegacyStateFilePath + ".bak");
                    Debug.Log($"[CodexUnity] 已迁移 state.json 到实例 {instanceId}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CodexUnity] 迁移 state.json 失败: {e.Message}");
                }
            }

            // 迁移 history.jsonl
            if (File.Exists(LegacyHistoryFilePath))
            {
                var destPath = GetInstanceHistoryPath(instanceId);
                try
                {
                    File.Copy(LegacyHistoryFilePath, destPath);
                    File.Move(LegacyHistoryFilePath, LegacyHistoryFilePath + ".bak");
                    Debug.Log($"[CodexUnity] 已迁移 history.jsonl 到实例 {instanceId}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CodexUnity] 迁移 history.jsonl 失败: {e.Message}");
                }
            }

            // 创建注册表
            var registry = new InstanceRegistry
            {
                instances = new[]
                {
                    new InstanceInfo
                    {
                        id = instanceId,
                        name = "Session #1 (Migrated)",
                        createdAt = GetIso8601Timestamp(),
                        lastActiveAt = GetIso8601Timestamp()
                    }
                },
                lastActiveInstanceId = instanceId
            };
            SaveRegistry(registry);

            Debug.Log($"[CodexUnity] 数据迁移完成，创建实例: {instanceId}");
            return instanceId;
        }

        #endregion

        #region 私有辅助方法

        private static CodexState CreateDefaultState()
        {
            return new CodexState
            {
                hasActiveThread = false,
                model = "gpt-5.1-codex-mini",
                effort = "medium",
                activeStatus = "idle",
                debug = false
            };
        }

        private static CodexState ApplyStateDefaults(CodexState state)
        {
            if (state == null)
            {
                return CreateDefaultState();
            }

            if (string.IsNullOrEmpty(state.model))
            {
                state.model = "gpt-5.1-codex-mini";
            }

            if (string.IsNullOrEmpty(state.effort))
            {
                state.effort = "medium";
            }

            if (string.IsNullOrEmpty(state.activeStatus))
            {
                state.activeStatus = "idle";
            }

            return state;
        }

        private static HistoryItem NormalizeHistoryItem(HistoryItem item)
        {
            if (item == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(item.kind) && !string.IsNullOrEmpty(item.role))
            {
                item.kind = item.role;
            }

            if (string.IsNullOrEmpty(item.role) && (item.kind == "user" || item.kind == "assistant"))
            {
                item.role = item.kind;
            }

            return item;
        }

        #endregion
    }
}
