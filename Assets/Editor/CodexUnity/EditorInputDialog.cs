using UnityEditor;
using UnityEngine;

namespace CodexUnity
{
    /// <summary>
    /// 简易输入对话框
    /// </summary>
    public static class EditorInputDialog
    {
        /// <summary>
        /// 显示输入对话框
        /// </summary>
        public static string Show(string title, string message, string defaultValue = "")
        {
            var window = EditorWindow.GetWindow<InputDialogWindow>(true, title, true);
            window.message = message;
            window.inputValue = defaultValue;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(400, 120);
            window.ShowModalUtility();
            return window.confirmed ? window.inputValue : null;
        }
    }

    /// <summary>
    /// 输入对话框窗口
    /// </summary>
    public class InputDialogWindow : EditorWindow
    {
        public string message = "";
        public string inputValue = "";
        public bool confirmed;

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(message);
            EditorGUILayout.Space(5);

            GUI.SetNextControlName("InputField");
            inputValue = EditorGUILayout.TextField(inputValue);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("取消", GUILayout.Width(80)))
            {
                confirmed = false;
                Close();
            }

            if (GUILayout.Button("确定", GUILayout.Width(80)))
            {
                confirmed = true;
                Close();
            }

            EditorGUILayout.EndHorizontal();

            // 按 Enter 确认
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                confirmed = true;
                Close();
            }

            // 按 Escape 取消
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                confirmed = false;
                Close();
            }

            // 自动聚焦输入框
            EditorGUI.FocusTextInControl("InputField");
        }
    }
}
