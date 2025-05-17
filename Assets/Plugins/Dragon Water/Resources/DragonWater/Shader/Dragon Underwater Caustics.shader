Shader "Hidden/Dragon Underwater Caustics"
{
    Properties
    {
        [NoScaleOffset] _Caustics_Texture_1("Caustics Texture 1", 2D) = "black" {}
        _Caustics_Texture_1_Params("Caustics Texture 1 Params", Vector) = (0.5, 0.1, 0, 0)
        [NoScaleOffset]_Caustics_Texture_2("Caustics Texture 2", 2D) = "black" {}
        _Caustics_Texture_2_Params("Caustics Texture 2 Params", Vector) = (0.4, -0.033, 0, 0)
        _Water_Color("Water Color", Color) = (0.1462264, 0.5942079, 1, 1)
        _Intensity("Intensity", Range(0, 2)) = 1
        [Toggle(_USE_CHROMATIC_ABERRATION)]_USE_CHROMATIC_ABERRATION("Use Chromatic Aberration", Float) = 0
        _Chromatic_Aberration_Strength("Chromatic Aberration Strength", Float) = 0.01
        _Water_Level("Water Level", Float) = 0
        _Water_Fade("Water Fade", Float) = 2
        _Max_Visibility_Distance("Max Visibility Distance", Float) = 100
        _Max_Visibility_Depth("Max Visibility Depth", Float) = 150
        [Toggle(_OCCLUDE_BY_SHADOWS)]_OCCLUDE_BY_SHADOWS("Occlude By Shadows", Float) = 0
        _Shadows_Attenuation("Shadows Attenuation", Range(0, 1)) = 0.75
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _Water_Color;
        float _Water_Level;
        float _Water_Fade;
        float _Max_Visibility_Distance;
        float _Intensity;
        float _Max_Visibility_Depth;
        float _Chromatic_Aberration_Strength;
        float _Shadows_Attenuation;
        float4 _Caustics_Texture_1_Params;
        float4 _Caustics_Texture_2_Params;
        float4x4 _Light_Direction_Matrix;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "DragonUnderwaterCaustics"
            Tags
            {
                "LightMode" = "DragonUnderwaterCaustics"
            }

            Cull Front
            Blend SrcAlpha One, One One
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_local_fragment _ _USE_CHROMATIC_ABERRATION
            #pragma multi_compile_local_fragment _ _OCCLUDE_BY_SHADOWS

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #define _SURFACE_TYPE_TRANSPARENT

            #include "./Include/Common.hlsl"

            TEXTURE2D(_Caustics_Texture_1);
            TEXTURE2D(_Caustics_Texture_2);

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
            };


            void ReconstructWorldPosition(float2 screenUV, float screenDepth, out float3 outPosition, out float outExistance)
            {
                #if UNITY_REVERSED_Z
                    real depth = screenDepth;
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, screenDepth);
                #endif

                #if UNITY_REVERSED_Z
                if (depth < 0.0001)
                {
                    outPosition = 0;
                    outExistance = 0;
                    return;
                }
                #else
                if (depth > 0.9999)
                {
                    outPosition = 0;
                    outExistance = 0;
                    return;
                }
                #endif

                outPosition = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                outExistance = 1;
            }

            float3 SampleCausticsTexture(float2 uv, TEXTURE2D(tex), float4 params)
            {
                float2 tuv = uv * params.x + _Time.y * params.y;

                #ifdef _USE_CHROMATIC_ABERRATION
                    float2 tuv1 = tuv + float2(_Chromatic_Aberration_Strength, _Chromatic_Aberration_Strength);
                    float2 tuv2 = tuv + float2(_Chromatic_Aberration_Strength, -_Chromatic_Aberration_Strength);
                    float2 tuv3 = tuv + float2(-_Chromatic_Aberration_Strength, -_Chromatic_Aberration_Strength);
                    float r = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, tuv1);
                    float g = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, tuv2);
                    float b = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, tuv3);
                #else
                    float r = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, tuv);
                    float g = r;
                    float b = r;
                #endif

                return float3(r,g,b);
            }


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float4 screenPosition = ComputeScreenPos(TransformWorldToHClip(IN.positionWS), _ProjectionParams.x);
                float depth = SampleSceneDepth(screenPosition.xy);

                float3 positionWS;
                float existance;
                ReconstructWorldPosition(screenPosition.xy, depth, positionWS, existance);


                float waterLevelFade;
                waterLevelFade = saturate((_Water_Level - positionWS.y) / _Water_Fade);

                float depthFade;
                depthFade = abs(_Water_Level - positionWS.y);
                depthFade -= (_Max_Visibility_Depth * 0.5);
                depthFade /= (_Max_Visibility_Depth * 0.5);
                depthFade = 1 - saturate(depthFade);

                float distanceFade;
                distanceFade = distance(_WorldSpaceCameraPos, positionWS);
                distanceFade -= (_Max_Visibility_Distance * 0.5);
                distanceFade /= (_Max_Visibility_Distance * 0.5);
                distanceFade = 1 - saturate(distanceFade);


                float2 uv = mul(positionWS, _Light_Direction_Matrix).xy;

                float3 caustics1 = SampleCausticsTexture(uv, _Caustics_Texture_1, _Caustics_Texture_1_Params);
                float3 caustics2 = SampleCausticsTexture(uv, _Caustics_Texture_2, _Caustics_Texture_2_Params);
                float3 caustics = min(caustics1, caustics2);


                float3 color;
                color = lerp(_MainLightColor.rgb, _Water_Color.rgb, 0.5);
                color *= caustics * _Intensity;
                color *= existance * waterLevelFade * depthFade * distanceFade;

                #ifdef _OCCLUDE_BY_SHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                    float shadow = MainLightShadow(shadowCoord, positionWS, half4(1, 1, 1, 1), _MainLightOcclusionProbes);
                    shadow = lerp(1 - _Shadows_Attenuation, 1, shadow);
                    color *= shadow;
                #endif

                return half4(color.rgb, 1); // one alpha, its additive
            }
            ENDHLSL
        }
    }
}
