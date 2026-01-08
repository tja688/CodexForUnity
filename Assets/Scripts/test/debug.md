[CodexUnity] OnBeforeAssemblyReload: _isRunning=True, state.activeStatus=running, wasRunning=True
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:OnBeforeAssemblyReload () (at Assets/Editor/CodexUnity/CodexRunner.cs:156)
UnityEditor.AssemblyReloadEvents:OnBeforeAssemblyReload ()

[CodexUnity] 检测到运行中任务，已设置中断标志
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:OnBeforeAssemblyReload () (at Assets/Editor/CodexUnity/CodexRunner.cs:170)
UnityEditor.AssemblyReloadEvents:OnBeforeAssemblyReload ()

[CodexUnity] 采集输出失败: Thread was being aborted.
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:DebugLog (string) (at Assets/Editor/CodexUnity/CodexRunner.cs:1058)
CodexUnity.CodexRunner:CaptureStream (System.IO.StreamReader,string,string,string,string,string) (at Assets/Editor/CodexUnity/CodexRunner.cs:422)
CodexUnity.CodexRunner/<>c__DisplayClass40_0:<Execute>b__0 () (at Assets/Editor/CodexUnity/CodexRunner.cs:376)
System.Threading._ThreadPoolWaitCallback:PerformWaitCallback ()

[CodexUnity] 采集输出失败: Thread was being aborted.
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:DebugLog (string) (at Assets/Editor/CodexUnity/CodexRunner.cs:1058)
CodexUnity.CodexRunner:CaptureStream (System.IO.StreamReader,string,string,string,string,string) (at Assets/Editor/CodexUnity/CodexRunner.cs:422)
CodexUnity.CodexRunner/<>c__DisplayClass40_0:<Execute>b__1 () (at Assets/Editor/CodexUnity/CodexRunner.cs:377)
System.Threading._ThreadPoolWaitCallback:PerformWaitCallback ()

[CodexUnity] 等待进程退出失败: Thread was being aborted.
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:DebugLog (string) (at Assets/Editor/CodexUnity/CodexRunner.cs:1058)
CodexUnity.CodexRunner:WaitForExit (System.Diagnostics.Process,string) (at Assets/Editor/CodexUnity/CodexRunner.cs:405)
CodexUnity.CodexRunner/<>c__DisplayClass40_0:<Execute>b__2 () (at Assets/Editor/CodexUnity/CodexRunner.cs:378)
System.Threading._ThreadPoolWaitCallback:PerformWaitCallback ()

[CodexUnity] 在常见路径找到 codex: C:\Users\jinji\AppData\Roaming\npm\codex.cmd
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:DebugLog (string) (at Assets/Editor/CodexUnity/CodexRunner.cs:1058)
CodexUnity.CodexRunner:ResolveCodexPath () (at Assets/Editor/CodexUnity/CodexRunner.cs:849)
CodexUnity.CodexRunner:CheckCodexAvailable () (at Assets/Editor/CodexUnity/CodexRunner.cs:102)
CodexUnity.CodexWindow:CheckEnvironment () (at Assets/Editor/CodexUnity/CodexWindow.cs:200)
CodexUnity.CodexWindow:OnFocus () (at Assets/Editor/CodexUnity/CodexWindow.cs:111)
UnityEditor.DockArea:OnEnable ()

[CodexUnity] 初始化中...
UnityEngine.Debug:Log (object)
CodexUnity.CodexBootstrap:Initialize () (at Assets/Editor/CodexUnity/CodexBootstrap.cs:20)
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[CodexUnity] Assembly Reload 事件已绑定
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:BindAssemblyReloadEvents () (at Assets/Editor/CodexUnity/CodexRunner.cs:145)
CodexUnity.CodexBootstrap:Initialize () (at Assets/Editor/CodexUnity/CodexBootstrap.cs:26)
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[CodexUnity] CheckAndRecoverPendingRun: runId=20260108_171056_5432, status=running, pid=40680, interruptedByReload=True
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:CheckAndRecoverPendingRun () (at Assets/Editor/CodexUnity/CodexRunner.cs:1074)
CodexUnity.CodexBootstrap:Initialize () (at Assets/Editor/CodexUnity/CodexBootstrap.cs:29)
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[CodexUnity] 中断检查: SessionState=True, FileState=True, Combined=True
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:CheckAndRecoverPendingRun () (at Assets/Editor/CodexUnity/CodexRunner.cs:1130)
CodexUnity.CodexBootstrap:Initialize () (at Assets/Editor/CodexUnity/CodexBootstrap.cs:29)
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[CodexUnity] 进程状态: processLost=False, IsProcessAlive=True
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:CheckAndRecoverPendingRun () (at Assets/Editor/CodexUnity/CodexRunner.cs:1135)
CodexUnity.CodexBootstrap:Initialize () (at Assets/Editor/CodexUnity/CodexBootstrap.cs:29)
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[CodexUnity] 初始化完成
UnityEngine.Debug:Log (object)
CodexUnity.CodexBootstrap:Initialize () (at Assets/Editor/CodexUnity/CodexBootstrap.cs:31)
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Unity MCP] Auth info loaded from file
UnityEngine.Debug:Log (object)
UnityMCP.Editor.License.LicenseManagerV2:‪‭‌⁮⁬‫‭⁭‏‭⁮‭⁯‫‍⁫⁭⁪‎​⁪‏⁬‭⁭⁮‏‫⁬⁫‏⁯‎‪⁯‫⁮‏‌‬‮ (object)
UnityMCP.Editor.License.LicenseManagerV2:LoadAuthFromFile ()
UnityMCP.Editor.License.LicenseManagerV2:Initialize ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Unity MCP] License signature verified successfully
UnityEngine.Debug:Log (object)
UnityMCP.Editor.License.LicenseManagerV2:‪‭‌⁮⁬‫‭⁭‏‭⁮‭⁯‫‍⁫⁭⁪‎​⁪‏⁬‭⁭⁮‏‫⁬⁫‏⁯‎‪⁯‫⁮‏‌‬‮ (object)
UnityMCP.Editor.License.LicenseManagerV2:VerifySignedPayload (string,string)
UnityMCP.Editor.License.LicenseManagerV2:LoadLocalLicense ()
UnityMCP.Editor.License.LicenseManagerV2:Initialize ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Unity MCP] ✓ Security validation passed
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Security.CertificateValidator:‌⁮‎‫‬‫⁮‌⁫‍‌⁯‮‎‭⁭‫‭‌‍‍‪‫⁫‌​​‭​‌‪‏‬⁫‫⁬‍‍‬‏‮ (object)
UnityMCP.Editor.Security.CertificateValidator:ValidateAssembly ()
UnityMCP.Editor.Security.CertificateValidator/<>c:<.cctor>b__5_0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[MCP] Domain reload detected, reconnecting to server...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.MCPBridgeManager:‫⁭‌‫‍‍⁯⁭​‫⁪‮‭⁯⁭‎⁯‍⁯‌‪⁪⁭‮⁭‫‍‫​⁫‬‌⁮⁪⁭⁪‭‍⁪‌‮ (object)
UnityMCP.Editor.Bridge.MCPBridgeManager:ReconnectAfterReload ()
UnityMCP.Editor.Bridge.MCPBridgeManager/<>c:<Initialize>b__25_2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Connecting to pipe: UnityMCPPipe
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient:‮‪‭‫‏‌‎⁬‮‌‏⁪‌‫⁫⁫​‮‭⁪⁫⁮‏⁯⁮⁮⁬⁪⁯‫​‎‌‭⁪‍‮⁪‮⁮‮ (object)
UnityMCP.Editor.Bridge.BridgeClient:ConnectInBackground (string,System.Action`1<bool>)
UnityMCP.Editor.Bridge.MCPBridgeManager:ReconnectAfterReload ()
UnityMCP.Editor.Bridge.MCPBridgeManager/<>c:<Initialize>b__25_2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Background connect thread started
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient:‮‪‭‫‏‌‎⁬‮‌‏⁪‌‫⁫⁫​‮‭⁪⁫⁮‏⁯⁮⁮⁬⁪⁯‫​‎‌‭⁪‍‮⁪‮⁮‮ (object)
UnityMCP.Editor.Bridge.BridgeClient:ConnectInBackground (string,System.Action`1<bool>)
UnityMCP.Editor.Bridge.MCPBridgeManager:ReconnectAfterReload ()
UnityMCP.Editor.Bridge.MCPBridgeManager/<>c:<Initialize>b__25_2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Unity MCP] Valid license found on window open
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Windows.UserAuthenticationWindow:‫‎​‪⁫‌⁬​‪‬⁬‎‬‌⁬​⁯⁮⁮⁯⁯‍‏‭‏‬⁬⁭‪⁬⁬‪‮‍‍⁮‪⁭⁪‮ (object)
UnityMCP.Editor.Windows.UserAuthenticationWindow:<OnEnable>b__36_0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Background thread started, creating pipe client...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Attempting to connect (5 second timeout)...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Connect() returned, IsConnected: True
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Setting up streams...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Creating StreamReader...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] StreamReader created OK
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Creating StreamWriter...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] StreamWriter created OK (manual flush mode)
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Streams created successfully
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Connected to MCP Server via Named Pipe
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Starting read thread...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Read thread started
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Starting heartbeat...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Heartbeat started
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Scheduling main thread callback via delayCall...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Main thread callback executing...
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:‭⁯‫⁫⁬‭⁬⁯‪‫⁬‍‎⁬⁯‫‬‍⁮‮‌⁫⁭⁯⁫⁬⁭⁪⁮⁪‫​‮‪‫‫‬‮‮‮‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:<ConnectInBackground>b__2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Registered 55 tools with server
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient:‮‪‭‫‏‌‎⁬‮‌‏⁪‌‫⁫⁫​‮‭⁪⁫⁮‏⁯⁮⁮⁬⁪⁯‫​‎‌‭⁪‍‮⁪‮⁮‮ (object)
UnityMCP.Editor.Bridge.BridgeClient:RegisterToolsAsync ()
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:<ConnectInBackground>b__2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[MCP] Reconnected to server successfully after domain reload
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.MCPBridgeManager/<>c:⁪⁪⁪​⁪⁯⁭⁭‍⁫‍​‏‍‌⁯‪‭‫⁮‬⁬⁬⁮‮‬‌‏⁪⁪⁬‫‬‬‭‎⁫⁪‏‍‮ (object)
UnityMCP.Editor.Bridge.MCPBridgeManager/<>c:<ReconnectAfterReload>b__26_0 (bool)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:<ConnectInBackground>b__2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Unity MCP] Synced 55 enabled tools to standalone server
UnityEngine.Debug:Log (object)
UnityMCP.Editor.UnityMCPMain:‪‪⁭⁭‪⁪⁪⁪⁭⁬‭⁫‫‪⁬​⁬​​‍⁯‫‎⁪‫⁯​‌⁭‍‪⁪⁮‎‌⁫​‎⁮‬‮ (object)
UnityMCP.Editor.UnityMCPMain:SyncServerTools ()
UnityMCP.Editor.Bridge.MCPBridgeManager/<>c:<ReconnectAfterReload>b__26_0 (bool)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:<ConnectInBackground>b__2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Main thread callback complete
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:‭⁯‫⁫⁬‭⁬⁯‪‫⁬‍‎⁬⁯‫‬‍⁮‮‌⁫⁭⁯⁫⁬⁭⁪⁮⁪‫​‮‪‫‫‬‮‮‮‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass31_1:<ConnectInBackground>b__2 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Background thread work complete
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] ReadLoop started
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] ReadLoop: received message (166 chars)
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:⁪‫⁫‏⁫‎‎‫‏‎‌​⁭‌⁭⁭‭‫⁫‎⁯⁪⁫‍⁪⁬​⁪⁬‮⁪‎‍⁯⁫⁮‪⁫⁮‍‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass16_0:<ThreadLog>b__0 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[Bridge] Executing tool: editor_executeMenuItem
UnityEngine.Debug:Log (object)
UnityMCP.Editor.Bridge.BridgeClient/<HandleToolCall>d__36:‎‮‍‌‪‍‌⁫⁬​​‫⁫‪⁫⁮‮‍⁭‍‫‭‏‍‍‬‫‪‌‏‭‫⁫​‬⁭‪⁪‌‭‮ (object)
UnityMCP.Editor.Bridge.BridgeClient/<HandleToolCall>d__36:MoveNext ()
System.Runtime.CompilerServices.AsyncVoidMethodBuilder:Start<UnityMCP.Editor.Bridge.BridgeClient/<HandleToolCall>d__36> (UnityMCP.Editor.Bridge.BridgeClient/<HandleToolCall>d__36&)
UnityMCP.Editor.Bridge.BridgeClient:HandleToolCall (Newtonsoft.Json.Linq.JObject)
UnityMCP.Editor.Bridge.BridgeClient:ProcessMessage (UnityMCP.Editor.Bridge.PipeMessage)
UnityMCP.Editor.Bridge.BridgeClient/<>c__DisplayClass34_0:<ReadLoop>b__1 ()
UnityEditor.EditorApplication:Internal_CallDelayFunctions ()

[CodexUnity] TailActiveRunFiles: 进程丢失，但检测到 reload 中断标志，保持 running 状态
UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:TailActiveRunFiles () (at Assets/Editor/CodexUnity/CodexRunner.cs:574)
CodexUnity.CodexRunner:Update () (at Assets/Editor/CodexUnity/CodexRunner.cs:484)
UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()

[Unity MCP] License validation failed: 403 The remote server returned an error: (403) Forbidden.
UnityEngine.Debug:LogWarning (object)
UnityMCP.Editor.License.LicenseManagerV2/<ValidateLicenseAsync>d__44:⁬‌‎‍‪⁯‫‎‭‏‏⁪‍‍‍⁮⁬⁮⁬‬⁭‍⁮⁭⁯‮‭‮⁪⁫‪‫‪​⁭‍⁭‮‮‭‮ (object)
UnityMCP.Editor.License.LicenseManagerV2/<ValidateLicenseAsync>d__44:MoveNext ()
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()

[Unity MCP] Fallback validation failed: The remote server returned an error: (403) Forbidden.
UnityEngine.Debug:LogWarning (object)
UnityMCP.Editor.License.LicenseManagerV2/<ValidateLicenseAsync>d__44:⁬‌‎‍‪⁯‫‎‭‏‏⁪‍‍‍⁮⁬⁮⁬‬⁭‍⁮⁭⁯‮‭‮⁪⁫‪‫‪​⁭‍⁭‮‮‭‮ (object)
UnityMCP.Editor.License.LicenseManagerV2/<ValidateLicenseAsync>d__44:MoveNext ()
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()

[Unity MCP] License validation required - offline grace period expired
UnityEngine.Debug:LogError (object)
UnityMCP.Editor.License.LicenseManagerV2/<ValidateLicenseAsync>d__44:‮‌‫​‎‏‮‭‪‌‌‍​‪‎‭⁫⁬‬‏⁬⁪⁮⁬‎‬⁬‍‌‬⁬‫​⁫​⁯‫‏⁭‮ (object)
UnityMCP.Editor.License.LicenseManagerV2/<ValidateLicenseAsync>d__44:MoveNext ()
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()

[CodexUnity] taskkill output: 
错误: 没有找到进程 "40680"。

UnityEngine.Debug:Log (object)
CodexUnity.CodexRunner:DebugLog (string) (at Assets/Editor/CodexUnity/CodexRunner.cs:1058)
CodexUnity.CodexRunner:KillActiveProcessTree () (at Assets/Editor/CodexUnity/CodexRunner.cs:222)
CodexUnity.CodexWindow:KillRun () (at Assets/Editor/CodexUnity/CodexWindow.cs:664)
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

