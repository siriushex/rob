#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(WaterCutoutVolume))]
    internal class WaterCutoutVolumeDrawer : EditorEx
    {
        protected override void DrawInspector()
        {
            var volume = target as WaterCutoutVolume;

            EditorGUILayout.BeginVertical();
            volume.Shape = (WaterCutoutVolume.CutoutShape)EditorGUILayout.EnumPopup("Shape", volume.shape);
            switch (volume.Shape)
            {
                case WaterCutoutVolume.CutoutShape.Sphere:
                    EditorGUILayout.HelpBox("Configure SphereCollider below.", MessageType.Info);
                    break;
                case WaterCutoutVolume.CutoutShape.Box:
                    EditorGUILayout.HelpBox("Configure BoxCollider below.", MessageType.Info);
                    break;
                case WaterCutoutVolume.CutoutShape.CustomMesh:
                    EditorGUILayout.HelpBox("Configure MeshCollider below.", MessageType.Info);
                    EditorGUILayout.HelpBox("Make sure your source mesh is by default already:\n- simple\n- convex\n- single-sided", MessageType.Warning);
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }
    }
}
#endif