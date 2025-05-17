#if UNITY_EDITOR
using DragonWater.Attributes;
using DragonWater.Editor.Build;
using DragonWater.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(DragonWaterConfig))]
    internal class DragonWaterConfigDrawer : EditorEx
    {
        static readonly float[] OCEAN_DENSITY_VALUES = new float[] { 0.125f, 0.25f, 0.5f, 1.0f, 2.0f, 4.0f, 8.0f, 16.0f };
        static readonly string[] OCEAN_DENSITY_NAMES = new string[] { "1\\8", "1\\4", "1\\2", "1", "2", "4", "8", "16" };


        string _newPresetName = "New Preset Name";
        Dictionary<int, int> _presetVerticesCache = new();

        protected override void DrawInspector()
        {
            var config = target as DragonWaterConfig;

            PushGUITint(1, 1, 1);

            DrawTitle("General");
            {
                PropertyField(serializedObject.FindProperty(nameof(DragonWaterConfig.autoTimeSimulation)));
                config.waterRendererLayer = LayerField("Water Renderer Layer", config.waterRendererLayer);

                EditorGUILayout.HelpBox("Make sure water layer is included in your camera's culling mask!", MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawTitle("Infinite Ocean Mesh Presets");
            if (config.oceanQualities == null)
                config.oceanQualities = new();
            for (int i = 0; i < config.oceanQualities.Count; i++)
            {
                var preset = config.oceanQualities[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                DrawFoldoutSection("oceanpreset_"+ preset.name, preset.name, () =>
                {
                    EditorGUI.BeginChangeCheck();
                    DrawOceanPreset(preset);
                    if (EditorGUI.EndChangeCheck())
                    {
                        DragonWaterManager.Instance.Surfaces
                        .Where(s => s.geometryType == WaterSurface.GeometryMeshType.InfiniteOcean)
                        .ToList()
                        .ForEach(s => s.InvalidateMesh());
                    }
                }, false);

                EditorGUILayout.EndVertical();
                PushGUITint(1.25f, 0.75f, 0.75f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(50));
                if (GUILayout.Button("X"))
                {
                    if (EditorUtility.DisplayDialog("Delete preset?", $"Delete \"{preset.name}\" ocean mesh preset?\nThis will break existing surfaces using it.", "Yes", "Cancel"))
                    {
                        config.oceanQualities.RemoveAt(i);
                        break;
                    }
                }
                EditorGUILayout.EndVertical();
                PopGUITint();
                EditorGUILayout.EndHorizontal();
            }

            PushGUITint(0.75f, 1.0f, 0.75f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _newPresetName = EditorGUILayout.TextField(_newPresetName);
            GUI.enabled = !string.IsNullOrWhiteSpace(_newPresetName) && !config.oceanQualities.Any(p => p.name == _newPresetName);
            if (GUILayout.Button("Add"))
            {
                config.oceanQualities.Add(new() { name = _newPresetName });
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            PopGUITint();


            EditorGUILayout.Space();
            DrawTitle("Cutout Volumes");
            {
                if (config.cutoutLayer == -1 || string.IsNullOrEmpty(LayerMask.LayerToName(config.cutoutLayer)))
                {
                    config.cutoutLayer = Layers.InstallLayer("Dragon Water Cutout");
                    EditorUtility.SetDirty(target);
                    EditorApplication.delayCall += EditorBackgroundRunner.SetLayersVisibility;
                }

                config.cutoutLayer = LayerField("Cutout Layer", config.cutoutLayer);
                EditorGUILayout.HelpBox("Make sure cutout layer is included in your camera's culling mask!", MessageType.Warning);
            }


            EditorGUILayout.Space();
            DrawTitle("Underwater Effect");
            {
                config.underwaterVolumeLayer = LayerField("Underwater Volume Layer", config.underwaterVolumeLayer);

                PropertyField(serializedObject.FindProperty(nameof(DragonWaterConfig.underwaterVolumePriority)));
            }


            EditorGUILayout.Space();
            DrawTitle("Ripple Effect");
            {
                if (config.rippleLayer == -1 || string.IsNullOrEmpty(LayerMask.LayerToName(config.rippleLayer)))
                {
                    config.rippleLayer = Layers.InstallLayer("Dragon Water Ripple");
                    EditorUtility.SetDirty(target);
                    EditorApplication.delayCall += EditorBackgroundRunner.SetLayersVisibility;
                }

                config.rippleLayer = LayerField("Ripple Layer", config.rippleLayer);
                PropertyField(serializedObject.FindProperty(nameof(DragonWaterConfig.rippleURPRendererIndex)), "URP Renderer Index");

                EditorGUILayout.HelpBox("You can safely disable ripple layer from your camera's culling mask.", MessageType.Info);
            }


            EditorGUILayout.Space();
            DrawTitle("Build Settings");
            {
                DrawFoldoutSection("build_water_shader_stripping", "Water Shader Stripping",
                    () => { DrawShaderStripping(ref config.buildWaterShaderStripping, Constants.Shader.WaterShaderName); },
                    false);
            }


            EditorGUILayout.Space();
            DrawTitle("Debug Settings");
            {
                PropertyField(serializedObject.FindProperty(nameof(DragonWaterConfig.debugLevel)));
                PropertyField(serializedObject.FindProperty(nameof(DragonWaterConfig.drawDebugInspectors)));
            }

            PopGUITint();
        }

        protected override void OnChanges()
        {
            foreach (var rp in DragonWaterManager.Instance.RippleProjectors)
            {
                rp.UpdateProjector();
            }
        }

        private void DrawOceanPreset(OceanMeshQualityPreset preset)
        {
            // validate
            if (preset.densities.Count != preset.lengths.Count)
            {
                preset.densities.Clear();
                preset.lengths.Clear();
            }
            if (preset.densities.Count == 0)
            {
                preset.densities.Add(1);
                preset.lengths.Add(100);
            }
            for (int i = 0; i < preset.densities.Count; i++)
            {
                var density = Mathf.Clamp(preset.densities[i], OCEAN_DENSITY_VALUES[0], OCEAN_DENSITY_VALUES[^1]);
                var length = preset.lengths[i];

                if (i == 0)
                {
                    preset.densities[i] = density;
                }
                else
                {
                    preset.densities[i] = Mathf.Max(density, preset.densities[i-1]);
                    preset.lengths[i] = Mathf.Max(length, preset.lengths[i-1]);
                }    
            }
            preset.maxFarDistance = Mathf.Max(preset.maxFarDistance, preset.lengths[^1]);
            preset.gridSnapUnits = Mathf.RoundToInt(Mathf.Max(1, preset.densities[^1]));

            if (!_presetVerticesCache.TryGetValue(preset.GetHashCode(), out var maxVertices))
            {
                var creator = new OceanMeshCreator(false);
                creator.ApplyPreset(preset);
                maxVertices = creator.CountVertices * 4; // 4 planes
                _presetVerticesCache.Add(preset.GetHashCode(), maxVertices);
            }

            // draw inspector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("L#", EditorStyles.boldLabel, GUILayout.Width(30));
            EditorGUILayout.LabelField("Density", EditorStyles.boldLabel, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("Length", EditorStyles.boldLabel, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("-", EditorStyles.boldLabel, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < preset.densities.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("L" + i.ToString(), EditorStyles.boldLabel, GUILayout.Width(30));

                var densityIndex = Array.IndexOf(OCEAN_DENSITY_VALUES, preset.densities[i]);
                var newDensity = EditorGUILayout.Popup(densityIndex, OCEAN_DENSITY_NAMES);
                var newLength = EditorGUILayout.DelayedFloatField(preset.lengths[i]);

                preset.densities[i] = OCEAN_DENSITY_VALUES[newDensity];
                preset.lengths[i] = newLength;

                GUI.enabled = i > 0;
                PushGUITint(1, 0.75f, 0.75f);
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    preset.densities.RemoveAt(i);
                    preset.lengths.RemoveAt(i);
                    break;
                }
                PopGUITint();
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            PushGUITint(0.75f, 1.0f, 0.75f);
            if (GUILayout.Button("+"))
            {
                preset.densities.Add(preset.densities[^1]);
                preset.lengths.Add(preset.lengths[^1]);
            }
            PopGUITint();
            EditorGUILayout.EndHorizontal();

            preset.maxFarDistance = EditorGUILayout.DelayedFloatField("Max Far Distance", preset.maxFarDistance);

            if (preset.gridSnapUnits * 2 > preset.lengths[0])
            {
                if (preset.gridSnapUnits >= preset.lengths[0])
                    PushGUITint(1.0f, 0.5f, 0.5f);
                else
                    PushGUITint(1.0f, 1.0f, 0.5f);
                EditorGUILayout.LabelField("Snap unis (last L# density) should not be higher than half of L0 length.");
            } else PushGUITint(1.0f, 1.0f, 1.0f);
            GUI.enabled = false;
            EditorGUILayout.FloatField("Snap Units", preset.gridSnapUnits);
            PopGUITint();
            EditorGUILayout.LabelField($"Max Vertices: {maxVertices:N0}", EditorStyles.boldLabel);
            GUI.enabled = true;
        }

        private void DrawShaderStripping(ref List<ShaderKeywordStrip> list, string shaderName)
        {
            var updateList = new List<ShaderKeywordStrip>();

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth *= 2.0f;
            foreach (var kwField in typeof(Constants.Shader.Keyword).GetFields())
            {
                var attr = kwField.GetCustomAttribute<DragonShaderStrippableAttribute>();
                if (attr == null) continue;
                if (attr.Shader != shaderName) continue;

                var kw = (string)kwField.GetValue(null);
                var mode = list.FirstOrDefault(v => v.keyword == kw).mode;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox(attr.Description, MessageType.Info);
                mode = (KeywordStrippingMode)EditorGUILayout.EnumPopup(kw, mode);
                EditorGUILayout.EndVertical();

                updateList.Add(new() { keyword = kw, mode = mode });
            }
            EditorGUIUtility.labelWidth /= 2.0f;
            if (EditorGUI.EndChangeCheck())
            {
                list.Clear();
                list.AddRange(updateList);
            }
        }

        private int LayerField(string name, int current)
        {
            var layer = EditorGUILayout.LayerField(name, current);
            if (current != layer)
            {
                EditorUtility.DisplayDialog("Layer changed", "You have changed layer settings.\nIt's recommended to reload scene now to properly apply changes.", "OK");
                EditorApplication.delayCall += EditorBackgroundRunner.SetLayersVisibility;
            }
            return layer;
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }
    }
}
#endif