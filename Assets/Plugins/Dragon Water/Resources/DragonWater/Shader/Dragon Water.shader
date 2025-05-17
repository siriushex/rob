Shader "Hidden/Dragon Water"
{
    Properties
    {
        _World_Origin_Offset("World Origin Offset", Vector) = (0, 0, 0, 0)
        [NoScaleOffset]_Wave_Texture_Offset("Wave Texture Offset", 2D) = "black" {}
        [NoScaleOffset]_Wave_Texture_Normal("Wave Texture Normal", 2D) = "black" {}
        _Wave_Texture_Projection("Wave Texture Projection", Vector) = (0, 0, 1, 1)
        _Water_Depth("Water Depth", Float) = 3
        _Shallow_Water_Color("Shallow Water Color", Color) = (0, 0.7410011, 1, 0.5019608)
        _Deep_Water_Color("Deep Water Color", Color) = (0.1273585, 0.384043, 1, 0.7843137)
        _Color_Hillness_Params("Color Hillness Params", Vector) = (0.01, 0, 0, 0)
        _Color_Noise_Color("Color Noise Color", Color) = (0, 0.3254902, 0.3568628, 1)
        _Color_Noise_Params("Color Noise Params", Vector) = (0.01, 0.5, 0, 0)
        [NoScaleOffset]_Color_Noise_Texture_1("Color Noise Texture 1", 2D) = "white" {}
        [NoScaleOffset]_Color_Noise_Texture2_("Color Noise Texture 2", 2D) = "white" {}
        _Color_Noise_Texture_1_Params("Color Noise Texture 1 Params", Float) = (1, 1, 1, 1)
        _Color_Noise_Texture_2_Params("Color Noise Texture 2 Params", Float) = (1, 1, 1, 1)
        [Toggle(_USE_NORMAL_MAP)]_USE_NORMAL_MAP("Use Normal Map", Float) = 0
        _Normal_Global_Strength("Normal Global Strength", Range(0, 1)) = 0.5
        [Normal][NoScaleOffset]_Normal_Map_1("Normal Map 1", 2D) = "bump" {}
        _Normal_Map_1_Params("Normal Map 1 Params", Vector) = (0.01, 0.1, 0.5, 0)
        [Normal][NoScaleOffset]_Normal_Map_2("Normal Map 2", 2D) = "bump" {}
        _Normal_Map_2_Params("Normal Map 2 Params", Vector) = (0.01, 0.1, 0.5, 0)
        [KeywordEnum(Off, On, On Extra)]_FOAM_MODE("Foam Mode", Float) = 0
        [NoScaleOffset]_Foam_Noise_Texture("Foam Noise Texture", 2D) = "white" {}
        [NoScaleOffset]_Foam_Noise_Texture_Extra_1("Foam Noise Texture Extra 1", 2D) = "white" {}
        [NoScaleOffset]_Foam_Noise_Texture_Extra_2("Foam Noise Texture Extra 2", 2D) = "white" {}
        _Foam_Color("Foam Color", Color) = (1, 1, 1, 1)
        _Foam_Color_Extra_1("Foam Color Extra 1", Color) = (1, 1, 1, 1)
        _Foam_Color_Extra_2("Foam Color Extra 2", Color) = (1, 1, 1, 1)
        _Foam_Params("Foam Params", Vector) = (200, 0.5, 0.5, 1)
        _Foam_Normal_Params("Foam Normal Params", Vector) = (0.5, 1, 0, 0)
        _Foam_Hillness_Params("Foam Hillness Params", Vector) = (1, 0.5, 1, 200)
        _Foam_Hillness_Params_Extra_1("Foam Hillness Params Extra 1", Vector) = (1, 0.5, 1, 200)
        _Foam_Hillness_Params_Extra_2("Foam Hillness Params Extra 2", Vector) = (1, 0.5, 1, 200)
        _Foam_Edge_Params("Foam Edge Params", Vector) = (1, 10, 2, 0)
        [Toggle(_USE_REFRACTION)]_USE_REFRACTION("Use Refraction", Float) = 0
        _Refraction_Params("Refraction Params", Vector) = (0.1, 50, 0, 0)
        _Cutout_Water_Volume_Mode("Cutout Water Volume Mode", Int) = 1
        _Cutout_Water_Reverse("Cutout Water Reverse", Float) = 0
        [Toggle(_USE_RIPPLE)]_USE_RIPPLE("Use Ripple", Float) = 0
        [NoScaleOffset]_Ripple_Texture("Ripple Texture", 2D) = "black" {}
        _Ripple_Projection("Ripple Projection", Vector) = (0, 0, 0, 0)
        _Ripple_Material_Params("Ripple Material Params", Vector) = (0, 0, 0.5, 0)
        _Ripple_Color_Color("Ripple Color Color", Color) = (0, 0.3254902, 0.3568628, 1)
        _Ripple_Color_Params("Ripple Color Params", Vector) = (0.8, 1, 0, 0)
        _Ripple_Normal_Params("Ripple Normal Params", Vector) = (0.7, 1, 0.25, 8)
        _Ripple_Foam_Params("Ripple Foam Params", Vector) = (1, 1, 0, 0)
        [NoScaleOffset]_Ripple_Noise_Texture("Ripple Noise Texture", 2D) = "black" {}
        _Ripple_Noise_Color("Ripple Noise Color", Color) = (1, 1, 1, 1)
        _Ripple_Noise_Params("Ripple Noise Params", Vector) = (0.5, 1, 0.05, 0)
        _SSS_Global_Intensity("SSS Global Intensity", Float) = 1
        _SSS_Params("SSS Params", Vector) = (0.2, 2, 4, 0.5)
        _Smoothness("Smoothness", Range(0, 1)) = 0.9
        _Specular("Specular", Color) = (0.1254902, 0.1254902, 0.1254902, 1)
        _Reflections_GI_Intensity("Reflections GI Intensity", Float) = 1
        _Reflections_Glossy_Tint("Reflections Glossy Tint", Color) = (0, 0, 0, 0)
        [HideInInspector]_AlphaClip("_AlphaClip", Float) = 0
        [HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
        [HideInInspector]_DstBlend("_DstBlend", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }

        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        #include "./Include/WaterCBuffer.hlsl"
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "DragonWaterSurface"
            Tags
            {
                "LightMode" = "DragonWaterSurface"
            }
        
            // Render State
            Cull Off
            Blend [_SrcBlend] [_DstBlend]
            ZWrite On
			ZTest LEqual

            HLSLPROGRAM
		    #pragma vertex vert
		    #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_local_fragment _ _SURFACE_TYPE_TRANSPARENT      
            #pragma multi_compile_local _ _RECEIVE_SHADOWS_OFF

            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _SPECULARHIGHLIGHTS_OFF
            //#pragma multi_compile_local_fragment _ _ENVIRONMENTREFLECTIONS_OFF

            #pragma multi_compile_local_fragment _ _USE_COLOR_NOISE_TEXTURE
            #pragma multi_compile_local_fragment _ _USE_NORMAL_MAP
            #pragma multi_compile_local_fragment _FOAM_MODE_OFF _FOAM_MODE_ON _FOAM_MODE_ON_EXTRA
            #pragma multi_compile_local_fragment _ _USE_REFRACTION
            #pragma multi_compile_local_fragment _ _USE_RIPPLE
            #pragma multi_compile_local_fragment _REFLECTIONS_SIMPLE _REFLECTIONS_NORMAL _REFLECTIONS_CUBEMAP

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #define _SPECULAR_SETUP
            #define _NORMALMAP
            #define _NORMAL_DROPOFF_WS

            #ifdef _REFLECTIONS_SIMPLE
                #define _ENVIRONMENTREFLECTIONS_OFF
            #endif

            #include "./Include/WaterMain.hlsl"

		    ENDHLSL
        }
    }
}
