using UnityEditor;
using UnityEngine;

namespace CodexUnity
{
    /// <summary>
    /// 在 Domain Reload 后执行恢复逻辑
    /// </summary>
    [InitializeOnLoad]
    public static class CodexBootstrap
    {
        static CodexBootstrap()
        {
            // 延迟执行恢复逻辑，确保编辑器完全初始化
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            Debug.Log("[CodexUnity] 初始化中...");

            // 确保目录存在
            CodexStore.EnsureDirectoriesExist();

            // 绑定 Assembly Reload 事件
            CodexRunner.BindAssemblyReloadEvents();

            // 检查并恢复未完成的运行
            CodexRunner.CheckAndRecoverPendingRun();

            Debug.Log("[CodexUnity] 初始化完成");
        }
    }
}
