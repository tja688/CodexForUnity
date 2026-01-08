using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CodexUnity
{
    /// <summary>
    /// 单个 Codex 实例的运行器（实例版本）
    /// 每个实例拥有独立的状态、历史、进程管理
    /// </summary>
    public class CodexRunnerInstance
    {
        #region 嵌套类型

        private class ExitInfo
        {
            public string runId;
            public int exitCode;
            public bool killed;
        }

        #endregion

        #region 实例状态

        private readonly string _instanceId;
        private InstanceState _state;
        private string _currentRunId;
        private bool _isRunning;
        private Process _activeProcess;
        private bool _killRequested;
        private ExitInfo _pendingExit;
        private int _seqCounter;
        private long _lastOutputTicks;
        private double _lastTailTime;
        private string _tailStdoutPartial = string.Empty;
        private string _tailStderrPartial = string.Empty;
        private string _tailEventsPartial = string.Empty;
        private bool _debugEnabled;
        private double _lastProcessCheckTime;
        private const double ProcessCheckInterval = 1.0;

        private readonly ConcurrentQueue<HistoryItem> _pendingItems = new();

        private Action<string> _onComplete;
        private Action<string> _onError;

        #endregion

        #region 公共属性

        public string InstanceId => _instanceId;
        public InstanceState State => _state;
        public bool IsRunning => _isRunning;
        public string CurrentRunId => _currentRunId;

        public int? ActivePid
        {
            get
            {
                if (_activeProcess != null)
                {
                    try
                    {
                        return _activeProcess.Id;
                    }
                    catch
                    {
                        return null;
                    }
                }
                return _state.activePid > 0 ? _state.activePid : null;
            }
        }

        public string ActiveRunId => !string.IsNullOrEmpty(_currentRunId) ? _currentRunId : _state.activeRunId;

        public DateTime? LastOutputTime
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastOutputTicks);
                return ticks <= 0 ? null : new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
            }
        }

        #endregion

        #region 事件

        public event Action<InstanceStatus> OnStatusChanged;
        public event Action<HistoryItem> OnHistoryItemAppended;
        public event Action<ToastNotification> OnNotification;

        #endregion

        #region 静态工具方法

        private static string _codexExePath;

        /// <summary>
        /// 静态方法：检查 codex 是否可用
        /// </summary>
        public static bool CheckCodexAvailableStatic()
        {
            var (available, _) = CheckCodexAvailableWithVersion();
            return available;
        }

        public static (bool available, string version) CheckCodexAvailableWithVersion()
        {
            try
            {
                var resolvedPath = ResolveCodexPath();
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    return (false, null);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = resolvedPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return (false, null);
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                return process.ExitCode == 0 ? (true, output.Trim()) : (false, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 检查 codex 失败: {e.Message}");
                return (false, null);
            }
        }

        #endregion

        #region 构造函数

        public CodexRunnerInstance(string instanceId)
        {
            _instanceId = instanceId;
            _state = CodexStore.LoadInstanceState(instanceId);
            _debugEnabled = _state.debug;
        }

        #endregion

        #region 状态管理

        public void SaveState()
        {
            CodexStore.SaveInstanceState(_state);
        }

        public void ReloadState()
        {
            _state = CodexStore.LoadInstanceState(_instanceId);
            _debugEnabled = _state.debug;
        }

        private void UpdateStatus(InstanceStatus status)
        {
            if (_state.status != status)
            {
                _state.status = status;
                SaveState();
                OnStatusChanged?.Invoke(status);
            }
        }

        #endregion

        #region 进程管理

        public static bool IsProcessAlive(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public void KillActiveProcessTree()
        {
            var pid = ActivePid;
            if (!pid.HasValue || pid.Value <= 0)
            {
                CleanupRunState("killed");
                return;
            }

            _killRequested = true;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/PID {pid.Value} /T /F",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit(5000);
                    DebugLog($"[CodexUnity] [{_instanceId}] taskkill output: {output}\n{error}");
                }
            }
            catch (Exception e)
            {
                DebugLog($"[CodexUnity] [{_instanceId}] taskkill 失败: {e.Message}");
            }

            CleanupRunState("killed");
            EnqueueSystemMessage($"已强杀进程 PID={pid.Value}", "warn");
        }

        private void CleanupRunState(string newStatus)
        {
            var runId = ActiveRunId;

            _state.activePid = 0;
            _state.activeStatus = newStatus;
            _state.status = newStatus == "error" ? InstanceStatus.Error :
                           newStatus == "completed" ? InstanceStatus.Completed : InstanceStatus.Idle;
            SaveState();

            if (!string.IsNullOrEmpty(runId))
            {
                var meta = CodexStore.LoadRunMeta(runId);
                if (meta != null)
                {
                    meta.killed = newStatus == "killed";
                    meta.finishedAt = CodexStore.GetIso8601Timestamp();
                    CodexStore.SaveRunMeta(meta);
                }
            }

            _isRunning = false;
            _activeProcess = null;
            _currentRunId = null;

            OnStatusChanged?.Invoke(_state.status);
        }

        #endregion

        #region 执行命令

        public void Execute(string prompt, string model, string effort, bool resume,
            Action<string> onComplete, Action<string> onError)
        {
            if (_isRunning)
            {
                onError?.Invoke("此实例已有任务正在运行");
                return;
            }

            if (!CodexStore.HasGitRepository())
            {
                onError?.Invoke("请先在项目根目录执行 git init（本插件要求 git repo）");
                return;
            }

            var resolvedPath = ResolveCodexPath();
            if (string.IsNullOrEmpty(resolvedPath))
            {
                onError?.Invoke("codex not found in PATH");
                return;
            }

            _debugEnabled = _state.debug;
            _onComplete = onComplete;
            _onError = onError;
            _seqCounter = 0;
            _killRequested = false;
            _tailStdoutPartial = string.Empty;
            _tailStderrPartial = string.Empty;
            _tailEventsPartial = string.Empty;
            _lastTailTime = 0;
            _lastProcessCheckTime = 0;

            _currentRunId = CodexStore.GenerateRunId();
            var runDir = CodexStore.GetRunDir(_currentRunId);
            var outPath = CodexStore.GetOutPath(_currentRunId);
            var stdoutPath = CodexStore.GetStdoutPath(_currentRunId);
            var stderrPath = CodexStore.GetStderrPath(_currentRunId);

            if (!Directory.Exists(runDir))
            {
                Directory.CreateDirectory(runDir);
            }

            File.WriteAllText(stdoutPath, "", Encoding.UTF8);
            File.WriteAllText(stderrPath, "", Encoding.UTF8);

            var codexArgs = BuildArguments(prompt, model, effort, resume, outPath);

            var meta = new RunMeta
            {
                runId = _currentRunId,
                command = $"codex {codexArgs}",
                prompt = prompt,
                model = model,
                effort = effort,
                time = CodexStore.GetIso8601Timestamp(),
                historyWritten = false,
                startedAt = CodexStore.GetIso8601Timestamp(),
                pid = 0,
                exitCode = 0,
                killed = false,
                stdoutOffset = 0,
                stderrOffset = 0,
                eventsOffset = 0
            };
            CodexStore.SaveRunMeta(meta);

            // 更新实例状态
            _state.hasActiveThread = true;
            _state.lastRunId = _currentRunId;
            _state.lastRunOutPath = outPath;
            _state.model = model;
            _state.effort = effort;
            _state.activeRunId = _currentRunId;
            _state.activePid = 0;
            _state.stdoutOffset = 0;
            _state.stderrOffset = 0;
            _state.eventsOffset = 0;
            _state.activeStatus = "running";
            _state.status = InstanceStatus.Running;
            SaveState();

            // 构建命令
            var cmdArgs = $"/c \"\"{resolvedPath}\" {codexArgs} > \"{stdoutPath}\" 2> \"{stderrPath}\"\"";

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArgs,
                WorkingDirectory = CodexStore.ProjectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false
            };

            try
            {
                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var capturedRunId = _currentRunId;

                process.Exited += (sender, args) =>
                {
                    try
                    {
                        var exitInfo = new ExitInfo
                        {
                            runId = capturedRunId,
                            exitCode = process.ExitCode,
                            killed = _killRequested
                        };
                        _pendingExit = exitInfo;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[CodexUnity] [{_instanceId}] 进程退出事件处理失败: {e.Message}");
                    }
                };

                if (!process.Start())
                {
                    _isRunning = false;
                    UpdateStatus(InstanceStatus.Error);
                    onError?.Invoke("无法启动 codex 进程");
                    return;
                }

                _activeProcess = process;
                _isRunning = true;
                _lastOutputTicks = 0;

                meta.pid = process.Id;
                meta.startedAt = CodexStore.GetIso8601Timestamp();
                CodexStore.SaveRunMeta(meta);

                _state.activePid = process.Id;
                _state.activeStatus = "running";
                SaveState();

                Debug.Log($"[CodexUnity] [{_instanceId}] 启动 codex 进程 PID={process.Id}, runId={_currentRunId}");
                OnStatusChanged?.Invoke(InstanceStatus.Running);
            }
            catch (Exception e)
            {
                _isRunning = false;
                UpdateStatus(InstanceStatus.Error);
                onError?.Invoke($"启动 codex 失败: {e.Message}");
            }
        }

        #endregion

        #region 轮询更新

        public void Update()
        {
            DrainPendingItems();
            HandlePendingExit();
            TailActiveRunFiles();
            CheckProcessStatus();
        }

        private void DrainPendingItems()
        {
            var appended = false;
            while (_pendingItems.TryDequeue(out var item))
            {
                CodexStore.AppendInstanceHistory(_instanceId, item);
                OnHistoryItemAppended?.Invoke(item);
                appended = true;
            }

            if (appended)
            {
                UpdateOffsetsForActiveRun();
            }
        }

        private void HandlePendingExit()
        {
            if (_pendingExit == null) return;

            var exitInfo = _pendingExit;
            _pendingExit = null;

            Debug.Log($"[CodexUnity] [{_instanceId}] 进程已退出: runId={exitInfo.runId}, exitCode={exitInfo.exitCode}, killed={exitInfo.killed}");

            _isRunning = false;
            _activeProcess = null;

            var meta = CodexStore.LoadRunMeta(exitInfo.runId);
            if (meta != null)
            {
                meta.exitCode = exitInfo.exitCode;
                meta.finishedAt = CodexStore.GetIso8601Timestamp();
                meta.killed = exitInfo.killed;
                CodexStore.SaveRunMeta(meta);
            }

            var isSuccess = exitInfo.exitCode == 0 || (exitInfo.exitCode == -1 && HasValidOutputContent(exitInfo.runId));
            var newStatus = exitInfo.killed ? "killed" : (isSuccess ? "completed" : "error");

            _state.activePid = 0;
            _state.activeStatus = newStatus;
            _state.status = newStatus == "error" ? InstanceStatus.Error :
                           newStatus == "completed" ? InstanceStatus.Completed : InstanceStatus.Idle;
            SaveState();

            TailActiveRunFilesForce(exitInfo.runId);
            AppendFinalSummaryIfNeeded(exitInfo.runId);

            OnStatusChanged?.Invoke(_state.status);

            if (isSuccess && !exitInfo.killed)
            {
                _onComplete?.Invoke("completed");
            }
            else if (exitInfo.killed)
            {
                _onError?.Invoke("运行已被终止");
            }
            else
            {
                _onError?.Invoke($"运行失败，退出码 {exitInfo.exitCode}");
            }
        }

        private void CheckProcessStatus()
        {
            if (EditorApplication.timeSinceStartup - _lastProcessCheckTime < ProcessCheckInterval)
                return;
            _lastProcessCheckTime = EditorApplication.timeSinceStartup;

            if (_state.activeStatus != "running" || _state.activePid <= 0)
                return;

            if (!IsProcessAlive(_state.activePid))
            {
                Debug.Log($"[CodexUnity] [{_instanceId}] 检测到进程 {_state.activePid} 已退出");
                var isSuccess = HasValidOutputContent(_state.activeRunId);
                _pendingExit = new ExitInfo
                {
                    runId = _state.activeRunId,
                    exitCode = isSuccess ? 0 : -1,
                    killed = false
                };
            }
        }

        private void TailActiveRunFiles()
        {
            if (string.IsNullOrEmpty(_state.activeRunId) || _state.activeStatus != "running")
                return;

            if (EditorApplication.timeSinceStartup - _lastTailTime < 0.2f)
                return;
            _lastTailTime = EditorApplication.timeSinceStartup;

            TailActiveRunFilesForce(_state.activeRunId);
        }

        private void TailActiveRunFilesForce(string runId)
        {
            var stdoutPath = CodexStore.GetStdoutPath(runId);
            var stderrPath = CodexStore.GetStderrPath(runId);
            var eventsPath = CodexStore.GetEventsPath(runId);

            var stdoutOffset = _state.stdoutOffset;
            var stderrOffset = _state.stderrOffset;
            var eventsOffset = _state.eventsOffset;

            var stdoutLines = CodexStore.ReadNewLines(stdoutPath, ref stdoutOffset, ref _tailStdoutPartial);
            var stderrLines = CodexStore.ReadNewLines(stderrPath, ref stderrOffset, ref _tailStderrPartial);
            var eventLines = CodexStore.ReadNewLines(eventsPath, ref eventsOffset, ref _tailEventsPartial);

            foreach (var line in stdoutLines)
                AppendRecoveredLine(runId, "event", "codex/stdout", "stdout", null, line);
            foreach (var line in stderrLines)
                AppendRecoveredLine(runId, "stderr", "codex/stderr", "stderr", "warn", line);
            foreach (var line in eventLines)
                AppendRecoveredLine(runId, "event", "codex/event", "event", null, line);

            if (stdoutLines.Count > 0 || stderrLines.Count > 0 || eventLines.Count > 0)
            {
                _state.stdoutOffset = stdoutOffset;
                _state.stderrOffset = stderrOffset;
                _state.eventsOffset = eventsOffset;
                SaveState();
                Interlocked.Exchange(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
            }
        }

        #endregion

        #region 历史记录

        private void EnqueueHistoryItem(string kind, string source, string title, string level, string text)
        {
            var item = new HistoryItem
            {
                ts = CodexStore.GetIso8601Timestamp(),
                kind = kind,
                role = kind == "user" || kind == "assistant" ? kind : null,
                title = title,
                level = level,
                text = text,
                raw = text,
                runId = _currentRunId ?? ActiveRunId,
                source = source,
                seq = Interlocked.Increment(ref _seqCounter)
            };
            _pendingItems.Enqueue(item);
            Interlocked.Exchange(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
        }

        private void EnqueueSystemMessage(string text, string level)
        {
            var item = new HistoryItem
            {
                ts = CodexStore.GetIso8601Timestamp(),
                kind = "system",
                title = "System",
                level = level,
                text = text,
                raw = text,
                runId = ActiveRunId,
                source = "ui"
            };
            _pendingItems.Enqueue(item);
        }

        private void AppendRecoveredLine(string runId, string kind, string source, string title, string level, string line)
        {
            var item = new HistoryItem
            {
                ts = CodexStore.GetIso8601Timestamp(),
                kind = kind,
                role = kind == "user" || kind == "assistant" ? kind : null,
                title = title,
                level = level,
                text = line,
                raw = line,
                runId = runId,
                source = source,
                seq = Interlocked.Increment(ref _seqCounter)
            };
            CodexStore.AppendInstanceHistory(_instanceId, item);
            OnHistoryItemAppended?.Invoke(item);
            Interlocked.Exchange(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
        }

        private void AppendSystemMessage(string runId, string level, string text)
        {
            var item = new HistoryItem
            {
                ts = CodexStore.GetIso8601Timestamp(),
                kind = "system",
                title = "System",
                level = level,
                text = text,
                raw = text,
                runId = runId,
                source = "ui"
            };
            CodexStore.AppendInstanceHistory(_instanceId, item);
            OnHistoryItemAppended?.Invoke(item);
        }

        private void UpdateOffsetsForActiveRun()
        {
            var runId = _currentRunId ?? _state.activeRunId;
            if (string.IsNullOrEmpty(runId)) return;

            _state.stdoutOffset = GetFileLengthSafe(CodexStore.GetStdoutPath(runId));
            _state.stderrOffset = GetFileLengthSafe(CodexStore.GetStderrPath(runId));
            _state.eventsOffset = GetFileLengthSafe(CodexStore.GetEventsPath(runId));
            SaveState();
        }

        private static long GetFileLengthSafe(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists ? info.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool HasValidOutputContent(string runId)
        {
            if (string.IsNullOrEmpty(runId)) return false;

            var outPath = CodexStore.GetOutPath(runId);
            if (File.Exists(outPath))
            {
                try
                {
                    var content = File.ReadAllText(outPath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        DebugLog($"[CodexUnity] [{_instanceId}] 发现有效输出内容 (out.txt): {content.Length} 字符");
                        return true;
                    }
                }
                catch { }
            }

            var stdoutPath = CodexStore.GetStdoutPath(runId);
            if (File.Exists(stdoutPath))
            {
                try
                {
                    var info = new FileInfo(stdoutPath);
                    if (info.Length > 50)
                    {
                        DebugLog($"[CodexUnity] [{_instanceId}] 发现有效输出内容 (stdout.txt): {info.Length} 字节");
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        private void AppendFinalSummaryIfNeeded(string runId)
        {
            var outPath = CodexStore.GetOutPath(runId);
            if (!File.Exists(outPath)) return;

            string output;
            try
            {
                output = File.ReadAllText(outPath, Encoding.UTF8);
            }
            catch
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(output)) return;

            var history = CodexStore.LoadInstanceHistory(_instanceId);
            foreach (var item in history)
            {
                if (item.runId == runId && (item.kind == "assistant" || item.role == "assistant"))
                    return;
            }

            var historyItem = new HistoryItem
            {
                ts = CodexStore.GetIso8601Timestamp(),
                kind = "assistant",
                role = "assistant",
                title = "Summary",
                text = output,
                raw = output,
                runId = runId,
                source = "codex/out"
            };

            CodexStore.AppendInstanceHistory(_instanceId, historyItem);
            OnHistoryItemAppended?.Invoke(historyItem);

            var meta = CodexStore.LoadRunMeta(runId);
            if (meta != null)
            {
                meta.historyWritten = true;
                CodexStore.SaveRunMeta(meta);
            }
        }

        #endregion

        #region Domain Reload 恢复

        public void CheckAndRecoverPendingRun()
        {
            ReloadState();

            var runId = !string.IsNullOrEmpty(_state.activeRunId) ? _state.activeRunId : _state.lastRunId;
            Debug.Log($"[CodexUnity] [{_instanceId}] CheckAndRecoverPendingRun: runId={runId}, status={_state.activeStatus}, pid={_state.activePid}");

            if (string.IsNullOrEmpty(runId))
            {
                Debug.Log($"[CodexUnity] [{_instanceId}] 没有找到需要恢复的运行");
                return;
            }

            _tailStdoutPartial = string.Empty;
            _tailStderrPartial = string.Empty;
            _tailEventsPartial = string.Empty;

            TailActiveRunFilesForce(runId);

            if (_state.activeStatus != "running")
            {
                AppendFinalSummaryIfNeeded(runId);
                return;
            }

            if (_state.activePid > 0 && IsProcessAlive(_state.activePid))
            {
                Debug.Log($"[CodexUnity] [{_instanceId}] 进程 {_state.activePid} 仍在运行，继续轮询输出");
                _isRunning = true;
                _state.status = InstanceStatus.Running;
                AppendSystemMessage(runId, "info", $"检测到进程 {_state.activePid} 仍在运行，继续监控...");
            }
            else
            {
                Debug.Log($"[CodexUnity] [{_instanceId}] 进程 {_state.activePid} 已结束");
                TailActiveRunFilesForce(runId);

                _state.activeStatus = "completed";
                _state.activePid = 0;
                _state.status = InstanceStatus.Completed;
                SaveState();

                AppendFinalSummaryIfNeeded(runId);
                AppendSystemMessage(runId, "info", "任务已完成（进程在 Domain Reload 期间结束）");
            }

            OnStatusChanged?.Invoke(_state.status);
        }

        #endregion

        #region 历史管理

        public List<HistoryItem> LoadHistory()
        {
            return CodexStore.LoadInstanceHistory(_instanceId);
        }

        public void ClearHistory()
        {
            CodexStore.ClearInstanceHistory(_instanceId);

            _state.activeRunId = null;
            _state.lastRunId = null;
            _state.lastRunOutPath = null;
            _state.stdoutOffset = 0;
            _state.stderrOffset = 0;
            _state.eventsOffset = 0;
            _state.activeStatus = "idle";
            _state.status = InstanceStatus.Idle;
            SaveState();
        }

        #endregion

        #region 构建参数

        private static string BuildArguments(string prompt, string model, string effort, bool resume, string outPath)
        {
            var sb = new StringBuilder();

            if (resume)
            {
                sb.Append("exec resume --last ");
                sb.Append("--dangerously-bypass-approvals-and-sandbox ");
                sb.Append($"--model \"{model}\" ");
                sb.Append($"-c model_reasoning_effort={effort} ");
                sb.Append($"\"{EscapePrompt(prompt)}\"");
            }
            else
            {
                sb.Append("exec ");
                sb.Append("--dangerously-bypass-approvals-and-sandbox ");
                sb.Append($"--model \"{model}\" ");
                sb.Append($"-c model_reasoning_effort={effort} ");
                sb.Append($"-o \"{outPath}\" ");
                sb.Append($"\"{EscapePrompt(prompt)}\"");
            }

            return sb.ToString();
        }

        private static string EscapePrompt(string prompt)
        {
            return prompt.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        #endregion

        #region Codex 路径解析

        private static readonly string[] CommonCodexPaths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "codex.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "codex"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pnpm", "codex.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pnpm", "codex"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yarn", "bin", "codex.cmd"),
            @"C:\ProgramData\chocolatey\bin\codex.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "codex.cmd"),
        };

        private static string ResolveCodexPath()
        {
            if (!string.IsNullOrEmpty(_codexExePath) && File.Exists(_codexExePath))
                return _codexExePath;

            foreach (var path in CommonCodexPaths)
            {
                if (File.Exists(path))
                {
                    _codexExePath = path;
                    return _codexExePath;
                }
            }

            try
            {
                var extendedPath = BuildExtendedPath();
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where codex",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                psi.EnvironmentVariables["PATH"] = extendedPath;

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            _codexExePath = lines[0].Trim();
                            return _codexExePath;
                        }
                    }
                }
            }
            catch { }

            try
            {
                var npmPath = FindNpmPath();
                if (!string.IsNullOrEmpty(npmPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = "root -g",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit(10000);
                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            var npmBinDir = Directory.GetParent(output)?.FullName;
                            if (!string.IsNullOrEmpty(npmBinDir))
                            {
                                var codexCmd = Path.Combine(npmBinDir, "codex.cmd");
                                if (File.Exists(codexCmd))
                                {
                                    _codexExePath = codexCmd;
                                    return _codexExePath;
                                }

                                var codexPlain = Path.Combine(npmBinDir, "codex");
                                if (File.Exists(codexPlain))
                                {
                                    _codexExePath = codexPlain;
                                    return _codexExePath;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private static string BuildExtendedPath()
        {
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var additionalPaths = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pnpm"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yarn", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims"),
                @"C:\Program Files\nodejs",
                @"C:\Program Files (x86)\nodejs",
            };

            var nvmHome = Environment.GetEnvironmentVariable("NVM_HOME");
            if (!string.IsNullOrEmpty(nvmHome))
            {
                additionalPaths.Add(nvmHome);
                var nvmSymlink = Environment.GetEnvironmentVariable("NVM_SYMLINK");
                if (!string.IsNullOrEmpty(nvmSymlink))
                    additionalPaths.Add(nvmSymlink);
            }

            if (Directory.Exists(@"C:\nvm4w\nodejs"))
                additionalPaths.Add(@"C:\nvm4w\nodejs");

            var allPaths = new HashSet<string>(currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries));
            foreach (var p in additionalPaths)
            {
                if (Directory.Exists(p))
                    allPaths.Add(p);
            }

            return string.Join(";", allPaths);
        }

        private static string FindNpmPath()
        {
            var candidates = new[]
            {
                @"C:\Program Files\nodejs\npm.cmd",
                @"C:\nvm4w\nodejs\npm.cmd",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "npm.cmd"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where npm",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                            return lines[0].Trim();
                    }
                }
            }
            catch { }

            return null;
        }

        #endregion

        #region 调试

        private void DebugLog(string message)
        {
            if (_debugEnabled)
                Debug.Log(message);
        }

        #endregion
    }
}
