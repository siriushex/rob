using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater
{
    [CreateAssetMenu(menuName = "Dragon Water/Underwater Profile")]
    public class UnderwaterProfile : ScriptableObject
    {
        [Serializable]
        public class FogConfig
        {
            public bool fog = true;
            public Color fogColor = new Color(0.4f, 0.55f, 0.7f);
            public FogMode fogMode = FogMode.ExponentialSquared;
            public float fogDensity = 0.01f;
            public float fogStartDistance = 0;
            public float fogEndDistance = 300;

            public void Apply()
            {
                RenderSettings.fog = fog;
                if (fog)
                {
                    RenderSettings.fogColor = fogColor;
                    RenderSettings.fogMode = fogMode;
                    if (fogMode == FogMode.Linear)
                    {
                        RenderSettings.fogStartDistance = fogStartDistance;
                        RenderSettings.fogEndDistance = fogEndDistance;
                    }
                    else
                    {
                        RenderSettings.fogDensity = fogDensity;
                    }
                }
            }

            public static FogConfig GetCurrent()
            {
                return new()
                {
                    fog = RenderSettings.fog,
                    fogColor = RenderSettings.fogColor,
                    fogMode = RenderSettings.fogMode,
                    fogDensity = RenderSettings.fogDensity,
                    fogStartDistance = RenderSettings.fogStartDistance,
                    fogEndDistance = RenderSettings.fogEndDistance
                };
            }
        }

        [Serializable]
        public class CausticTextureDescription
        {
            [Tooltip("Grayscale R-channel texture")]
            public Texture texture;
            public float size = 10.0f;
            public float speed = 0.05f;

            public Vector4 ParamsVector => new Vector4(1.0f / size, speed, 0, 0);
        }


        [Tooltip("Override built-in fog parameters when underwater")]
        public bool overrideFog;
        public FogConfig fogConfig;
        [Tooltip("Default fog is not rendered if there is no geometry on screen.\nIf your underwater scene isn't filled with geometry, you can enable this to draw fake flat plane at the end of camera visibility frustum.")]
        public bool showSuperfarPlane;


        [Tooltip("Show rays of volumetric lighting?\nNOTE: It's quite computation heavy, try to keep low density.")]
        public bool showGodRays;
        [Tooltip("Tint is also blended with main directional light color.")]
        public Color raysTint = new Color(0.0f, 0.41f, 0.47f);
        public float raysLength = 50.0f;
        public Vector2 raysWidthRange = new Vector2(3.0f, 10.0f);
        [Range(0.0f, 1.0f)] public float raysIntensity = 0.2f;
        [Tooltip("Warning: this setting has high impact on performance")]
        [Range(0.0f, 1.0f)] public float raysDensity = 0.5f;
        [Tooltip("Warning: this setting has high impact on performance")]
        public float raysDistance = 120.0f;
        public float raysDepthFade = 20.0f;
        public float raysCameraFade = 20.0f;
        public float raysWaterFade = 2.0f;
        [Tooltip("If enabled, rays will be occluded by shadowed areas.")]
        public bool raysOccludeByShadows = false;


        [Tooltip("Show screen-space caustics effect?")]
        public bool showCaustics;
        public Color causticsTint = new Color(0.0f, 0.41f, 0.47f);
        [Range(0.0f, 2.0f)] public float causticsIntensity = 1.0f;
        public CausticTextureDescription causticsTexture1 = new() { size = 5, speed = 0.05f };
        public CausticTextureDescription causticsTexture2 = new() { size = -6.165f, speed = -0.033f};
        public float causticsDistance = 70.0f;
        public float causticsDepth = 70.0f;
        public float causticsWaterFade = 2.0f;
        public float causticsWaterLevelOffset = 1.0f;
        [Tooltip("If enabled, caustics will be attenuated by shadowed areas.")]
        public bool causticsOccludeByShadows = false;
        [Range(0.0f, 1.0f)] public float causticsShadowsAttenuation = 0.75f;
        [Tooltip("Chromatic aberration split RGB channels. It may be computation heavy.")]
        public bool causticsChromaticAberration = false;
        [Range(0.0f, 1.0f)] public float causticsChromaticAberrationStrength = 0.25f;


        [Tooltip("Override global URP post-processing volume?")]
        public bool overrideVolume;
        public VolumeProfile volumeProfile;
        [Range(0.0f, 1.0f)] public float volumeWeight = 1.0f;


        [NonSerialized, HideInInspector] FogConfig _originalFog = null;
        internal void Show()
        {
            var renderer = DragonWaterManager.Instance.UnderwaterRenderer;

            if (overrideFog)
            {
                _originalFog = FogConfig.GetCurrent();
                fogConfig.Apply();
            }
            renderer.ShowSuperfarPlane = showSuperfarPlane;

            if (showGodRays)
            {
                renderer.ShowRays = true;
                renderer.RayVisibilityDistance = raysDistance;
                renderer.RayLength = raysLength;
                renderer.RayWidth = raysWidthRange;
                renderer.RayDensityFactor = raysDensity;
                renderer.RayMaterial.SetColor(Constants.Shader.Property.WaterColor, raysTint);
                renderer.RayMaterial.SetFloat(Constants.Shader.Property.Intensity, raysIntensity);
                renderer.RayMaterial.SetFloat(Constants.Shader.Property.DepthFade, raysDepthFade);
                renderer.RayMaterial.SetFloat(Constants.Shader.Property.CameraFade, raysCameraFade);
                renderer.RayMaterial.SetFloat(Constants.Shader.Property.WaterFade, raysWaterFade);
                renderer.RayMaterial.SetFloat(Constants.Shader.Property.MaxVisibilityDistance, raysDistance);

                if (raysOccludeByShadows)
                    renderer.RayMaterial.EnableKeyword(Constants.Shader.Keyword.OccludeByShadows);
                else
                    renderer.RayMaterial.DisableKeyword(Constants.Shader.Keyword.OccludeByShadows);
            }

            if (showCaustics)
            {
                renderer.ShowCaustics = true;
                renderer.CausticsWaterLevelOffset = causticsWaterLevelOffset;

                renderer.CausticsMaterial.SetColor(Constants.Shader.Property.WaterColor, causticsTint);
                renderer.CausticsMaterial.SetFloat(Constants.Shader.Property.Intensity, causticsIntensity);

                renderer.CausticsMaterial.SetTexture(Constants.Shader.Property.CausticsTexture1, causticsTexture1.texture);
                renderer.CausticsMaterial.SetVector(Constants.Shader.Property.CausticsTexture1Params, causticsTexture1.ParamsVector);
                renderer.CausticsMaterial.SetTexture(Constants.Shader.Property.CausticsTexture2, causticsTexture2.texture);
                renderer.CausticsMaterial.SetVector(Constants.Shader.Property.CausticsTexture2Params, causticsTexture2.ParamsVector);

                renderer.CausticsMaterial.SetFloat(Constants.Shader.Property.MaxVisibilityDistance, causticsDistance);
                renderer.CausticsMaterial.SetFloat(Constants.Shader.Property.MaxVisibilityDepth, causticsDepth);
                renderer.CausticsMaterial.SetFloat(Constants.Shader.Property.WaterFade, causticsWaterFade);

                if (causticsChromaticAberration)
                {
                    renderer.CausticsMaterial.EnableKeyword(Constants.Shader.Keyword.UseChromaticAberration);
                    renderer.CausticsMaterial.SetFloat(Constants.Shader.Property.ChromaticAberrationStrength, causticsChromaticAberrationStrength * 0.02f);
                }
                else
                {
                    renderer.CausticsMaterial.DisableKeyword(Constants.Shader.Keyword.UseChromaticAberration);
                }

                if (causticsOccludeByShadows)
                {
                    renderer.CausticsMaterial.EnableKeyword(Constants.Shader.Keyword.OccludeByShadows);
                    renderer.CausticsMaterial.SetFloat(Constants.Shader.Property.ShadowsAttenuation, causticsShadowsAttenuation);
                }
                else
                {
                    renderer.CausticsMaterial.DisableKeyword(Constants.Shader.Keyword.OccludeByShadows);
                }
            }

            if (overrideVolume)
            {
                renderer.OverrideVolume.enabled = true;
                renderer.OverrideVolume.sharedProfile = volumeProfile;
                renderer.OverrideVolume.weight = volumeWeight;
            }
        }

        internal void Hide()
        {
            var renderer = DragonWaterManager.Instance.UnderwaterRenderer;

            if (_originalFog != null)
            {
                _originalFog.Apply();
                _originalFog = null;
            }

            renderer.ShowSuperfarPlane = false;
            renderer.ShowRays = false;
            renderer.ShowCaustics = false;
            renderer.OverrideVolume.enabled = false;
        }
    }
}
