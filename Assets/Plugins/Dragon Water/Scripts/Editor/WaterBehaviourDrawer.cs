#if UNITY_EDITOR
using DragonWater.Scripting;
using UnityEditor;
using WSC = DragonWater.Scripting.WaterBehaviour.WaterSamplerConfig;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(WaterBehaviour), true)]
    internal class WaterBehaviourDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            var behaviour = (WaterBehaviour)target;

            DrawFoldoutSection("wb_sampler", "Water Sampler Config", () =>
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.precision)}"));
                PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.frameDivider)}"));

                EditorGUILayout.Space();
                PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.surfaceDetection)}"));
                if (behaviour.samplerConfig.surfaceDetection == WaterSampler.SurfaceDetectionMode.Custom)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.surfaces)}"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
                PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.cutoutDetection)}"));
                if (behaviour.samplerConfig.cutoutDetection == WaterSampler.CutoutDetectionMode.Custom)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.cutouts)}"));
                    EditorGUI.indentLevel--;
                }

                if (ShouldDrawCullingBox())
                {
                    EditorGUILayout.Space();
                    PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.autoCullingBox)}"));
                    EditorGUI.indentLevel++;
                    if (!behaviour.samplerConfig.autoCullingBox)
                    {
                        PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.cullingBoxSize)}"), "Size");
                    }
                    PropertyField(serializedObject.FindProperty($"{nameof(WaterBehaviour.samplerConfig)}.{nameof(WSC.cullingRefreshTime)}"), "Refresh Time");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            });

            EditorGUILayout.Space();
            DrawInspectorDefault();
        }

        protected virtual void DrawInspectorDefault()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(WaterBehaviour.samplerConfig));
        }

        private bool ShouldDrawCullingBox()
        {
            var behaviour = (WaterBehaviour)target;
            return behaviour.samplerConfig.surfaceDetection == WaterSampler.SurfaceDetectionMode.AutoCull
                || behaviour.samplerConfig.cutoutDetection == WaterSampler.CutoutDetectionMode.AutoCull;
        }
    }
}
#endif