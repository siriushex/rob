#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(WaterRippleCaster))]
    internal class WaterRippleCasterDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            EditorGUILayout.BeginVertical();
            PropertyField(serializedObject.FindProperty(nameof(WaterRippleCaster.mesh)));
            EditorGUILayout.EndVertical();
        }

        protected override void OnChanges()
        {
            var caster = target as WaterRippleCaster;
            caster.UpdateCaster();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }
    }
}
#endif