using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using DragonWater.Editor;
using DragonWater.Editor.Build;
#endif

namespace DragonWater
{
    [Serializable]
    public class OceanMeshQualityPreset
    {
        public string name = "";
        public List<float> densities = new();
        public List<float> lengths = new();
        public float maxFarDistance = 1000.0f;
        public int gridSnapUnits = 1;

        public override int GetHashCode()
        {
            return $"{maxFarDistance},{gridSnapUnits},{string.Join(':', densities)},{string.Join(':', lengths)}".GetHashCode();
        }
    }

    public class DragonWaterConfig : ScriptableObject
    {
        [Serializable]
        internal enum DebugLevel
        {
            InfoWarningError,
            WarningError,
            Error
        }

        [SerializeField] internal string version = "<new>";

        [Tooltip("If enabled, DragonWaterManager.Instance.Time will be incremented automatically with Time.deltaTime.\nDisable it if you want to process this value manually.")]
        [SerializeField] internal bool autoTimeSimulation = true;
        [SerializeField] internal List<OceanMeshQualityPreset> oceanQualities = new();
        [SerializeField] internal int waterRendererLayer = -1;
        [SerializeField] internal int cutoutLayer = -1;
        [SerializeField] internal int underwaterVolumeLayer = 0;
        [Tooltip("Underwater global URP volume will be created with this priority.")]
        [SerializeField] internal int underwaterVolumePriority = 10;
        [SerializeField] internal int rippleLayer = -1;
        [Tooltip("Index of URP renderer that will be used by ripple projector.")]
        [SerializeField] internal int rippleURPRendererIndex = 1;

        [SerializeField] internal DebugLevel debugLevel = DebugLevel.InfoWarningError;
        [SerializeField] internal bool drawDebugInspectors = false;

#if UNITY_EDITOR
        [SerializeField] internal List<ShaderKeywordStrip> buildWaterShaderStripping = new();
#endif

        public string Version => version;
        public bool AutoTimeSimulation => autoTimeSimulation;

        public int WaterRendererLayer => waterRendererLayer;
        public int WaterRendererMask => 1 << waterRendererLayer;

        public int CutoutLayer => cutoutLayer;
        public int CutoutMask => 1 << cutoutLayer;

        public int UnderwaterVolumeLayer => underwaterVolumeLayer;
        public int UnderwaterVolumePriority => underwaterVolumePriority;

        public int RippleLayer => rippleLayer;
        public int RippleMask => 1 << rippleLayer;
        public int RippleURPRendererIndex => rippleURPRendererIndex;


        private void Reset()
        {
            oceanQualities = new()
            {
                new() { name = "Low", densities = new() { 1f, 2f, 4f, 8f, 16f }, lengths = new() { 64, 96, 128, 256, 512 }, maxFarDistance = 5000, },
                new() { name = "Low Far", densities = new() { 1f, 4f, 16f }, lengths = new() { 160, 256, 512 }, maxFarDistance = 5000, },
                new() { name = "Medium", densities = new() { 0.5f, 1f, 2f, 8f, 16f }, lengths = new() { 64, 96, 128, 256, 512 }, maxFarDistance = 5000, },
                new() { name = "Medium Far", densities = new() { 0.5f, 2f, 16f }, lengths = new() { 160, 256, 512 }, maxFarDistance = 5000, },
                new() { name = "High", densities = new() { 0.25f, 0.5f, 2f, 8f, 16f }, lengths = new() { 64, 96, 128, 256, 512 }, maxFarDistance = 5000 },
                new() { name = "High Far", densities = new() { 0.25f, 1.0f, 16f }, lengths = new() { 160, 256, 512 }, maxFarDistance = 5000 },
                new() { name = "Top Down", densities = new() { 1f }, lengths = new() { 50 }, maxFarDistance = 1000 },
            };

            OnValidate();
        }

        private void OnEnable()
        {
            if (version != Constants.Version)
            {
                version = Constants.Version;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (waterRendererLayer == -1) waterRendererLayer = Layers.InstallLayer("Dragon Water Renderer");
            if (cutoutLayer == -1) cutoutLayer = Layers.InstallLayer("Dragon Water Cutout");
            if (rippleLayer == -1) rippleLayer = Layers.InstallLayer("Dragon Water Ripple");
#endif
        }

        public OceanMeshQualityPreset GetOceanQualityPreset(string presetName)
        {
            return oceanQualities.Find(oq => oq.name == presetName);
        }
    }
}
