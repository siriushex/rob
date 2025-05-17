using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DragonWater
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct LocalWaveAreaEntry
    {
        public const int SIZEOF = 7 * 4;

        public Vector2 position;
        public Vector2 radius;
        public Vector3 influences; // x=amplitude, y=steepness, z=hillness
    }

    [AddComponentMenu("Dragon Water/Local Wave Area")]
    [ExecuteAlways]
    public class LocalWaveArea : MonoBehaviour
    {
        [SerializeField] internal float radius = 100.0f;
        [SerializeField] [Range(0.0f, 1.0f)] internal float innerRadiusRatio = 0.6f;
        [SerializeField] internal float amplitudeMultiplier = 1.0f;
        [SerializeField] internal float steepnessMultiplier = 1.0f;
        [SerializeField] internal float hillnessMultiplier = 1.0f;

        #region main properties
        public float Radius { get => radius; set => radius = value; }
        public float InnerRadiusRatio { get => innerRadiusRatio; set => innerRadiusRatio = value; }
        public float AmplitudeMultiplier { get => amplitudeMultiplier; set => amplitudeMultiplier = value; }
        public float SteepnessMultiplier { get => steepnessMultiplier; set => steepnessMultiplier = value; }
        public float HillnessMultiplier { get => hillnessMultiplier; set => hillnessMultiplier = value; }
        #endregion


        private void OnEnable()
        {
            DragonWaterManager.Instance.RegisterLocalWaveArea(this);
        }
        private void OnDisable()
        {
            DragonWaterManager.InstanceUnsafe?.UnregisterLocalWaveArea(this);
        }


        public LocalWaveAreaEntry GetEntry()
        {
            var position = transform.position;
            return new()
            {
                position = new(position.x, position.z),
                radius = new(radius, innerRadiusRatio),
                influences = new(amplitudeMultiplier, steepnessMultiplier, hillnessMultiplier)
            };
        }


#if UNITY_EDITOR
        // in editor mode, force update activeness in manager in case of script reload
        private void Update()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (enabled && !DragonWaterManager.Instance.LocalWaveAreas.Contains(this))
                {
                    enabled = false;
                    enabled = true;
                }
            }
        }
#endif

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = Color.white * 0.75f;
            Gizmos.DrawWireSphere(transform.position, radius * innerRadiusRatio);
        }
#endif
    }
}
