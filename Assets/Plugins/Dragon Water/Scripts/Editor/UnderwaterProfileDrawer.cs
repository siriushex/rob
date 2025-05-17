#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DragonWater.UnderwaterProfile;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(UnderwaterProfile))]
    internal class UnderwaterProfileDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            var profile = target as UnderwaterProfile;

            RepushGUITint(1.0f, 1.0f, 0.75f);
            DrawFoldoutSection("up_fog", "Fog", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.overrideFog)));
                if (profile.overrideFog)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty($"{nameof(UnderwaterProfile.fogConfig)}.{nameof(FogConfig.fog)}"));
                    if (profile.fogConfig.fog)
                    {
                        PropertyField(serializedObject.FindProperty($"{nameof(UnderwaterProfile.fogConfig)}.{nameof(FogConfig.fogColor)}"));
                        PropertyField(serializedObject.FindProperty($"{nameof(UnderwaterProfile.fogConfig)}.{nameof(FogConfig.fogMode)}"));
                        if (profile.fogConfig.fogMode == FogMode.Linear)
                        {
                            PropertyField(serializedObject.FindProperty($"{nameof(UnderwaterProfile.fogConfig)}.{nameof(FogConfig.fogStartDistance)}"));
                            PropertyField(serializedObject.FindProperty($"{nameof(UnderwaterProfile.fogConfig)}.{nameof(FogConfig.fogEndDistance)}"));
                        }
                        else
                        {
                            PropertyField(serializedObject.FindProperty($"{nameof(UnderwaterProfile.fogConfig)}.{nameof(FogConfig.fogDensity)}"));
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.showSuperfarPlane)));
            });


            EditorGUILayout.Space();
            RepushGUITint(0.75f, 1.0f, 1.0f);
            DrawFoldoutSection("up_godrays", "God Rays", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.showGodRays)));
                if (profile.showGodRays)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysTint)), "Tint");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysLength)), "Rays Length");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysWidthRange)), "Rays Width Range");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysIntensity)), "Intensity");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysDensity)), "Density");

                    var distanceProperty = serializedObject.FindProperty(nameof(UnderwaterProfile.raysDistance));
                    PropertyField(distanceProperty, "Visibility Distance");
                    distanceProperty.floatValue = Mathf.Clamp(distanceProperty.floatValue, 0, 240);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawToggleSection("underwater_godrays_advanced", "Show Advanced...", () =>
                    {
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysDepthFade)), "Depth Fade");
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysCameraFade)), "Camera Fade");
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysWaterFade)), "Water Fade");
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.raysOccludeByShadows)), "Occlude By Shadows");
                    }, false);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndVertical();
                }
            });


            EditorGUILayout.Space();
            RepushGUITint(1.0f, 0.75f, 1.0f);
            DrawFoldoutSection("up_caustics", "Caustics", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.showCaustics)));
                if (profile.showCaustics)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsTint)), "Tint");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsIntensity)), "Intensity");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsTexture1)), "Texture 1");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsTexture2)), "Texture 2");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsDistance)), "Visibility Distance");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsDepth)), "Max Depth");

                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsChromaticAberration)), "Chromatic Aberration");
                    if (profile.causticsChromaticAberration)
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsChromaticAberrationStrength)), "Strength");
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawToggleSection("underwater_caustics_advanced", "Show Advanced...", () =>
                    {
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsWaterFade)), "Water Fade");
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsWaterLevelOffset)), "Water Level Offset");
                        PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsOccludeByShadows)), "Occlude By Shadows");
                        if (profile.causticsOccludeByShadows)
                        {
                            EditorGUI.indentLevel++;
                            PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.causticsShadowsAttenuation)), "Attenuation");
                            EditorGUI.indentLevel--;
                        }
                    }, false);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndVertical();
                }
            });


            EditorGUILayout.Space();
            RepushGUITint(1.0f, 0.75f, 0.75f);
            DrawFoldoutSection("up_volume", "Volume", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.overrideVolume)));
                if (profile.overrideVolume)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.volumeProfile)), "Profile");
                    PropertyField(serializedObject.FindProperty(nameof(UnderwaterProfile.volumeWeight)), "Weight");
                    EditorGUILayout.EndVertical();
                }
            });
        }

        protected override void OnChanges()
        {
            DragonWaterManager.Instance.UpdateUnderwaterProfile();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }
    }
}
#endif