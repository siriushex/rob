#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(WaterSurface))]
    internal class WaterSurfaceDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            var surface = (WaterSurface)target;

            if (surface.transform.lossyScale != Vector3.one)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Final scale of this surface transform is not 1.\nFix this in your hierachy or expect potential problems with this water surface.", MessageType.Warning);
                EditorGUILayout.Space();
            }

            DrawTitle("Geometry");
            EditorGUI.BeginChangeCheck();
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.geometryType)));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (surface.geometryType)
            {
                case WaterSurface.GeometryMeshType.Rectangle:
                    PropertyField(serializedObject.FindProperty(nameof(WaterSurface.geometryRectangleConfig)), "Rectangle Config");
                    surface.geometryRectangleConfig.SafeClamp();
                    break;
                case WaterSurface.GeometryMeshType.Circle:
                    PropertyField(serializedObject.FindProperty(nameof(WaterSurface.geometryCircleConfig)), "Circle Config");
                    surface.geometryCircleConfig.SafeClamp();
                    break;
                case WaterSurface.GeometryMeshType.InfiniteOcean:
                    {
                        var names = DragonWaterManager.Instance.Config.oceanQualities.Select(v => v.name).ToArray();
                        if (string.IsNullOrEmpty(surface.geometryInfiniteOceanPreset))
                        {
                            surface.geometryInfiniteOceanPreset = names.FirstOrDefault();
                        }
                        var index = ArrayUtility.IndexOf(names, surface.geometryInfiniteOceanPreset);
                        var newIndex = EditorGUILayout.Popup("Quality Preset", index, names);
                        if (newIndex != index)
                        {
                            serializedObject.FindProperty(nameof(WaterSurface.geometryInfiniteOceanPreset)).stringValue = names[newIndex];
                        }

                        PropertyField(serializedObject.FindProperty(nameof(WaterSurface.geometryInfiniteOceanAttachTo)), "Attach To");
                        if (surface.geometryInfiniteOceanAttachTo == WaterSurface.OceanAttachTarget.CustomObject)
                        {
                            EditorGUI.indentLevel++;
                            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.geometryInfiniteOceanAttachCustomTarget)), "Target");
                            if (surface.geometryInfiniteOceanAttachCustomTarget == null)
                                EditorGUILayout.HelpBox("No custom target specified. Main Camera will be used instead.", MessageType.Warning);
                            EditorGUI.indentLevel--;
                        }
                    }
                    break;
                case WaterSurface.GeometryMeshType.CustomMesh:
                    PropertyField(serializedObject.FindProperty(nameof(WaterSurface.geometryCustomMesh)), "Mesh");
                    break;
            }
            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                surface.InvalidateMesh();
            }


            EditorGUILayout.Space();
            DrawTitle("Renderer");
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.waveProfile)));
            if (surface.waveProfile != null)
            {
                EditorGUI.indentLevel++;
                PropertyField(serializedObject.FindProperty(nameof(WaterSurface.synchronizeWorldOrigin)));
                if (surface.synchronizeWorldOrigin)
                {
                    EditorGUILayout.HelpBox("If origin synchronization is enabled, this wave profile will work correctly only with this surface and may not be reusable.\nThis option is usefull only for moving surfaces.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.materialProfile)));
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.underwaterProfile)));

            EditorGUILayout.Space();
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.cutoutWaterVolume)));
            if (surface.cutoutWaterVolume)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                PropertyField(serializedObject.FindProperty(nameof(WaterSurface.cutoutReverse)));
                PropertyField(serializedObject.FindProperty(nameof(WaterSurface.cutoutCulling)));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.useRipple)));
            if (surface.useRipple)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (surface.rippleProjector == null)
                {
                    EditorGUILayout.HelpBox("In order to have ripple working, assign a projector.", MessageType.Warning);
                }
                PropertyField(serializedObject.FindProperty(nameof(WaterSurface.rippleProjector)));
                EditorGUILayout.EndVertical();
            }


            EditorGUILayout.Space();
            DrawTitle("Boundaries");
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.maxHeight)));
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.maxDepth)));


            EditorGUILayout.Space();
            DrawTitle("Physics");
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.physicsBuoyancyFactor)), "Buoyancy Factor");
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.physicsExtraDrag)), "Extra Drag");
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.physicsExtraAngularDrag)), "Extra Angular Drag");


            EditorGUILayout.Space();
            DrawTitle("Default Water Surface");
            if (surface.isDefaultSurface && DragonWaterManager.Instance.DefaultSurface == surface)
                EditorGUILayout.HelpBox("This is default water surface that is going to be used in first place in calculations.", MessageType.Info);
            if (DragonWaterManager.Instance.Surfaces.Count(s => s.isDefaultSurface) > 1)
                EditorGUILayout.HelpBox("More than 2 default waters detected!", MessageType.Warning);
            PropertyField(serializedObject.FindProperty(nameof(WaterSurface.isDefaultSurface)));
        }

        protected override void OnChanges()
        {
            var surface = (WaterSurface)target;

            if (surface.materialProfile != null && surface.Material != null)
                surface.materialProfile.SetMaterialDirty(surface.Material);

            DragonWaterManager.Instance.UpdateDefaultSurface();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }

        protected override void DrawInspectorDebug()
        {
            var surface = (WaterSurface)target;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField("Material", surface.Material, typeof(Material), false);
            EditorGUILayout.ObjectField("Default Surface", DragonWaterManager.Instance.DefaultSurface, typeof(WaterSurface), false);
            EditorGUILayout.EndVertical();
        }
    }
}
#endif