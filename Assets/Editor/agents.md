# agents.md — CodexUnity 升级版（UI Toolkit + 事件流 + 进程管理 + Debug）

## 0. 目标与硬约束

### 目标

1. 把当前 IMGUI 窗口改成 **UI Toolkit（UXML + USS）**，UI 更现代、层级清晰、按钮/配色好看。
2. 把输出从“等 out.txt 生成后一次性读完”升级为**结构化事件流 + 增量输出**，每条输出/事件/用户指令都落盘成**消息气泡**，支持滚动自动顶起，**Domain Reload 后自动恢复历史并继续追尾最新输出**。
3. 增加**进程存活检测**、**强杀按钮**、异常警告（例如：进程已不存在但仍显示 Running）。
4. 增加 **Debug 模式**，打开后会输出详细执行与采集日志到 Console（以及可选写入 run 日志文件）。

### 你现有实现的关键点（作为改造依据）

* 当前窗口是 IMGUI，历史用 `history.jsonl` 拼成一大段 TextArea 显示 
* Send 只在点击时写入一条 user history，然后启动 codex，靠轮询 `out.txt` 文件出现内容后一次性写入 assistant history  
* Runner 目前用 `cmd.exe /c codex ...` 启动，记录日志里显示 PID，但这个 PID 实际是 cmd 的 PID，容易出现“查不到进程”的错觉 
* BuildArguments 里启用了最高风险参数 `--dangerously-bypass-approvals-and-sandbox --sandbox danger-full-access`，并输出到 `-o out.txt` 
* Domain Reload 恢复逻辑目前仅仅是：如果 meta 未写入历史且 out.txt 有内容就补写一次 
* `CodexStore` 管理 Library/CodexUnity/state.json + history.jsonl + runs 目录 

---

## 1) 文件结构与新增资源

### 1.1 新增/调整目录

```
Assets/Editor/CodexUnity/
  UI/
    CodexWindow.uxml
    CodexWindow.uss
    ChatBubble.uxml
    ChatBubble.uss
  RuntimeData/ (可选，不建议；仍然写 Library/CodexUnity)
  CodexWindow.cs        (改为 UI Toolkit)
  CodexRunner.cs        (升级：进程句柄 + 流采集 + 文件追尾)
  CodexStore.cs         (升级：消息结构 + 增量读取offset + run日志路径)
  CodexBootstrap.cs     (升级：恢复“正在运行/未完成”的 run）
  Models.cs             (升级：状态/消息/事件模型)
```

### 1.2 运行目录结构（每个 run）

继续用 `Library/CodexUnity/runs/{runId}/`，但新增：

```
out.txt              // 最终整段输出（保留你原本逻辑）
events.jsonl         // 如果能开 --json：每行一个事件
stdout.log           // 原生标准输出（逐行追加）
stderr.log           // 原生标准错误（逐行追加）
runmeta.json         // 扩展后的 meta（pid、offset 等）
```

> 你现在 `GetOutPath()` 只有 out.txt ，需要扩展同类方法。

---

## 2) 模型与落盘格式升级（最关键：可恢复 + 可追尾）

### 2.1 扩展 `HistoryItem` 变成“气泡消息”

在 `Models.cs` 的 `HistoryItem` 增加字段（保持兼容：旧 jsonl 仍能反序列化，缺字段默认为 null） ：

新增字段建议：

* `public string kind;`  // "user" | "assistant" | "event" | "stderr" | "system"
* `public string title;` // 气泡标题（可选：event type / warning 等）
* `public string level;` // "info" | "warn" | "error"（用于颜色/图标）
* `public int seq;`      // 单 run 内递增序号（可选）
* `public string source;`// "codex/stdout" "codex/stderr" "codex/event" "ui"
* `public string raw;`   // 原始行（比如 jsonline 原文），展示可折叠
* `public string runId;` // 你已有

兼容规则：

* 如果旧记录只有 `role`，则映射：`kind = role`（"user"/"assistant"）。

### 2.2 扩展 `CodexState`：记录 Debug、以及“正在跑的 pid/run”

在 `CodexState` 增加：

* `public bool debug;`
* `public string activeRunId;`   // 当前认为正在跑的 run
* `public int activePid;`        // 当前认为正在跑的 pid（**必须是 codex pid，而不是 cmd pid**）
* `public long stdoutOffset;`    // 上次读到 stdout.log 的字节位置
* `public long stderrOffset;`
* `public long eventsOffset;`
* `public string activeStatus;`  // "running" | "completed" | "killed" | "error" | "unknown"

你现在 state 只存 `hasActiveThread/lastRunId/lastRunOutPath/model/effort` ，不够支撑“追尾 + 恢复”。

### 2.3 扩展 `RunMeta`：记录 pid、启动时间、退出码、最后输出时间等

在 `RunMeta` 增加：

* `public int pid;`
* `public string startedAt;`
* `public string finishedAt;`
* `public int exitCode;`
* `public bool killed;`
* `public long stdoutOffset;`
* `public long stderrOffset;`
* `public long eventsOffset;`

你当前 meta 只有 command/prompt/model/effort/time/historyWritten ，恢复能力弱。

---

## 3) CodexRunner：从“轮询 out.txt”升级为“进程句柄 + 流采集 + 文件追尾”

> 你现在的 Runner：
>
> * 启动：`cmd.exe /c codex ...` 
> * 轮询：只看 out.txt 是否有内容，有就一次性读完写入历史 
> * 超时：10 分钟就报错 
>   这导致你**看不到过程**，也不容易判断“是卡住还是已经退出”。

### 3.1 进程启动策略（Windows-only，追求 PID 准确）

**不要再把 cmd.exe 当作主进程 pid**。解决方法：

**方案 A（推荐）**：先用 `where codex` 找到绝对路径，然后 `ProcessStartInfo.FileName = codexExePath` 直接启动。

* `CheckCodexAvailable()` 现在用 `cmd /c codex --version` 
* 改为：`cmd /c where codex` 获取路径（第一行），再用这个路径直接跑 `--version`
* `Execute()` 时用 `FileName = codexExePath`，这样 `process.Id` 就是 codex 的 PID，后续可查可杀。

**方案 B（备选）**：仍用 cmd，但通过 `taskkill /T` 强杀进程树；PID 仍不稳定但至少可杀。
（A 更适合你要的“检查存活/卡住”。）

### 3.2 输出采集策略（必须能“逐条展示”）

启动时设置：

* `RedirectStandardOutput = true`
* `RedirectStandardError = true`
* `UseShellExecute = false`
* `CreateNoWindow = true`

然后：

* 起两个后台 Task/Thread：逐行 ReadLine
* 每读到一行：

  1. 追加写入 `stdout.log`/`stderr.log`
  2. **解析成事件消息气泡**（见 3.3）
  3. enqueue 到线程安全队列（`ConcurrentQueue<HistoryItem>`）
* 主线程在 `EditorApplication.update` 中 drain 队列，写入 `history.jsonl` 并刷新 UI

> 这样你能实时看到输出，并且即便 Unity 卡顿，文件也在增长。
> Domain Reload 时后台线程会死，但文件已落盘；恢复时走 “追尾读取” 机制（3.4）。

### 3.3 “结构化事件流”怎么做（不依赖官方插件能力）

这里分两档：

**档 1（优先实现，最稳）**：不强依赖 codex 的 `--json` 事件格式

* 将 stdout 每行当作文本消息：`kind="event" source="codex/stdout"`
* stderr 每行：`kind="stderr" level="warn|error" source="codex/stderr"`
* UI 上可折叠“原文 raw”，主展示 text 做裁剪/换行

**档 2（可选增强）**：如果 codex 支持 `--json` 或类似开关

* BuildArguments 里增加 `--json`（或等价参数），让 stdout 输出 JSONL
* 解析每行 JSON：尝试提取 `type` / `message` / `phase` 等字段，映射到：

  * `title = type`
  * `text = message（或简化摘要）`
  * `raw = 原始 jsonline`
* 解析失败就 fallback 当普通文本

### 3.4 Domain Reload 恢复：从“补写 out.txt”升级为“追尾日志文件”

你现在的恢复逻辑只会在 out.txt 已有完整内容时补写一次 assistant 。升级为：

恢复步骤（在 `CodexBootstrap.Initialize` 调用，仍然保留你现在入口 ）：

1. 读取 state：如果 `activeStatus == running` 且 `activeRunId != null`
2. 检查 pid 是否存活：

   * 存活：继续 “追尾读取 stdout.log/stderr.log/events.jsonl” 并不断写入历史
   * 不存活：标记 `activeStatus="unknown"`，并追尾读取到文件末尾后给出 system warn 气泡：“进程已结束/丢失，但日志已恢复到最新”
3. out.txt 若已生成：作为 “最终总结气泡” 补写 assistant（可选：只在 history 里不存在该 runId 的 assistant 总结时写）

追尾读取实现要点：

* 用 offset 读文件新增部分（字节偏移），不要 `ReadAllText`。
* 处理半行：如果最后一行没换行符，缓存到下一次拼接。

---

## 4) 进程存活检测与强杀（UI + Runner API）

### 4.1 Runner 暴露的 API（给 UI 用）

在 `CodexRunner` 增加：

* `public static int? ActivePid {get;}`
* `public static string ActiveRunId {get;}`
* `public static bool IsProcessAlive(int pid)`
* `public static void KillActiveProcessTree()`（Windows：`taskkill /PID {pid} /T /F`）
* `public static string GetRunDir(string runId)`（或复用 Store）

### 4.2 UI 行为

窗口顶部常驻 “Run 状态条”：

* 绿色：Idle / Completed
* 蓝色：Running（显示 PID、runId、最后输出时间）
* 黄色：Warning（pid 不存在但 state 认为 running；或长时间无输出增长）
* 红色：Error（stderr 连续输出；或 exitCode != 0）

按钮：

* `Kill`：弹窗确认（默认危险操作），然后强杀；写入一条 system 气泡 `killed=true`
* `Open Run Folder`：打开当前 run 目录（Windows Explorer）
* `Copy Command`：复制 meta.command

> 你现在 Window 只有 Send/New Task ，需要扩展按钮区。

---

## 5) Debug 模式（控制台详细日志 + 可选落盘）

### 5.1 State 存储

`CodexState.debug` 存在 state.json 里，UI 上是 Toggle。
读取 state 的逻辑你已经有：RefreshData 会加载 state 并恢复 model/effort 

### 5.2 Debug 输出点位（建议至少这些）

当 debug 开启时：

* 启动前：打印 codexExePath、工作目录、完整 args、run 路径
* 启动后：打印 PID、采集线程启动成功
* 每次追尾读取：打印新增字节数、解析出的事件 type（可限频）
* Kill：打印 taskkill 命令与结果
* 恢复：打印恢复决策（pid alive? offsets?）

---

## 6) UI Toolkit 重做（UXML + USS + 消息气泡）

### 6.1 CodexWindow 改造方向

* `CodexWindow : EditorWindow`
* 用 `CreateGUI()` 构建 UI（不要再用 OnGUI）
* 加载 `CodexWindow.uxml` + `CodexWindow.uss`
* 核心控件：

  * 顶部：环境状态（git/codex version）+ 风险条（红）
  * 中部：`ScrollView` 聊天区（气泡列表）
  * 底部：输入区（模型 dropdown、effort dropdown、textarea、Send、New Task、Kill、Debug toggle）

### 6.2 气泡组件（ChatBubble.uxml + .uss）

* 每条 `HistoryItem` 渲染一个 `ChatBubbleElement : VisualElement`
* 样式类：

  * `.bubble.user`（右侧对齐、主色）
  * `.bubble.assistant`（左侧对齐、中性色）
  * `.bubble.event`（左侧、较淡）
  * `.bubble.stderr`（左侧、红/橙）
  * `.bubble.system`（居中或左侧、灰）
* 气泡结构：

  * 顶部：title + timestamp
  * 中部：正文 text（支持换行）
  * 底部：raw 折叠区（可选，“显示原文”按钮）

### 6.3 自动顶起 + 历史恢复

* 发送/收到新消息后：把新气泡 add 到 ScrollView.contentContainer
* `scrollView.ScrollTo(lastBubble)`（用 schedule 延迟一帧，确保布局完成）
* OnEnable/CreateGUI 时：

  * LoadHistory：遍历 history.jsonl，逐条创建气泡（不要拼成一个大字符串 —— 你现在是 BuildHistoryDisplay 拼接 ）
  * 然后如果 state 表示 running：启动追尾

---

## 7) 具体开发任务拆解（按顺序做，避免返工）

### Task A — 扩展 Store 路径与读取/追加能力

1. `CodexStore` 增加：

   * `GetStdoutPath(runId)`, `GetStderrPath(runId)`, `GetEventsPath(runId)`（仿照 GetOutPath ）
   * `AppendHistory(HistoryItem item)` 保留（你已有 ），但要确保写入 UTF-8（可显式 Encoding.UTF8）
2. 增加 “追尾读取”工具：

   * `ReadNewLines(path, ref offset, ref partialLineBuffer) -> List<string>`
   * 读文件时用 FileStream Seek(offset) + StreamReader(UTF8)
   * 返回完整行列表并更新 offset

### Task B — Models 扩展（兼容旧数据）

按 2.1~2.3 修改 `Models.cs` 

### Task C — Runner 改造（核心）

1. `CheckCodexAvailable` 改为：`where codex` -> 绝对路径缓存（可存到 state 或静态字段）
2. `Execute`：

   * 创建 runDir
   * 写 meta（新增 pid/startedAt 等）
   * 更新 state：activeRunId/activePid/offset=0/status=running
   * 启动 codex 进程（FileName=codexExePath，Arguments=...）
   * 开两个采集线程：stdout/stderr -> 写 log 文件 + enqueue HistoryItem
3. 增加 `EditorApplication.update` 的 drain：

   * 把 queue 里的 HistoryItem 追加到 history.jsonl
   * 同时回调 UI 刷新（用事件或静态 Action）
4. 退出处理：

   * process.WaitForExit 在后台 Task
   * 写 meta.finishedAt/exitCode
   * state.activeStatus = completed/error
   * 最后如果 out.txt 存在就生成一条 assistant 总结气泡（或把 out.txt 当作最终气泡）

> 你当前 PollOutput 是 “文件出现内容就结束” ，这一步会被新的机制替代或降级为 fallback。

### Task D — Bootstrap 恢复升级

把 `CheckAndRecoverPendingRun()` 从“只补 out.txt”升级为：

* 先检查 state.activeStatus/running
* 如果 stdout/stderr/events 文件存在：追尾补齐到最新
* 再检查 out.txt 是否有最终总结需要补写
  入口仍由 `CodexBootstrap` 的 delayCall 触发 

### Task E — UI Toolkit 窗口重写

1. 新建 UXML/USS（先做结构，不追求完美）
2. CreateGUI：

   * 绑定按钮/字段事件（Send/New Task/Kill/Debug toggle）
   * 初始化加载历史（逐条气泡）
   * 注册 Runner 的 “新消息事件” 回调：来一条就 add bubble + scroll to bottom
3. 环境检查区仍保留 git/codex 检查（你现在用 CodexStore.HasGitRepository + CodexRunner.CheckCodexAvailable ）

---

## 8) 验收用例（必须跑通）

1. **正常发送**：点 Send 后，聊天区立刻出现 user 气泡，然后 stdout/stderr 逐条出现 event 气泡；结束后出现 assistant 总结气泡。
2. **Domain Reload**：运行中改脚本触发重编译；重载后重新打开窗口：历史完整恢复，并继续追尾新增输出。
3. **进程丢失**：模拟 codex 进程自行退出；UI 能显示“进程已结束/无法找到 PID”，并停止 running 状态。
4. **强杀**：运行中点击 Kill -> taskkill 成功；写入 system 气泡“已强杀”，state 变为 killed。
5. **Debug**：打开 debug toggle 后，Console 输出包含启动路径、args、pid、追尾字节数、kill 结果等。
