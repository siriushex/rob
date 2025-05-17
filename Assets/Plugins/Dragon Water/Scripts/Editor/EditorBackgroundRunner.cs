#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DragonWater.Editor
{
    internal static class EditorBackgroundRunner
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SetLayersVisibility();
        }


        internal static void SetLayersVisibility()
        {
            var mask = Tools.visibleLayers;

            foreach (var layer in new int[]
            {
                DragonWaterManager.Instance.Config.RippleMask
            })
            {
                if ((mask & layer) > 0)
                {
                    mask ^= layer;
                    Tools.visibleLayers = mask;
                }
            }

            foreach (var layer in new int[]
            {
                DragonWaterManager.Instance.Config.WaterRendererMask,
                DragonWaterManager.Instance.Config.CutoutMask,
            })
            {
                if ((mask & layer) == 0)
                {
                    mask |= layer;
                    Tools.visibleLayers = mask;
                }
            }
        }
    }
}
#endif