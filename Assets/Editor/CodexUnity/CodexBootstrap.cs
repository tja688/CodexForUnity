using UnityEditor;
using UnityEngine;

namespace CodexUnity
{
    /// <summary>
    /// 在 Domain Reload 后执行恢复逻辑 - 多实例版本
    /// </summary>
    [InitializeOnLoad]
    public static class CodexBootstrap
    {
        static CodexBootstrap()
        {
            // 延迟执行恢复逻辑，确保编辑器完全初始化
            EditorApplication.delayCall += Initialize;

            // 绑定 Assembly Reload 事件
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void Initialize()
        {
            Debug.Log("[CodexUnity] 初始化中（多实例模式）...");

            // 确保目录存在
            CodexStore.EnsureDirectoriesExist();

            // 初始化 InstanceManager 并恢复所有实例状态
            InstanceManager.Instance.RestoreAllStates();

            // 同时保持旧版 CodexRunner 的兼容性 (如果有现有代码依赖)
            try
            {
                CodexRunner.BindAssemblyReloadEvents();
                CodexRunner.CheckAndRecoverPendingRun();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CodexUnity] 旧版 CodexRunner 初始化跳过: {e.Message}");
            }

            Debug.Log("[CodexUnity] 初始化完成");
        }

        private static void OnBeforeAssemblyReload()
        {
            Debug.Log("[CodexUnity] Domain Reload 前保存所有实例状态...");

            // 保存所有实例状态
            InstanceManager.Instance.SaveAllStates();
        }
    }
}
