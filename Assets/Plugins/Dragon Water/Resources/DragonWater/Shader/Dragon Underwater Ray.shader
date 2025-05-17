Shader "Hidden/Dragon Underwater Ray"
{
    Properties
    {
        _Depth_Fade("Depth Fade", Float) = 20
        _Camera_Fade("Camera Fade", Float) = 20
        _Water_Color("Water Color", Color) = (0.1462264, 0.5942079, 1, 1)
        _Intensity("Intensity", Range(0, 1)) = 0.5
        _Water_Level("Water Level", Float) = 0
        _Water_Fade("Water Fade", Float) = 2
        _Max_Visibility_Distance("Max Visibility Distance", Float) = 300
        [Toggle(_OCCLUDE_BY_SHADOWS)]_OCCLUDE_BY_SHADOWS("Occlude By Shadows", Float) = 0
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Unlit"
            "Queue" = "Transparent"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float _Depth_Fade;
        float _Camera_Fade;
        float4 _Water_Color;
        float _Intensity;
        float _Water_Level;
        float _Water_Fade;
        float _Max_Visibility_Distance;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Universal Forward"
            Tags
            {
                //
            }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_local_fragment _ _OCCLUDE_BY_SHADOWS

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #define _SURFACE_TYPE_TRANSPARENT

            #include "./Include/Common.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO

                float4 topColor : INTERP4;
                float4 bottomColor : INTERP5;
                float hash : INTERP6;
            };



            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.uv = IN.uv;

                float3 objPos = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
                OUT.hash = abs(objPos.x * objPos.z);

                OUT.topColor = lerp(_MainLightColor, _Water_Color, fmod(OUT.hash, 1.0));
                OUT.bottomColor = lerp(OUT.topColor, _Water_Color, 0.5);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float4 screenPosition = ComputeScreenPos(TransformWorldToHClip(IN.positionWS), _ProjectionParams.x);

                float lengthFade;
                lengthFade = saturate(GetWaterDepth(screenPosition) / _Depth_Fade) * IN.uv.y;

                float smoothShape;
                smoothShape = abs(IN.uv.x * 2.0 - 1.0);
                smoothShape = smoothstep(0, 0.75, 1 - smoothShape);

                float waterLevelFade;
                waterLevelFade = saturate((_Water_Level - IN.positionWS.y) / _Water_Fade);

                float cameraFade;
                cameraFade = saturate(screenPosition.w / _Camera_Fade);

                float distanceFade;
                distanceFade = screenPosition.w - (_Max_Visibility_Distance * 0.5);
                distanceFade /= (_Max_Visibility_Distance * 0.5);
                distanceFade = 1 - saturate(distanceFade);


                float3 color = lerp(IN.bottomColor, IN.topColor, IN.uv.y).rgb;
                float alpha = lengthFade * smoothShape * waterLevelFade * cameraFade * distanceFade;


                float animatedIntensity;
                animatedIntensity = frac(IN.hash) * _SinTime.w;
                animatedIntensity = animatedIntensity * 0.5 + 0.5;
                animatedIntensity *= _Intensity;

                alpha *= animatedIntensity;

                #ifdef _OCCLUDE_BY_SHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    float shadow = MainLightShadow(shadowCoord, IN.positionWS, half4(1, 1, 1, 1), _MainLightOcclusionProbes);
                    alpha *= shadow;
                #endif

                return half4(color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
