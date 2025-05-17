Shader "Hidden/Dragon Ripple Caster"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            //Name "DragonRippleCaster"
            //Tags { "LightMode" = "DragonRippleCaster" }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One
            BlendOp Min

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 world : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.world = mul(unity_ObjectToWorld, float4(v.vertex, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, o.world);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(i.world.y, 0, 0, 0);
            }
            ENDCG
        }
    }
}
