Shader "Hidden/Dragon Water Cutout"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "DragonWaterCutout"
            Tags { "LightMode" = "DragonWaterCutout" }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One//, One One
            BlendOp Min//, Max

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define FLT_MAX 3.402823466e+38
            #define FLT_MIN 1.175494351e-38

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                float depth = ComputeScreenPos(i.vertex).w;
                float r = facing > 0 ? depth : FLT_MAX;
                float g = facing > 0 ? FLT_MAX : depth;
                return float4(r, g, 0, 0);
            }
            ENDCG
        }
    }
}
