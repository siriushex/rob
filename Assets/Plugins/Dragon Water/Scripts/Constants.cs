using DragonWater.Attributes;
using UnityEngine;

namespace DragonWater
{
    internal static class Constants
    {
        public static readonly string Version = "1.0.0";

        public static readonly string PluginPath = "Assets/Plugins/Dragon Water";
        public static readonly string ResourcesPath = PluginPath + "/Resources";

        public static readonly int MaxLocalWaveAreas = 32;
        public static readonly int DefaultSamplerPrecision = 4;

        public static class Shader
        {
            // custom
            public const string WaterShaderName = "Hidden/Dragon Water";
            public const string CutoutShaderName = "Hidden/Dragon Water Cutout";
            public const string RippleCasterShaderName = "Hidden/Dragon Ripple Caster";
            public const string UnderwaterRayShaderName = "Hidden/Dragon Underwater Ray";
            public const string UnderwaterCausticsShaderName = "Hidden/Dragon Underwater Caustics";

            // compute
            public static readonly ComputeShader WaveCompute = Resources.Load<ComputeShader>("DragonWater/Shader/Wave Compute");
            public static readonly ComputeShader RippleCompute = Resources.Load<ComputeShader>("DragonWater/Shader/Ripple Compute");

            public static class Keyword
            {
                // built-in
                public const string SpecularSetup = "_SPECULAR_SETUP";
                public const string SurfaceTypeTransparent = "_SURFACE_TYPE_TRANSPARENT";
                [DragonShaderStrippable(WaterShaderName, "Used if surface have enabled water cutout")]
                public const string AlphaTestOn = "_ALPHATEST_ON";
                //public static readonly string EnvironmentReflectionsOff = "_ENVIRONMENTREFLECTIONS_OFF";
                [DragonShaderStrippable(WaterShaderName, "Used if you DO NOT have enabled specular highlights")]
                public const string SpecularHighlightsOff = "_SPECULARHIGHLIGHTS_OFF";

                // main
                //public static readonly string UseWaveTexture = "_USE_WAVE_TEXTURE";
                [DragonShaderStrippable(WaterShaderName, "Used for color noise textures")]
                public const string UseColorNoiseTexture = "_USE_COLOR_NOISE_TEXTURE";
                [DragonShaderStrippable(WaterShaderName, "Used for normal mapping")]
                public const string UseNormalMap = "_USE_NORMAL_MAP";
                [DragonShaderStrippable(WaterShaderName, "Used by 'Refraction' transparency type")]
                public const string UseRefraction = "_USE_REFRACTION";
                [DragonShaderStrippable(WaterShaderName, "Used if surface have configured ripple projector")]
                public const string UseRipple = "_USE_RIPPLE";

                // underwater
                public const string OccludeByShadows = "_OCCLUDE_BY_SHADOWS";
                public const string UseChromaticAberration = "_USE_CHROMATIC_ABERRATION";

                // enum
                [DragonShaderStrippable(WaterShaderName, "Used if foam is disabled")]
                public const string FoamModeOff = "_FOAM_MODE_OFF";
                [DragonShaderStrippable(WaterShaderName, "Used if foam is enabled WITHOUT extra hillness layers")]
                public const string FoamModeOn = "_FOAM_MODE_ON";
                [DragonShaderStrippable(WaterShaderName, "Used if foam is enabled, but WITH extra hillness layers")]
                public const string FoamModeOnExtra = "_FOAM_MODE_ON_EXTRA";
                public static readonly string[] FoamMode = new[] { FoamModeOff, FoamModeOn, FoamModeOnExtra };

                [DragonShaderStrippable(WaterShaderName, "Used if reflections are in simple OR off mode")]
                public const string ReflectionsSimple = "_REFLECTIONS_SIMPLE";
                [DragonShaderStrippable(WaterShaderName, "Used if reflections are in default mode")]
                public const string ReflectionsNormal = "_REFLECTIONS_NORMAL";
                [DragonShaderStrippable(WaterShaderName, "Used if reflections are in cubemap override mode")]
                public const string ReflectionsCubemap = "_REFLECTIONS_CUBEMAP";
                public static readonly string[] Reflections = new[] { ReflectionsSimple, ReflectionsNormal, ReflectionsCubemap };


                // compute
                public const string ComputeCalculateHeightOffset = "_CALCULATE_HEIGHT_OFFSET";

                // compute enum
                public const string ComputePrecisionHeightOffset = "_PRECISION_HEIGHT_OFFSET";
                public const string ComputePrecisionSimple = "_PRECISION_SIMPLE";
                public const string ComputePrecisionFlat = "_PRECISION_FLAT";
                public static readonly string[] ComputePrecision = new[] { ComputePrecisionHeightOffset, ComputePrecisionSimple, ComputePrecisionFlat };
            }
            public static class Property
            {
                // built-in
                public static readonly int Surface = UnityEngine.Shader.PropertyToID("_Surface");
                public static readonly int SrcBlend = UnityEngine.Shader.PropertyToID("_SrcBlend");
                public static readonly int DstBlend = UnityEngine.Shader.PropertyToID("_DstBlend");
                public static readonly int AlphaClip = UnityEngine.Shader.PropertyToID("_AlphaClip");

                // main
                public static readonly int WorldOriginOffset = UnityEngine.Shader.PropertyToID("_World_Origin_Offset");
                
                public static readonly int WaveTextureOffset = UnityEngine.Shader.PropertyToID("_Wave_Texture_Offset");
                public static readonly int WaveTextureNormal = UnityEngine.Shader.PropertyToID("_Wave_Texture_Normal");
                public static readonly int WaveTextureProjection = UnityEngine.Shader.PropertyToID("_Wave_Texture_Projection");

                public static readonly int WaterDepth = UnityEngine.Shader.PropertyToID("_Water_Depth");
                public static readonly int ShallowWaterColor = UnityEngine.Shader.PropertyToID("_Shallow_Water_Color");
                public static readonly int DeepWaterColor = UnityEngine.Shader.PropertyToID("_Deep_Water_Color");
                public static readonly int Smoothness = UnityEngine.Shader.PropertyToID("_Smoothness");
                public static readonly int Specular = UnityEngine.Shader.PropertyToID("_Specular");

                public static readonly int ColorHillnessParams = UnityEngine.Shader.PropertyToID("_Color_Hillness_Params");
                public static readonly int ColorNoiseColor = UnityEngine.Shader.PropertyToID("_Color_Noise_Color");
                public static readonly int ColorNoiseParams = UnityEngine.Shader.PropertyToID("_Color_Noise_Params");
                public static readonly int ColorNoiseTexture1 = UnityEngine.Shader.PropertyToID("_Color_Noise_Texture_1");
                public static readonly int ColorNoiseTexture1Params = UnityEngine.Shader.PropertyToID("_Color_Noise_Texture_1_Params");
                public static readonly int ColorNoiseTexture2 = UnityEngine.Shader.PropertyToID("_Color_Noise_Texture_2");
                public static readonly int ColorNoiseTexture2Params = UnityEngine.Shader.PropertyToID("_Color_Noise_Texture_2_Params");

                public static readonly int SSSGlobalIntensity = UnityEngine.Shader.PropertyToID("_SSS_Global_Intensity");
                public static readonly int SSSParams = UnityEngine.Shader.PropertyToID("_SSS_Params");

                public static readonly int NormalGlobalStrength = UnityEngine.Shader.PropertyToID("_Normal_Global_Strength");
                public static readonly int NormalMap1 = UnityEngine.Shader.PropertyToID("_Normal_Map_1");
                public static readonly int NormalMap1Params = UnityEngine.Shader.PropertyToID("_Normal_Map_1_Params");
                public static readonly int NormalMap2 = UnityEngine.Shader.PropertyToID("_Normal_Map_2");
                public static readonly int NormalMap2Params = UnityEngine.Shader.PropertyToID("_Normal_Map_2_Params");

                public static readonly int FoamColor = UnityEngine.Shader.PropertyToID("_Foam_Color");
                public static readonly int FoamNoiseTexture = UnityEngine.Shader.PropertyToID("_Foam_Noise_Texture");
                public static readonly int FoamParams = UnityEngine.Shader.PropertyToID("_Foam_Params");
                public static readonly int FoamNormalParams = UnityEngine.Shader.PropertyToID("_Foam_Normal_Params");
                public static readonly int FoamHillnessParams = UnityEngine.Shader.PropertyToID("_Foam_Hillness_Params");
                public static readonly int FoamEdgeParams = UnityEngine.Shader.PropertyToID("_Foam_Edge_Params");

                public static readonly int FoamColorExtra1 = UnityEngine.Shader.PropertyToID("_Foam_Color_Extra_1");
                public static readonly int FoamColorExtra2 = UnityEngine.Shader.PropertyToID("_Foam_Color_Extra_2");
                public static readonly int FoamNoiseTextureExtra1 = UnityEngine.Shader.PropertyToID("_Foam_Noise_Texture_Extra_1");
                public static readonly int FoamNoiseTextureExtra2 = UnityEngine.Shader.PropertyToID("_Foam_Noise_Texture_Extra_2");
                public static readonly int FoamHillnessParamsExtra1 = UnityEngine.Shader.PropertyToID("_Foam_Hillness_Params_Extra_1");
                public static readonly int FoamHillnessParamsExtra2 = UnityEngine.Shader.PropertyToID("_Foam_Hillness_Params_Extra_2");

                public static readonly int RefractionParams = UnityEngine.Shader.PropertyToID("_Refraction_Params");

                public static readonly int ReflectionsGIIntensity = UnityEngine.Shader.PropertyToID("_Reflections_GI_Intensity");
                public static readonly int ReflectionsGICubemap = UnityEngine.Shader.PropertyToID("_Reflections_GI_Cubemap");
                public static readonly int ReflectionsGlossyTint = UnityEngine.Shader.PropertyToID("_Reflections_Glossy_Tint");

                public static readonly int RippleTexture = UnityEngine.Shader.PropertyToID("_Ripple_Texture");
                public static readonly int RippleProjection = UnityEngine.Shader.PropertyToID("_Ripple_Projection");
                public static readonly int RippleMaterialParams = UnityEngine.Shader.PropertyToID("_Ripple_Material_Params");
                public static readonly int RippleColorColor = UnityEngine.Shader.PropertyToID("_Ripple_Color_Color");
                public static readonly int RippleColorParams = UnityEngine.Shader.PropertyToID("_Ripple_Color_Params");
                public static readonly int RippleNormalParams = UnityEngine.Shader.PropertyToID("_Ripple_Normal_Params");
                public static readonly int RippleFoamParams = UnityEngine.Shader.PropertyToID("_Ripple_Foam_Params");
                public static readonly int RippleNoiseTexture = UnityEngine.Shader.PropertyToID("_Ripple_Noise_Texture");
                public static readonly int RippleNoiseColor = UnityEngine.Shader.PropertyToID("_Ripple_Noise_Color");
                public static readonly int RippleNoiseParams = UnityEngine.Shader.PropertyToID("_Ripple_Noise_Params");

                public static readonly int WaterCutoutMask = UnityEngine.Shader.PropertyToID("_Water_Cutout_Mask");
                public static readonly int CutoutWaterVolumeMode = UnityEngine.Shader.PropertyToID("_Cutout_Water_Volume_Mode");
                public static readonly int CutoutWaterReverse = UnityEngine.Shader.PropertyToID("_Cutout_Water_Reverse");

                // underwater
                public static readonly int DepthFade = UnityEngine.Shader.PropertyToID("_Depth_Fade");
                public static readonly int CameraFade = UnityEngine.Shader.PropertyToID("_Camera_Fade");
                public static readonly int WaterColor = UnityEngine.Shader.PropertyToID("_Water_Color");
                public static readonly int Intensity = UnityEngine.Shader.PropertyToID("_Intensity");
                public static readonly int WaterLevel = UnityEngine.Shader.PropertyToID("_Water_Level");
                public static readonly int WaterFade = UnityEngine.Shader.PropertyToID("_Water_Fade");
                public static readonly int MaxVisibilityDistance = UnityEngine.Shader.PropertyToID("_Max_Visibility_Distance");
                public static readonly int MaxVisibilityDepth = UnityEngine.Shader.PropertyToID("_Max_Visibility_Depth");
                public static readonly int ChromaticAberrationStrength = UnityEngine.Shader.PropertyToID("_Chromatic_Aberration_Strength");
                public static readonly int ShadowsAttenuation = UnityEngine.Shader.PropertyToID("_Shadows_Attenuation");
                public static readonly int LightDirectionMatrix = UnityEngine.Shader.PropertyToID("_Light_Direction_Matrix");

                public static readonly int CausticsTexture1 = UnityEngine.Shader.PropertyToID("_Caustics_Texture_1");
                public static readonly int CausticsTexture1Params = UnityEngine.Shader.PropertyToID("_Caustics_Texture_1_Params");
                public static readonly int CausticsTexture2 = UnityEngine.Shader.PropertyToID("_Caustics_Texture_2");
                public static readonly int CausticsTexture2Params = UnityEngine.Shader.PropertyToID("_Caustics_Texture_2_Params");

                // compute
                public static readonly int ComputeResultOffset = UnityEngine.Shader.PropertyToID("ResultOffset");
                public static readonly int ComputeResultNormal = UnityEngine.Shader.PropertyToID("ResultNormal");
                public static readonly int ComputeResultHeightOffset = UnityEngine.Shader.PropertyToID("ResultHeightOffset");

                public static readonly int ComputeWaves = UnityEngine.Shader.PropertyToID("Waves");
                public static readonly int ComputeLocalAreas = UnityEngine.Shader.PropertyToID("LocalAreas");
                public static readonly int ComputeWaveCount = UnityEngine.Shader.PropertyToID("WaveCount");
                public static readonly int ComputeLocalAreaCount = UnityEngine.Shader.PropertyToID("LocalAreaCount");
                public static readonly int ComputeUseLocalArea = UnityEngine.Shader.PropertyToID("UseLocalArea");
                public static readonly int ComputeTime = UnityEngine.Shader.PropertyToID("Time");
                public static readonly int ComputeCameraOffset = UnityEngine.Shader.PropertyToID("CameraOffset");
                public static readonly int ComputeWorldOriginPosition = UnityEngine.Shader.PropertyToID("WorldOriginPosition");
                public static readonly int ComputeWorldOriginRotation = UnityEngine.Shader.PropertyToID("WorldOriginRotation");
                public static readonly int ComputeTextureSize = UnityEngine.Shader.PropertyToID("TextureSize");
                public static readonly int ComputeProjectionSize = UnityEngine.Shader.PropertyToID("ProjectionSize");
                public static readonly int ComputeBaseInfluences = UnityEngine.Shader.PropertyToID("BaseInfluences");
                public static readonly int ComputeHillnessOffsetFactor = UnityEngine.Shader.PropertyToID("HillnessOffsetFactor");
                public static readonly int ComputeHillnessNormalPower = UnityEngine.Shader.PropertyToID("HillnessNormalPower");

                public static readonly int ComputeResultSimulation = UnityEngine.Shader.PropertyToID("ResultSimulation");
                //public static readonly int ComputeResultRipple = UnityEngine.Shader.PropertyToID("ResultRipple");
                public static readonly int ComputeWaveOffsetTex = UnityEngine.Shader.PropertyToID("WaveOffsetTex");
                public static readonly int ComputeWaveHeightOffsetTex = UnityEngine.Shader.PropertyToID("WaveHeightOffsetTex");
                public static readonly int ComputeRippleProjectionTex = UnityEngine.Shader.PropertyToID("RippleProjectionTex");

                public static readonly int ComputeProjectorOffset = UnityEngine.Shader.PropertyToID("ProjectorOffset");
                public static readonly int ComputeProjectorY = UnityEngine.Shader.PropertyToID("ProjectorY");
                public static readonly int ComputeRippleTextureSize = UnityEngine.Shader.PropertyToID("RippleTextureSize");
                public static readonly int ComputeRippleProjectionSize = UnityEngine.Shader.PropertyToID("RippleProjectionSize");
                public static readonly int ComputeWaveProjectionSize = UnityEngine.Shader.PropertyToID("WaveProjectionSize");

                public static readonly int CumputeDeltaTime = UnityEngine.Shader.PropertyToID("DeltaTime");
                public static readonly int CumputeMaxDepth = UnityEngine.Shader.PropertyToID("MaxDepth");
                public static readonly int CumputeRippleTime = UnityEngine.Shader.PropertyToID("RippleTime");
                public static readonly int CumputeRestoreTime = UnityEngine.Shader.PropertyToID("RestoreTime");
                public static readonly int CumputeBlurStep = UnityEngine.Shader.PropertyToID("BlurStep");
                public static readonly int CumputeBlurAttenuation = UnityEngine.Shader.PropertyToID("BlurAttenuation");
            }
            public static class Tag
            {
                public static readonly string WaterSurface = "DragonWaterSurface";
                public static readonly string WaterCutout = "DragonWaterCutout";
                public static readonly string UnderwaterCaustics = "DragonUnderwaterCaustics";
            }
        }
    }
}
