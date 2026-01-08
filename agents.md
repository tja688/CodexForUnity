# agent.md — Unity ↔ Codex CLI 最小可运行版本（exec/resume，支持 Unity 自动重编译）

目标：做一个 **Unity Editor 插件（EditorWindow）**，通过 **Codex CLI 的 `codex exec` / `codex exec resume --last`** 驱动本地编程操作。  
约束（用户已确认）：
- 不做跨平台适配/分发/安装（本地开发测试即可）
- 不限制 Unity 自动重编译（Domain Reload 必须能扛住）
- 不做 diff/rollback/patch 审阅
- 不做可用额度显示
- 不做斜杠命令
- 默认 `--full-auto`（用户承担风险）
- 不碰 TUI，只用 exec/resume
- 要求用户项目已 `git init`（没有就提示并拒绝执行）

---

## 1. 最小功能清单（MVP）
1) Unity 菜单打开窗口：`Tools/Codex`
2) 窗口包含：
   - Prompt 输入框（多行）
   - Model 输入（先用文本输入即可，默认值写死一个，例如 `gpt-5.1-codex-mini`；不要求动态拉取）
   - Reasoning Effort 下拉（minimal/low/medium/high/xhigh）
   - 按钮：`Send`、`New Task`
   - 输出区：显示历史对话（粗糙拼文本即可）
   - 状态提示：是否正在运行、最近一次 run 的 id/时间、错误提示
3) `New Task`：
   - 清空本地历史（仅插件自己的 history）
   - 清空 state，下一次 Send 不走 resume
4) `Send`：
   - 若没有历史/已 New Task：`codex exec ... "<prompt>"`
   - 若已有历史：`codex exec resume --last ... "<prompt>"`
   - 使用 `-o <out.txt>` 把最终回复写到文件
   - 执行完成后把 `<out.txt>` 追加到 `history.jsonl` 并刷新 UI
5) 关键：**Unity Domain Reload 后对话不丢**
   - history/state 落盘到 `Library/CodexUnity/`
   - `[InitializeOnLoadMethod]` 启动时恢复 UI 状态（至少恢复历史文本）
   - 若上次 run 未写入历史但 out.txt 已存在：补写入

---

## 2. 目录与文件约定（固定）
在 Unity 项目下创建并使用（运行时自动创建）：

- `Library/CodexUnity/state.json`
- `Library/CodexUnity/history.jsonl`
- `Library/CodexUnity/runs/<runId>/out.txt`
- `Library/CodexUnity/runs/<runId>/meta.json`

### state.json（示例）
```json
{
  "hasActiveThread": true,
  "lastRunId": "20260108_153012_1234",
  "lastRunOutPath": "Library/CodexUnity/runs/20260108_153012_1234/out.txt",
  "model": "gpt-5.1-codex-mini",
  "effort": "medium"
}
````

### history.jsonl（每行一条）

每行一个 JSON 对象，至少字段：

* `ts`（ISO8601）
* `role`：`user` / `assistant`
* `text`

示例行：

```json
{"ts":"2026-01-08T15:30:12+08:00","role":"user","text":"把 PlayerController 的跳跃改成支持二段跳"}
{"ts":"2026-01-08T15:30:45+08:00","role":"assistant","text":"已完成：..."}
```

---

## 3. 运行命令（核心）

项目根目录：`<ProjectRoot> = Directory.GetParent(Application.dataPath).FullName`

### Git 仓库校验（必须）

执行前检查 `<ProjectRoot>/.git` 是否存在；不存在则提示：

* “请先在项目根目录执行 git init（本插件要求 git repo）”
  并 **拒绝执行**（不要尝试自动 git init）。

### 新会话（首次/ New Task 后）

```bash
codex exec --full-auto -C "<ProjectRoot>" --model "<model>" -c model_reasoning_effort="<effort>" -o "<out.txt>" "<prompt>"
```

### 续会话（已有历史）

```bash
codex exec resume --last --full-auto -C "<ProjectRoot>" --model "<model>" -c model_reasoning_effort="<effort>" -o "<out.txt>" "<prompt>"
```

注意：

* 不做流式输出（避免 Domain Reload 期间管道阻塞/句柄丢失）
* 只依赖 `-o out.txt` 拿最终回复
* 可选：stderr 也可以重定向到 `runs/<runId>/stderr.txt`，但 MVP 不必须

---

## 4. Unity 端实现要点（强制要求）

### 4.1 不要做实时 stdout/stderr 读取

原因：Unity 重编译会导致读线程/回调丢失，且管道缓冲可能卡住外部进程。
MVP：只等文件输出。

### 4.2 进程启动方式（最稳）

使用 `System.Diagnostics.ProcessStartInfo`：

* `FileName = "codex"`（假设已在 PATH）
* `WorkingDirectory = <ProjectRoot>`（可选；同时也会传 `-C`）
* 参数建议用 `ArgumentList`（若当前 Unity/.NET 支持），避免引号拼接问题：

  * `exec`
  * （可选）`resume`, `--last`
  * `--full-auto`
  * `-C`, `<ProjectRoot>`
  * `--model`, `<model>`
  * `-c`, `model_reasoning_effort=<effort>`
  * `-o`, `<out.txt>`
  * `<prompt>`

若 `ArgumentList` 不可用，则手写参数字符串并用严谨的引号包裹路径与 prompt。

### 4.3 轮询 out.txt（EditorApplication.update）

* 点击 Send 后：

  * 生成 runId
  * 写 meta.json（包含命令、prompt、model、effort、time）
  * 更新 state.json（lastRunId 等）
  * 启动进程（可不保留句柄；保留也可以，但不依赖它）
  * 注册一个轮询器，每 0.2~0.5 秒检查 `out.txt`：

    * 文件存在且长度 > 0：读入文本，追加到 history（assistant）
    * 然后停止轮询
* 轮询过程中 UI 显示 “Running…”

### 4.4 Domain Reload 恢复

用 `[InitializeOnLoadMethod]` 或静态构造：

* 确保 `Library/CodexUnity/` 存在
* 读取 history.jsonl（如果存在）到内存缓存（供窗口显示）
* 读取 state.json：

  * 如果 lastRunOutPath 存在但还没写入 history（可通过 meta 标记或对比 history 最后一条 ts/runId 判断）：补写一条 assistant
* 打开窗口时也重新读 history（避免内存缓存过期）

### 4.5 环境检查（最低限度）

窗口顶部显示 Codex CLI 检查结果：

* 尝试运行：`codex --version`
* 失败则提示：`codex not found in PATH`（并拒绝执行）
  （不需要给安装指导，因为用户明确不做分发/安装）

---

## 5. 代码结构建议（放到 Packages 或 Assets/Editor 都可）

最省事：`Assets/Editor/CodexUnity/`

文件建议：

* `CodexWindow.cs`：EditorWindow UI + 触发 Send/New
* `CodexRunner.cs`：拼命令、启动进程、写 meta/state
* `CodexStore.cs`：读写 state.json / history.jsonl / 路径管理
* `CodexBootstrap.cs`：InitializeOnLoad 恢复逻辑
* `Models.cs`：State/HistoryItem/Meta 数据结构

---

## 6. 伪代码骨架（给实现方向）

### 6.1 ProjectRoot

```csharp
var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
```

### 6.2 Send 流程

1. 校验 `.git` 目录存在，否则弹窗并 return
2. runId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000,9999)}"
3. outPath = $"Library/CodexUnity/runs/{runId}/out.txt"（确保目录存在）
4. history append user
5. state.hasActiveThread = true（一旦执行过就视为 active）
6. 启动 codex exec（根据是否 hasActiveThread 决定 resume）
7. 开始轮询 out.txt，成功后 history append assistant，保存

### 6.3 New Task

* 删除/清空 `history.jsonl`
* state.hasActiveThread=false；state.lastRunId=null；保存
* UI 清空显示

---

## 7. 验收标准（必须全部通过）

1. 项目已 `git init` 时：

   * 打开 Tools/Codex
   * Send 一条 prompt：能在 out.txt 得到回复，并显示在窗口历史中
2. 再 Send 第二条 prompt：

   * 走 `resume --last`，对话连续（至少 Codex 侧上下文连续，返回内容能体现承接上一轮）
3. 让 Codex 修改一个 C# 脚本触发 Unity 自动重编译：

   * 编译/Domain Reload 后再次打开窗口：历史仍在（来自 history.jsonl）
   * 能继续 Send，且仍走 resume --last
4. 没有 `.git` 时：

   * 窗口明确提示必须 git init，并拒绝执行
5. 找不到 codex 命令时：

   * 窗口提示 codex not found，并拒绝执行

---

## 8. 非目标（明确不做）

* 流式输出/实时事件解析（--json 不用）
* diff / rollback / patch 审阅
* 可用额度显示
* slash 命令（/new /model 等）支持
* 多会话列表/并行任务
* TUI 嵌入/PTY

---

## 9. 风险声明（实现时原样放在 UI）

* `--full-auto` 允许 Codex 直接修改你的工程文件并运行命令，可能导致不可预期的改动。
* 本 MVP 不提供回滚/审阅，请自行使用 Git 管理风险。

---

## 10. 交付物

提交一个可运行的 Unity 工程内插件（上述目录结构），能通过“验收标准”。
