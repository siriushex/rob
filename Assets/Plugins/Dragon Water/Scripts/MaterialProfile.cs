using DragonWater.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DragonWater
{
    [CreateAssetMenu(menuName = "Dragon Water/Material Profile")]
    public class MaterialProfile : ScriptableObject
    {
        [Serializable]
        public enum TransparencyType
        {
            Opaque,
            Transparent,
            Refraction,
        }

        [Serializable]
        public enum ReflectionsMode
        {
            Off,
            Simple,
            DefaultEnvironmental,
            CustomCubemap
        }

        [Serializable]
        public class TextureDescription
        {
            public Texture2D texture;
            public float size = 10.0f;
            public float speed = 0.05f;
            [Range(0, 2)] public float strength = 1.0f;

            public Vector4 ParamsVector => new Vector4(1.0f / size, speed, strength);
        }

 
        [Serializable]
        public class FoamEdge
        {
            [Range(0, 3)] public float intensity = 1.0f;
            public float maxDepth = 10.0f;
            public float power = 2.0f;

            public Vector4 ParamsVector =>
                new Vector4(intensity,
                    maxDepth,
                    power,
                    0);
        }
        [Serializable]
        public class FoamHillness
        {
            [Range(0, 3)] public float intensity = 1.0f;
            [Range(-1, 2.0f)] public float hillnessThreshold = 0.5f;
            public float hillnessMultiplier = 1.5f;

            public Vector4 ParamsVector =>
                new Vector4(intensity,
                    hillnessThreshold,
                    hillnessMultiplier,
                    0);
            public Vector4 ParamsVectorWithSize(float size)
            {
                var v = ParamsVector;
                v.w = size;
                return v;
            }
        }
        [Serializable]
        public class FoamHillnessExtra
        {
            public Color color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            [Tooltip("Grayscale R-channel texture")]
            public Texture2D noiseTexture = null;
            public float noiseSize = 2.0f;

            [Range(0, 3)] public float intensity = 1.0f;
            [Range(-1, 2.0f)] public float hillnessThreshold = 0.5f;
            public float hillnessMultiplier = 1.5f;

            public Vector4 ParamsVector =>
                new Vector4(intensity,
                    hillnessThreshold,
                    hillnessMultiplier,
                    noiseSize);
        }

        [Serializable]
        public class RippleColor
        {
            public Color color = new Color(0.0f, 0.32f, 0.36f);
            [Range(0.0f, 1.0f)] public float attenuationStrength = 0.8f;
            [Range(-1.0f, 1.0f)] public float attenuationBias = 0.0f;

            public Vector4 ParamsVector =>
                new Vector4(attenuationStrength,
                    attenuationBias > 0 ? 1.0f + attenuationBias * 3.0f : 1.0f - attenuationBias * -0.75f,
                    0, 0);
        }
        [Serializable]
        public class RippleNoise
        {
            [Tooltip("Grayscale R-channel texture")]
            public Texture2D texture = null;
            public Color color = new Color(1.0f, 1.0f, 1.0f);
            public float scale = 10;
            [Range(0.0f, 1.0f)] public float attenuationStrength = 0.5f;
            [Range(-1.0f, 1.0f)] public float attenuationBias = 0.0f;

            public Vector4 ParamsVector =>
                new Vector4(attenuationStrength,
                    attenuationBias > 0 ? 1.0f + attenuationBias * 3.0f : 1.0f - attenuationBias * -0.75f,
                    1.0f / scale, 0);
        }
        [Serializable]
        public class RippleNormal
        {
            [Range(0.0f, 1.0f)] public float heightOffset = 0.25f;
            [Range(0.0f, 12.0f)] public float heightStrength = 8.0f;
            [Range(0.0f, 1.0f)] public float attenuationStrength = 0.7f;
            [Range(-1.0f, 1.0f)] public float attenuationBias = 0.0f;

            public Vector4 ParamsVector =>
                new Vector4(attenuationStrength,
                    attenuationBias > 0 ? 1.0f + attenuationBias * 3.0f : 1.0f - attenuationBias * -0.75f,
                    heightOffset, heightStrength);
        }
        [Serializable]
        public class RippleFoam
        {
            [Range(0.0f, 1.0f)] public float attenuationStrength = 1.0f;
            [Range(-1.0f, 1.0f)] public float attenuationBias = 0.0f;

            public Vector4 ParamsVector =>
                new Vector4(attenuationStrength,
                    attenuationBias > 0 ? 1.0f + attenuationBias * 3.0f : 1.0f - attenuationBias * -0.75f,
                    0, 0);
        }


        [Tooltip("Water depth.\nUsed for gradient between shallow and deep water.")]
        public float waterDepth = 3.0f;
        public Color shallowWaterColor = new Color(0.0f, 0.76f, 0.76f, 0.5f);
        public Color deepWaterColor = new Color(0.0f, 0.41f, 0.47f, 0.9f);

        [Range(0,1)] public float smoothness = 0.9f;
        [ColorUsage(false)] public Color specular = new Color(0.14f, 0.14f, 0.14f);
        public bool specularHighlights = true;

        [Tooltip("Used to lighten/darken color depending on hillness.\nKind of another layer of water deep.")]
        [Range(-1, 2)] public float colorNoiseHillnessOffset = 0.0f;
        [Tooltip("How much base color is lighten above hillness offset")]
        public float colorNoiseHillnessLighten = 0.1f;
        [Tooltip("How much base color is darken below hillness offset")]
        public float colorNoiseHillnessDarken = 0.2f;


        [Tooltip("If enabled, extra noise will be added on top of base color, based on two grayscale R-channel textures.")]
        public bool useColorNoiseTexture = false;
        public TextureDescription colorNoiseTexture1 = new() { size = 256.0f, speed = 0.0f, strength = 1.0f };
        public TextureDescription colorNoiseTexture2 = new() { size = 256.0f, speed = 0.05f, strength = 1.0f };
        public Color colorNoiseColor = new Color(0.0f, 0.32f, 0.36f);
        [Range(0, 2)] public float colorNoiseIntensity = 1.0f;
        public float colorNoiseEmission = 0.0f;


        [Range(0, 2)] public float sssGlobalIntensity = 1.0f;
        [Range(0, 1)] public float sssNormalInfluence = 0.2f;
        public float sssPower = 2.0f;
        [Tooltip("How much SSS affects output base color.")]
        public float sssOverlayStrength = 4.0f;
        [Tooltip("How much SSS affects output emissive.")]
        public float sssEmissionStrength = 0.5f;

        [Tooltip("Global strength that also affects waves normal.")]
        [Range(0, 1)] public float normalGlobalStrength = 0.5f;
        public bool useNormalMap = true;
        public TextureDescription normalMap1 = new() { size = 10.0f, speed = 0.05f, strength = 0.5f };
        public TextureDescription normalMap2 = new() { size = 15.0f, speed = -0.07f, strength = 0.5f };


        public bool useFoam = true;
        [Range(0, 2)] public float foamGlobalIntensity = 1.0f;
        [Tooltip("How much of output foam should go into emission")]
        public float foamEmissionRate = 0.0f;
        [Tooltip("Affects edge foam and 1st layer of hillness foam.")]
        public Color foamColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        [Tooltip("Grayscale R-channel texture.\nUsed for edge foam and 1st layer of hillness foam.")]
        public Texture2D foamNoiseTexture = null;
        [Tooltip("Size for noise texture.\nUsed for edge foam and 1st layer of hillness foam.")]
        public float foamNoiseSize = 2.0f;
        [Tooltip("How much foam affects output smoothness - depends on base smoothness value.")]
        [Range(-3, 3)] public float foamSmoothnessFactor = 0.0f;
        [Tooltip("How much foam affects output specular - depends on base specular value.")]
        [Range(-3, 3)] public float foamSpecularFactor = 0.0f;
        [Tooltip("How much normal will be attenuated by foam value.")]
        [Range(0, 2)] public float foamNormalAttenuation = 0.5f;
        [Tooltip("Higher value = higher foam values will attenaute normal")]
        [Range(-1, 1)] public float foamNormalAttenuationBias = 0.0f;
        public FoamEdge foamEdge = new();
        public FoamHillness foamHillness = new();
        [Tooltip("Adds extra two layers of hillness foam with their own texture and color")]
        public bool foamExtraLayers = false;
        public FoamHillnessExtra foamHillnessExtraLayer1 = new();
        public FoamHillnessExtra foamHillnessExtraLayer2 = new();


        public TransparencyType transparencyType = TransparencyType.Opaque;
        [Range(0, 1)] public float refractionStrength = 0.15f;
        [Tooltip("Determines distance, where strength becomes 100x lower.\nCan depends on camera FoV - set it so refraction looks same from both, near and far distance.\nDefault value of 100 is a good starting point.")]
        public float refractionDistanceFactor = 100.0f;


        public ReflectionsMode reflectionsMode = ReflectionsMode.DefaultEnvironmental;
        [Range(0, 2)] public float reflectionsIntensity = 1.0f;
        public Cubemap reflectionsCubemap = null;
        [ColorUsage(false)] public Color reflectionsGlossyTint = new Color(0, 0, 0);

        [Tooltip("How much ripple affects output smoothness - depends on base smoothness value.")]
        [Range(0, 2)] public float rippleSmoothnessFactor = 0.5f;
        [Tooltip("How much ripple affects output specular - depends on base specular value.")]
        [Range(0, 2)] public float rippleSpecularFactor = 0.0f;
        public RippleColor rippleColor = new();
        public RippleNoise rippleNoise = new();
        public RippleNormal rippleNormal = new();
        public RippleFoam rippleFoam = new();

        HashSet<Material> _filledMaterials = new();

        public void SetMaterialsDirty()
        {
            _filledMaterials.Clear();
        }
        public void SetMaterialDirty(Material material)
        {
            _filledMaterials.Remove(material);
        }

        public void ConfigureMaterial(Material material, bool force = false)
        {
            if (_filledMaterials.Contains(material) && !force)
                return;

            //material.EnableKeyword(Constants.Shader.Keyword.SpecularSetup);

            material.SetFloat(Constants.Shader.Property.WaterDepth, waterDepth);
            material.SetColor(Constants.Shader.Property.ShallowWaterColor, shallowWaterColor);
            material.SetColor(Constants.Shader.Property.DeepWaterColor, deepWaterColor);
            material.SetFloat(Constants.Shader.Property.Smoothness, smoothness);
            material.SetColor(Constants.Shader.Property.Specular, specular);

            if (specularHighlights)
                material.DisableKeyword(Constants.Shader.Keyword.SpecularHighlightsOff);
            else
                material.EnableKeyword(Constants.Shader.Keyword.SpecularHighlightsOff);

            material.SetVector(Constants.Shader.Property.ColorHillnessParams, new(colorNoiseHillnessOffset, colorNoiseHillnessLighten, colorNoiseHillnessDarken, 0));
           
            if (useColorNoiseTexture)
            {
                material.EnableKeyword(Constants.Shader.Keyword.UseColorNoiseTexture);

                material.SetColor(Constants.Shader.Property.ColorNoiseColor, colorNoiseColor);
                material.SetVector(Constants.Shader.Property.ColorNoiseParams, new(colorNoiseIntensity, colorNoiseEmission, 0, 0));

                material.SetTexture(Constants.Shader.Property.ColorNoiseTexture1, colorNoiseTexture1.texture);
                material.SetVector(Constants.Shader.Property.ColorNoiseTexture1Params, colorNoiseTexture1.ParamsVector);

                material.SetTexture(Constants.Shader.Property.ColorNoiseTexture2, colorNoiseTexture2.texture);
                material.SetVector(Constants.Shader.Property.ColorNoiseTexture2Params, colorNoiseTexture2.ParamsVector);
            }
            else
            {
                material.DisableKeyword(Constants.Shader.Keyword.UseColorNoiseTexture);
            }

            material.SetFloat(Constants.Shader.Property.SSSGlobalIntensity, sssGlobalIntensity);
            material.SetVector(Constants.Shader.Property.SSSParams, new(sssNormalInfluence, sssPower, sssOverlayStrength, sssEmissionStrength));

            material.SetFloat(Constants.Shader.Property.NormalGlobalStrength, normalGlobalStrength);
            if (useNormalMap)
            {
                material.EnableKeyword(Constants.Shader.Keyword.UseNormalMap);

                material.SetTexture(Constants.Shader.Property.NormalMap1, normalMap1.texture);
                material.SetVector(Constants.Shader.Property.NormalMap1Params, normalMap1.ParamsVector);

                material.SetTexture(Constants.Shader.Property.NormalMap2, normalMap2.texture);
                material.SetVector(Constants.Shader.Property.NormalMap2Params, normalMap2.ParamsVector);
            }
            else
            {
                material.DisableKeyword(Constants.Shader.Keyword.UseNormalMap);
            }


            if (useFoam)
            {
                if (foamExtraLayers)
                    material.SetKeywordEnum(Constants.Shader.Keyword.FoamMode, 2);
                else
                    material.SetKeywordEnum(Constants.Shader.Keyword.FoamMode, 1);

                material.SetColor(Constants.Shader.Property.FoamColor, foamColor);
                material.SetTexture(Constants.Shader.Property.FoamNoiseTexture, foamNoiseTexture);
                material.SetVector(Constants.Shader.Property.FoamParams, new Vector4(foamGlobalIntensity, foamSmoothnessFactor, foamSpecularFactor, foamEmissionRate));
                material.SetVector(Constants.Shader.Property.FoamNormalParams, new Vector4(
                    foamNormalAttenuation,
                    foamNormalAttenuationBias > 0 ? 1.0f + foamNormalAttenuationBias * 3.0f : 1.0f - foamNormalAttenuationBias * -0.75f,
                    0.0f, 0.0f
                    ));

                material.SetVector(Constants.Shader.Property.FoamEdgeParams, foamEdge.ParamsVector);
                material.SetVector(Constants.Shader.Property.FoamHillnessParams, foamHillness.ParamsVectorWithSize(foamNoiseSize));

                if (foamExtraLayers)
                {
                    material.SetColor(Constants.Shader.Property.FoamColorExtra1, foamHillnessExtraLayer1.color);
                    material.SetTexture(Constants.Shader.Property.FoamNoiseTextureExtra1, foamHillnessExtraLayer1.noiseTexture);
                    material.SetVector(Constants.Shader.Property.FoamHillnessParamsExtra1, foamHillnessExtraLayer1.ParamsVector);

                    material.SetColor(Constants.Shader.Property.FoamColorExtra2, foamHillnessExtraLayer2.color);
                    material.SetTexture(Constants.Shader.Property.FoamNoiseTextureExtra2, foamHillnessExtraLayer2.noiseTexture);
                    material.SetVector(Constants.Shader.Property.FoamHillnessParamsExtra2, foamHillnessExtraLayer2.ParamsVector);
                }
            }
            else
            {
                material.SetKeywordEnum(Constants.Shader.Keyword.FoamMode, 0);
            }


            if (transparencyType == TransparencyType.Opaque)
            {
                material.SetTransparent(false);
                material.DisableKeyword(Constants.Shader.Keyword.UseRefraction);
            }
            else if (transparencyType == TransparencyType.Transparent)
            {
                material.SetTransparent(true);
                material.DisableKeyword(Constants.Shader.Keyword.UseRefraction);
            }
            else if (transparencyType == TransparencyType.Refraction)
            {
                material.SetTransparent(false);
                material.EnableKeyword(Constants.Shader.Keyword.UseRefraction);

                material.SetVector(Constants.Shader.Property.RefractionParams, new Vector4(refractionStrength, refractionDistanceFactor, 0, 0));
            }


            if (reflectionsMode == ReflectionsMode.Off)
            {
                material.SetKeywordEnum(Constants.Shader.Keyword.Reflections, 0);
                material.SetColor(Constants.Shader.Property.ReflectionsGlossyTint, Color.clear);
            }
            else if (reflectionsMode == ReflectionsMode.Simple)
            {
                material.SetKeywordEnum(Constants.Shader.Keyword.Reflections, 0);
                material.SetColor(Constants.Shader.Property.ReflectionsGlossyTint, reflectionsGlossyTint);
            }
            else if (reflectionsMode == ReflectionsMode.DefaultEnvironmental)
            {
                material.SetKeywordEnum(Constants.Shader.Keyword.Reflections, 1);
                material.SetFloat(Constants.Shader.Property.ReflectionsGIIntensity, reflectionsIntensity);
            }
            else if (reflectionsMode == ReflectionsMode.CustomCubemap)
            {
                material.SetKeywordEnum(Constants.Shader.Keyword.Reflections, 2);
                material.SetFloat(Constants.Shader.Property.ReflectionsGIIntensity, reflectionsIntensity);
                material.SetTexture(Constants.Shader.Property.ReflectionsGICubemap, reflectionsCubemap);
            }


            material.SetVector(Constants.Shader.Property.RippleMaterialParams, new Vector4(0, 0, rippleSmoothnessFactor, rippleSpecularFactor));
            material.SetColor(Constants.Shader.Property.RippleColorColor, rippleColor.color);
            material.SetVector(Constants.Shader.Property.RippleColorParams, rippleColor.ParamsVector);
            material.SetVector(Constants.Shader.Property.RippleNormalParams, rippleNormal.ParamsVector);
            material.SetVector(Constants.Shader.Property.RippleFoamParams, rippleFoam.ParamsVector);
            material.SetTexture(Constants.Shader.Property.RippleNoiseTexture, rippleNoise.texture);
            material.SetColor(Constants.Shader.Property.RippleNoiseColor, rippleNoise.color);
            material.SetVector(Constants.Shader.Property.RippleNoiseParams, rippleNoise.ParamsVector);

            _filledMaterials.Add(material);
        }
    }
}
