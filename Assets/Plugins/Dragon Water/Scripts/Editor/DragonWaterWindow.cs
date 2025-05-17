#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    internal class DragonWaterWindow : EditorWindow
    {
        UnityEditor.Editor _configEditor = null;
        Vector2 _scrollViewPosition = Vector2.zero;

        public static void ShowWindow()
        {
            var window = GetWindow<DragonWaterWindow>();
            window.titleContent = new("Dragon Water Config");
            window.Show();
        }

        private void OnEnable()
        {
            UnityEditor.Editor.CreateCachedEditor(DragonWaterManager.Instance.Config, typeof(DragonWaterConfigDrawer), ref _configEditor);
        }

        private void OnGUI()
        {
            var configEditor = _configEditor as DragonWaterConfigDrawer;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(5);

            _scrollViewPosition = EditorGUILayout.BeginScrollView(_scrollViewPosition, false, false);
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                    configEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(20);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif