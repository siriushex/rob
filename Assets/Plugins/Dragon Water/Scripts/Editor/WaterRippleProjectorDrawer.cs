#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(WaterRippleProjector))]
    internal class WaterRippleProjectorDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            CheckURPRippleRenderer();

            var projector = target as WaterRippleProjector;

            EditorGUILayout.BeginVertical();
            projector.TextureSize = EditorGUILayout.IntPopup("Texture Size", projector.TextureSize, TEXTURE_SIZES_STR, TEXTURE_SIZES);
            projector.Type = (WaterRippleProjector.ProjectorType)EditorGUILayout.EnumPopup("Type", projector.type);
            EditorGUI.indentLevel++;
            switch (projector.type)
            {
                case WaterRippleProjector.ProjectorType.Local:
                    PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.size)));
                    break;
                case WaterRippleProjector.ProjectorType.Infinite:
                    PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.distance)));
                    PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.attachTo)));
                    if (projector.attachTo == WaterRippleProjector.AttachTarget.CustomObject)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.attachCustomTarget)), "Target");
                        if (projector.attachCustomTarget == null)
                            EditorGUILayout.HelpBox("No custom target specified. Main Camera will be used instead.", MessageType.Warning);
                        EditorGUI.indentLevel--;
                    }
                    break;
            }
            EditorGUI.indentLevel--;
            PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.upperClip)));
            PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.lowerClip)));
            PropertyField(serializedObject.FindProperty(nameof(WaterRippleProjector.precision)));
            EditorGUILayout.EndVertical();
        }

        private void CheckURPRippleRenderer()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            var defaultRenderer = pipeline.scriptableRenderer;
            var rippleRenderer = pipeline.GetRenderer(DragonWaterManager.Instance.Config.RippleURPRendererIndex);

            if (defaultRenderer != rippleRenderer)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Your URP pipeline is using same renderer for main rendering and ripple.\nDon't you want it to be separated?", MessageType.Warning);
            if (GUILayout.Button("Select Pipeline Asset"))
                EditorGUIUtility.PingObject(pipeline);
            if (GUILayout.Button("Open Dragon Water Config"))
                DragonWaterWindow.ShowWindow();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }

        protected override void DrawInspectorDebug()
        {
            var projector = (WaterRippleProjector)target;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField("RT", projector.ProjectionTexture, typeof(RenderTexture), false);
            EditorGUILayout.EndVertical();

            foreach (var rippler in projector.Ripplers)
            {
                EditorGUILayout.Space();

                if (rippler.IsEnqueued)
                    PushGUITint(1.0f, 1.25f, 1.0f);
                else
                    PushGUITint(0.75f, 0.75f, 0.75f);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Rippler - {rippler.Profile.name}", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Simulation", rippler.SimulationTexture, typeof(RenderTexture), false);
                //EditorGUILayout.ObjectField("Ripple", rippler.RippleTexture, typeof(RenderTexture), false);
                EditorGUILayout.EndVertical();

                PopGUITint();
            }
        }
    }
}
#endif