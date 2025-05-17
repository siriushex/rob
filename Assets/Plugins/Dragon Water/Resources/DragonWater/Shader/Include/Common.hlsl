#ifndef DRAGON_COMMON
#define DRAGON_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38

SAMPLER(SamplerState_Linear_Clamp);
SAMPLER(SamplerState_Linear_Repeat);


inline float2 RotateVec2(float2 position, float rot)
{
    float s = sin(rot);
    float c = cos(rot);
    return float2(
        (position.x * c) - (position.y * s),
        (position.y * c) + (position.x * s)
    );
 }

inline float3 TransformOriginWS(float3 positionWS, float3 offset)
{
    float2 transformed = RotateVec2(positionWS.xz - offset.xy, offset.z);
    return float3(transformed.x, positionWS.y, transformed.y);
}

inline float4 ComputeScreenPos(float4 posCS, float projectionSign)
{
    float4 o = posCS * 0.5f;
    o.xy = float2(o.x, o.y * projectionSign) + o.w;
    o.zw = posCS.zw;
    o.xy /= o.w;
    return o;
}

inline float GetWaterDepth(float4 screen)
{
    float linearDepth = Linear01Depth(SampleSceneDepth(screen.xy), _ZBufferParams);
    float dist = lerp(_ProjectionParams.y, _ProjectionParams.z, linearDepth);
    return max(0, dist - screen.w);
}

inline float2 GetWaterUV(float3 positionWS, float size, float speed)
{
    return positionWS.xz * size + (speed * _Time.x);
}

float3 NormalFromTexture(TEXTURE2D(tex), float2 uv, float offset, float strength)
{
    float poffset = pow(offset, 3) * 0.1;
    float2 offsetU = float2(uv.x + poffset, uv.y);
    float2 offsetV = float2(uv.x, uv.y + poffset);
    float normalSample = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, uv);
    float uSample = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, offsetU);
    float vSample = SAMPLE_TEXTURE2D(tex, SamplerState_Linear_Repeat, offsetV);
    float3 va = float3(1, 0, (uSample - normalSample) * strength);
    float3 vb = float3(0, 1, (vSample - normalSample) * strength);
    return normalize(cross(va, vb));
}

float3 BlendOverlay(float3 base, float3 blend, float opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - base) * (1.0 - blend);
    float3 result2 = 2.0 * base * blend;
    float3 zeroOrOne = step(base, 0.5);
    float3 result = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    return lerp(base, result, opacity);
}

inline float Attenuate(float amount, float2 params)
{
    return saturate(pow(amount, params.y) * params.x);
}

#endif