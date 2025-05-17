#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DragonWater.MaterialProfile;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(MaterialProfile))]
    internal class MaterialProfileDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            var profile = target as MaterialProfile;

            RepushGUITint(1.0f, 1.0f, 0.75f);
            DrawFoldoutSection("mp_base", "Base Material Settings", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.waterDepth)));
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.shallowWaterColor)));
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.deepWaterColor)));
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.smoothness)));
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.specular)));
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.specularHighlights)));
            });

            EditorGUILayout.Space();
            RepushGUITint(0.25f, 0.75f, 1.0f);
            DrawFoldoutSection("mp_colornoise", "Color Noise", () =>
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawTitle("Hillness Lighten/Darken");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseHillnessOffset)), "Hillness Offset");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseHillnessLighten)), "Lighten Hill");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseHillnessDarken)), "Darken Depth");
                EditorGUILayout.EndVertical();


                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.useColorNoiseTexture)), "Use Noise Texture");
                if (profile.useColorNoiseTexture)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawTitle("Noise Texture");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseColor)), "Color");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseIntensity)), "Intensity");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseEmission)), "Emision Rate");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseTexture1)));
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.colorNoiseTexture2)));
                    EditorGUILayout.EndVertical();
                }
            });

            EditorGUILayout.Space();
            RepushGUITint(1.0f, 0.75f, 0.5f);
            DrawFoldoutSection("mp_sss", "Subsurface Scattering", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.sssGlobalIntensity)), "Global Intensity");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.sssNormalInfluence)), "Normal influence");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.sssPower)), "Power");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.sssOverlayStrength)), "Overlay Strength");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.sssEmissionStrength)), "Emission Strength");
            });

            EditorGUILayout.Space();
            RepushGUITint(0.75f, 1.0f, 1.0f);
            DrawFoldoutSection("mp_normal", "Normal", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.normalGlobalStrength)));
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.useNormalMap)));
                if (profile.useNormalMap)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.normalMap1)));
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.normalMap2)));
                    EditorGUILayout.EndVertical();
                }
            });

            EditorGUILayout.Space();
            RepushGUITint(1.0f, 0.75f, 1.0f);
            DrawFoldoutSection("mp_foam", "Foam", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.useFoam)));
                if (profile.useFoam)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamGlobalIntensity)), "Global Intensity");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamEmissionRate)), "Emission Rate");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamColor)), "Color");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamNoiseTexture)), "Noise Texture");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamNoiseSize)), "Noise Size");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamSmoothnessFactor)), "Smoothness Factor");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamSpecularFactor)), "Specular Factor");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamNormalAttenuation)), "Normal Attenuation");
                    EditorGUI.indentLevel++;
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamNormalAttenuationBias)), "Bias");
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamEdge)), "Edge Foam");
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamHillness)), "Hillness Foam");
                    EditorGUILayout.EndVertical();

                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamExtraLayers)), "Extra Layers");
                    if (profile.foamExtraLayers)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamHillnessExtraLayer1)), "Hillness Extra Layer 1");
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.foamHillnessExtraLayer2)), "Hillness Extra Layer 2");
                        EditorGUILayout.EndVertical();
                    }
                }
            });


            EditorGUILayout.Space();
            RepushGUITint(0.75f, 1.0f, 0.75f);
            DrawFoldoutSection("mp_transparency", "Transparency", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.transparencyType)));
                if (profile.transparencyType == TransparencyType.Refraction)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.refractionStrength)));
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.refractionDistanceFactor)));
                    EditorGUILayout.EndVertical();
                }
            });


            EditorGUILayout.Space();
            RepushGUITint(0.75f, 0.75f, 1.0f);
            DrawFoldoutSection("mp_reflections", "Reflections", () =>
            {
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.reflectionsMode)));
                if (profile.reflectionsMode == ReflectionsMode.Simple)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.reflectionsGlossyTint)), "Glossy Tint");
                    EditorGUILayout.EndVertical();
                }
                else if (profile.reflectionsMode == ReflectionsMode.DefaultEnvironmental)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.reflectionsIntensity)), "Intensity");
                    EditorGUILayout.EndVertical();
                }
                else if (profile.reflectionsMode == ReflectionsMode.CustomCubemap)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.reflectionsCubemap)), "Cubemap");
                    PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.reflectionsIntensity)), "Intensity");
                    EditorGUILayout.EndVertical();
                }
            });


            EditorGUILayout.Space();
            RepushGUITint(1.0f, 0.75f, 0.75f);
            DrawFoldoutSection("mp_ripple", "Ripple", () =>
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.rippleSmoothnessFactor)), "Smoothness Factor");
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.rippleSpecularFactor)), "Specular Factor");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.rippleColor)), "Color Modification");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.rippleNoise)), "Color Noise");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.rippleNormal)), "Normal Modification");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty(nameof(MaterialProfile.rippleFoam)), "Foam Modification");
                EditorGUILayout.EndVertical();
            });
        }

        protected override void OnChanges()
        {
            var profile = target as MaterialProfile;
            profile.SetMaterialsDirty();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }
    }
}
#endif