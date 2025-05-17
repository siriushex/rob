#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(LocalWaveArea))]
    internal class LocalWaveAreaDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            var area = target as LocalWaveArea;

            DrawTitle("Boundary");
            PropertyField(serializedObject.FindProperty(nameof(LocalWaveArea.radius)));
            PropertyField(serializedObject.FindProperty(nameof(LocalWaveArea.innerRadiusRatio)));

            EditorGUILayout.Space();
            DrawTitle("Influences");
            PropertyField(serializedObject.FindProperty(nameof(LocalWaveArea.amplitudeMultiplier)));
            PropertyField(serializedObject.FindProperty(nameof(LocalWaveArea.steepnessMultiplier)));
            PropertyField(serializedObject.FindProperty(nameof(LocalWaveArea.hillnessMultiplier)));
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }

        protected override void DrawInspectorDebug()
        {
            EditorGUILayout.IntField("Currently Active Areas", DragonWaterManager.Instance.LocalWaveAreas.Count);
        }
    }
}
#endif