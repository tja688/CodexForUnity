using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CodexUnity
{
    /// <summary>
    /// 负责读写 state.json / history.jsonl / 路径管理
    /// </summary>
    public static class CodexStore
    {
        private static string _projectRoot;
        private static string _codexUnityDir;
        private static string _stateFilePath;
        private static string _historyFilePath;
        private static string _runsDir;

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

        public static string StateFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_stateFilePath))
                {
                    _stateFilePath = Path.Combine(CodexUnityDir, "state.json");
                }
                return _stateFilePath;
            }
        }

        public static string HistoryFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_historyFilePath))
                {
                    _historyFilePath = Path.Combine(CodexUnityDir, "history.jsonl");
                }
                return _historyFilePath;
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

        /// <summary>
        /// 确保目录存在
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
        }

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

        /// <summary>
        /// 获取元数据文件路径
        /// </summary>
        public static string GetMetaPath(string runId)
        {
            return Path.Combine(GetRunDir(runId), "meta.json");
        }

        /// <summary>
        /// 读取状态
        /// </summary>
        public static CodexState LoadState()
        {
            if (!File.Exists(StateFilePath))
            {
                return new CodexState
                {
                    hasActiveThread = false,
                    model = "gpt-5.1-codex-mini",
                    effort = "medium"
                };
            }

            try
            {
                var json = File.ReadAllText(StateFilePath);
                return JsonUtility.FromJson<CodexState>(json) ?? new CodexState
                {
                    hasActiveThread = false,
                    model = "gpt-5.1-codex-mini",
                    effort = "medium"
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 读取 state.json 失败: {e.Message}");
                return new CodexState
                {
                    hasActiveThread = false,
                    model = "gpt-5.1-codex-mini",
                    effort = "medium"
                };
            }
        }

        /// <summary>
        /// 保存状态
        /// </summary>
        public static void SaveState(CodexState state)
        {
            EnsureDirectoriesExist();
            var json = JsonUtility.ToJson(state, true);
            File.WriteAllText(StateFilePath, json);
        }

        /// <summary>
        /// 读取历史记录
        /// </summary>
        public static List<HistoryItem> LoadHistory()
        {
            var history = new List<HistoryItem>();

            if (!File.Exists(HistoryFilePath))
            {
                return history;
            }

            try
            {
                var lines = File.ReadAllLines(HistoryFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var item = JsonUtility.FromJson<HistoryItem>(line);
                    if (item != null)
                    {
                        history.Add(item);
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
        /// 追加历史记录
        /// </summary>
        public static void AppendHistory(HistoryItem item)
        {
            EnsureDirectoriesExist();
            var json = JsonUtility.ToJson(item);
            File.AppendAllText(HistoryFilePath, json + "\n");
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public static void ClearHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                File.Delete(HistoryFilePath);
            }
        }

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
                var json = File.ReadAllText(metaPath);
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
            File.WriteAllText(metaPath, json);
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
        /// 获取 ISO8601 时间戳
        /// </summary>
        public static string GetIso8601Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
    }
}
