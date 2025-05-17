#ifndef DRAGON_WATER_COMMON
#define DRAGON_WATER_COMMON

#ifdef _USE_REFRACTION
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#endif

#include "./Common.hlsl"
#include "./WaterCBuffer.hlsl"

TEXTURE2D(_Wave_Texture_Offset);
TEXTURE2D(_Wave_Texture_Normal);

TEXTURE2D(_Color_Noise_Texture_1);
TEXTURE2D(_Color_Noise_Texture_2);

TEXTURE2D(_Normal_Map_1);
TEXTURE2D(_Normal_Map_2);

TEXTURE2D(_Foam_Noise_Texture);
TEXTURE2D(_Foam_Noise_Texture_Extra_1);
TEXTURE2D(_Foam_Noise_Texture_Extra_2);

TEXTURE2D(_Ripple_Texture);
TEXTURE2D(_Ripple_Noise_Texture);

TEXTURE2D(_Water_Cutout_Mask);
SAMPLER(sampler_Water_Cutout_Mask);

TEXTURECUBE(_Reflections_GI_Cubemap);
SAMPLER(sampler_Reflections_GI_Cubemap);


struct ColorNoiseOutput
{
    float amount;
    float emission;
    float4 color;
};

struct RippleOutput
{
    float amount;
    float3 normal;
    float attenuationNormal;
    float attenuationColor;
    float attenuationNoise;
    float attenuationFoam;
    float2 ssFactor;
};

struct FoamOutput
{
    float amount;
    float emission;
    float4 color;
    float2 ssFactor;
    float attenuationNormal;
};

struct RefractionOutput
{
    float3 color;
    float depth;
};

struct MaterialOutput
{
    float3 albedo;
    float3 alpha;
    float3 emission;
    float3 specular;
    float smoothness;
};


inline float CutoutWaterVolume_None(float N, float F, float D)
{
    if (_Cutout_Water_Volume_Mode != 0)
	    return 0;

    return (F - N) != 0;
}
inline float CutoutWaterVolume_Front(float N, float F, float D)
{
    if (_Cutout_Water_Volume_Mode != 1)
        return 0;
        
    if ((F-N) == 0)
        return 0;
    else if (N == FLT_MAX)
        return 1;
        
    float m = min(F,N);
    return (m < D || F < N) ? 1 : 0;
}
inline float CutoutWaterVolume_Perfect(float N, float F, float D)
{
    if (_Cutout_Water_Volume_Mode != 2)
	    return 0;

    float a = D > N && D < F;
    float b = D < F && F < N;
    return a || b ? 1 : 0;
}
void CutoutWaterVolume(float4 screenPosition)
{
    #ifdef _ALPHATEST_ON
        float2 mask = SAMPLE_TEXTURE2D(_Water_Cutout_Mask, sampler_Water_Cutout_Mask, screenPosition.xy).rg;
        float depth = screenPosition.w;

        float c1 = CutoutWaterVolume_None(mask.r, mask.g, depth);
        float c2 = CutoutWaterVolume_Front(mask.r, mask.g, depth);
        float c3 = CutoutWaterVolume_Perfect(mask.r, mask.g, depth);

        float c = c1 + c2 + c3;
        c = lerp(c, 1 - c, _Cutout_Water_Reverse);

        clip(0.5 - c);
    #endif
}


void WaveDisplacementVertex(float3 positionWS, out float3 newPosition, out float distanceAttenuation)
{
    float2 uv = (positionWS.xz - _Wave_Texture_Projection.xy) / _Wave_Texture_Projection.zw;

    float dist = distance(positionWS.xz, _WorldSpaceCameraPos.xz);
    distanceAttenuation = smoothstep(
        _Wave_Texture_Projection.z * 0.25,
        _Wave_Texture_Projection.z * 0.5,
        dist);

    float3 offset = SAMPLE_TEXTURE2D_LOD(_Wave_Texture_Offset, SamplerState_Linear_Clamp, uv, 0).xyz;
    newPosition = positionWS + lerp(offset, 0, distanceAttenuation);
}
void WaveDisplacementFragment(float3 positionWS, float distanceAttenuation, out float3 normal, out float hillness)
{
    float2 uv = (positionWS.xz - _Wave_Texture_Projection.xy) / _Wave_Texture_Projection.zw;

    hillness = SAMPLE_TEXTURE2D(_Wave_Texture_Offset, SamplerState_Linear_Clamp, uv).w;
    normal = SAMPLE_TEXTURE2D(_Wave_Texture_Normal, SamplerState_Linear_Clamp, uv).xyz;

    hillness = lerp(hillness, 0, distanceAttenuation);
    normal = normalize(float3(0,1,0) + lerp(normal, 0, distanceAttenuation));
}

ColorNoiseOutput CalculateColorNoise(float3 positionWS, float3 positionOWS)
{
    ColorNoiseOutput output = (ColorNoiseOutput)0;

    #ifdef _USE_COLOR_NOISE_TEXTURE
        float2 uv1 = GetWaterUV(positionOWS, _Color_Noise_Texture_1_Params.x, _Color_Noise_Texture_1_Params.y);
        float2 uv2 = GetWaterUV(positionOWS, _Color_Noise_Texture_2_Params.x, _Color_Noise_Texture_2_Params.y);

        float noise1 = SAMPLE_TEXTURE2D(_Color_Noise_Texture_1, SamplerState_Linear_Repeat, uv1).r * _Color_Noise_Texture_1_Params.z;
        float noise2 = SAMPLE_TEXTURE2D(_Color_Noise_Texture_2, SamplerState_Linear_Repeat, uv2).r * _Color_Noise_Texture_2_Params.z;

        float noise = min(noise1, noise2);

        output.amount = noise * _Color_Noise_Params.x;
        output.emission = _Color_Noise_Params.y;
        output.color = _Color_Noise_Color;
    #endif

    return output;
}

RippleOutput CalculateRipple(float3 positionWS)
{
    RippleOutput output = (RippleOutput)0;

    #ifdef _USE_RIPPLE
        float2 uvRipple = (positionWS.xz - _Ripple_Projection.xy) / _Ripple_Projection.zw;
        uvRipple.y = 1 - uvRipple.y;

        output.amount = SAMPLE_TEXTURE2D(_Ripple_Texture, SamplerState_Linear_Clamp, uvRipple).r;

        output.normal = NormalFromTexture(_Ripple_Texture, uvRipple, _Ripple_Normal_Params.z, _Ripple_Normal_Params.w);
        output.normal = output.normal.xzy * float3(-1,1,-1);
        output.normal = lerp(float3(0,1,0), output.normal, output.amount);

        float2 uvNoise = positionWS.xz * _Ripple_Noise_Params.z;
        float noise = SAMPLE_TEXTURE2D(_Ripple_Noise_Texture, SamplerState_Linear_Repeat, uvNoise).r;

        output.attenuationNormal = Attenuate(output.amount, _Ripple_Normal_Params);
        output.attenuationColor = Attenuate(output.amount, _Ripple_Color_Params);
        output.attenuationNoise = Attenuate(output.amount, _Ripple_Noise_Params) * noise;
        output.attenuationFoam = Attenuate(output.amount, _Ripple_Foam_Params);

        output.ssFactor = 1 - ((1 - _Ripple_Material_Params.zw) * output.amount);
    #else
        output.normal = float3(0,1,0);
        output.ssFactor = 1;
    #endif

    return output;
}


float2 SampleHillnessFoam(float3 positionWS, float3 positionOWS, TEXTURE2D(noiseTex), float4 params, float hillness)
{
    float factor = saturate((hillness - params.y) * params.z);
    float weight = smoothstep(0,1,factor) * params.x;

    float2 uv = GetWaterUV(positionOWS, 0.01, 0) * params.w;
    float noise = SAMPLE_TEXTURE2D(noiseTex, SamplerState_Linear_Repeat, uv).r;

    return float2(weight, noise);
}
FoamOutput CalculateFoam(float3 positionWS, float3 positionOWS, float hillness, float waterDeph, RippleOutput ripple)
{
    FoamOutput output = (FoamOutput)0;

    #ifndef _FOAM_MODE_OFF
        float edgeFoam;
        edgeFoam = smoothstep(0, 1, 1 - saturate(waterDeph / _Foam_Edge_Params.y));
        edgeFoam = pow(edgeFoam, _Foam_Edge_Params.z) * _Foam_Edge_Params.x;

        float2 hFoam1 = SampleHillnessFoam(positionWS, positionOWS, _Foam_Noise_Texture, _Foam_Hillness_Params, hillness);

        float hFoamValue;
        float4 hFaomColor;

        #ifdef _FOAM_MODE_ON_EXTRA
            float2 hFoam2 = SampleHillnessFoam(positionWS, positionOWS, _Foam_Noise_Texture_Extra_1, _Foam_Hillness_Params_Extra_1, hillness);
            float2 hFoam3 = SampleHillnessFoam(positionWS, positionOWS, _Foam_Noise_Texture_Extra_2, _Foam_Hillness_Params_Extra_2, hillness);
        {
            float v1 = hFoam1.x * hFoam1.y;
            float v2 = hFoam2.x * hFoam2.y;
            float v3 = hFoam3.x * hFoam3.y;

            hFoamValue  = v1 + v2 + v3;

            if (hFoamValue > 0.005)
            {
                v1 = v1 / hFoamValue;
                v2 = v2 / hFoamValue;
                v3 = v3 / hFoamValue;
                hFaomColor = (v1 * _Foam_Color) + (v2 * _Foam_Color_Extra_1) + (v3 * _Foam_Color_Extra_2);
            }
            else
            {
                hFaomColor = _Foam_Color;
            }
        }
        #else
            hFoamValue = hFoam1.x * hFoam1.y;
            hFaomColor = _Foam_Color;
        #endif

        edgeFoam *= hFoam1.y;

        output.amount = max(edgeFoam, hFoamValue) * _Foam_Params.x;
        output.amount = lerp(output.amount, 0, ripple.attenuationFoam);
        output.emission = _Foam_Params.w;

        float2 ssMod = 1 - _Foam_Params.yz;
        output.ssFactor = 1 - (ssMod * output.amount);

        output.attenuationNormal = Attenuate(saturate(output.amount), _Foam_Normal_Params.xy);

        #ifdef _FOAM_MODE_ON_EXTRA
            output.color = lerp(hFaomColor, _Foam_Color, edgeFoam);
        #else
            output.color = _Foam_Color;
        #endif
    #else
        output.ssFactor = 1;
    #endif

    return output;
}


float3 CalculateNormal(float3 positionWS, float3 positionOWS, float3 vertexNormal, RippleOutput ripple, FoamOutput foam)
{
    float3 normal = vertexNormal;

    #ifdef _USE_NORMAL_MAP
    {
        float2 uv1 = GetWaterUV(positionOWS, _Normal_Map_1_Params.x, _Normal_Map_1_Params.y);
        float2 uv2 = GetWaterUV(positionOWS, _Normal_Map_2_Params.x, _Normal_Map_2_Params.y);

        float4 normal1 = SAMPLE_TEXTURE2D(_Normal_Map_1, SamplerState_Linear_Repeat, uv1);
        float4 normal2 = SAMPLE_TEXTURE2D(_Normal_Map_2, SamplerState_Linear_Repeat, uv2);

        normal1.rgb = UnpackNormal(normal1);
        normal2.rgb = UnpackNormal(normal2);

        normal1.rgb = float3(normal1.rg * _Normal_Map_1_Params.z, lerp(1, normal1.b, saturate(_Normal_Map_1_Params.z)));
        normal2.rgb = float3(normal2.rg * _Normal_Map_2_Params.z, lerp(1, normal2.b, saturate(_Normal_Map_2_Params.z)));
    
        float3 blended = normalize(float3(normal1.rg + normal2.rg, normal1.b * normal2.b));

        float3 t = normal.xzy + float3(0.0, 0.0, 1.0); // swizzled
        float3 u = blended.xyz * float3(-1.0, -1.0, 1.0);
        normal = ((t / t.z) * dot(t, u) - u).xzy; // swizzled
    }
    #endif

    #ifndef _FOAM_MODE_OFF
        normal = lerp(normal, float3(0,1,0), foam.attenuationNormal);
    #endif

    #ifdef _USE_RIPPLE
        normal = normalize(float3(ripple.normal.xz + normal.xz, ripple.normal.y * normal.y)).xzy;
        normal = lerp(normal, float3(0,1,0), ripple.attenuationNormal);
    #endif

    normal = lerp(float3(0,1,0), normal, _Normal_Global_Strength);

    return normalize(normal);
}


RefractionOutput CalculateRefraction(float4 screenPosition, float3 normal, float3 waterDepth)
{
    RefractionOutput output = (RefractionOutput)0;

    #ifdef _USE_REFRACTION
        float4 uv = screenPosition;
        float depthGradient = saturate(waterDepth / _Water_Depth);
        float2 offset = normal.xz;

        float planeT = saturate(screenPosition.w / _Refraction_Params.y);
        float strength = lerp(_Refraction_Params.x, _Refraction_Params.x * 0.01, planeT); 

        offset *= strength;
        uv.xy += offset;

        float depthThere = GetWaterDepth(uv);
        float depthGradientThere = saturate(depthThere / _Water_Depth);

        uv = screenPosition;
        uv.xy += offset * depthGradientThere;

        output.color = SampleSceneColor(uv);
        output.depth = GetWaterDepth(uv);
    #endif

    return output;
}


float4 CalculateBaseColor(float3 positionWS, float waterDepth, float hillness, RefractionOutput refraction, ColorNoiseOutput noise)
{
    float4 color;

    #ifdef _USE_REFRACTION
        float depthGradient = smoothstep(0, 1, saturate(refraction.depth / _Water_Depth));
    #else
        float depthGradient = smoothstep(0, 1, saturate(waterDepth / _Water_Depth));
    #endif
    color = lerp(_Shallow_Water_Color, _Deep_Water_Color, depthGradient);

    float hMul = hillness - _Color_Hillness_Params.x;
    float hMulDark = min(0, hMul) * _Color_Hillness_Params.z;
    float hMulLight = max(0, hMul) * _Color_Hillness_Params.y;
    color.rgb *= 1 + hMulDark + hMulLight;

    color.rgb = lerp(color.rgb, noise.color.rgb, noise.amount);

    return color;
}


float4 CombineColor(float4 baseColor, RippleOutput ripple, FoamOutput foam)
{
    float4 color = baseColor;

    #ifdef _USE_RIPPLE
        color = lerp(color, _Ripple_Color_Color, ripple.attenuationColor);
        color = lerp(color, _Ripple_Noise_Color, ripple.attenuationNoise);
    #endif

    #ifndef _FOAM_MODE_OFF
        color = lerp(color, foam.color, foam.amount);
    #endif

    return saturate(color);
}


void WaterSSS(float3 color, float3 normal, float3 viewDirWS, out float3 sssColor, out float3 sssEmission)
{
    float3 sssNormal = normal * _SSS_Params.x + _MainLightPosition.xyz;
    float sssDot = dot(viewDirWS, -sssNormal);
    float intensity = pow(clamp(sssDot,0,2), _SSS_Params.y) * _SSS_Global_Intensity;

    sssColor = BlendOverlay(color, _MainLightColor.rgb, intensity * _SSS_Params.z);
    sssEmission = _MainLightColor.rgb * intensity * _SSS_Params.w;
}
MaterialOutput GetMaterialOutput(float4 color, float3 normal, float3 viewDirWS, RippleOutput ripple, FoamOutput foam, RefractionOutput refraction, ColorNoiseOutput noise)
{
    MaterialOutput output = (MaterialOutput)0;

    output.alpha = color.a;
    output.emission = 0;

    #ifdef _USE_REFRACTION
        output.albedo = lerp(refraction.color, color.rgb, output.alpha);
    #else
        output.albedo = color.rgb;
    #endif

    float3 sssColor;
    float3 sssEmission;
    WaterSSS(output.albedo, normal, viewDirWS, sssColor, sssEmission);

    output.albedo.rgb = sssColor.rgb;
    output.emission.rgb += sssEmission.rgb;

    output.emission.rgb += foam.color.rgb * foam.amount * foam.emission;
    output.emission.rgb += noise.color.rgb * noise.amount * noise.emission;

    float2 ssFactor = foam.ssFactor * ripple.ssFactor;
    output.smoothness = ssFactor.x * _Smoothness;
    output.specular = ssFactor.y * _Specular;

    return output;
}


void PatchReflections()
{
    #ifdef _REFLECTIONS_CUBEMAP
        unity_SpecCube0_HDR.x = _Reflections_GI_Intensity;
        unity_SpecCube0_HDR.y = 1;
        unity_SpecCube1_HDR.x = _Reflections_GI_Intensity;
        unity_SpecCube1_HDR.y = 0;
        unity_SpecCube0 = _Reflections_GI_Cubemap;
        samplerunity_SpecCube0 = sampler_Reflections_GI_Cubemap;
        unity_SpecCube1 = _Reflections_GI_Cubemap;
        samplerunity_SpecCube1 = sampler_Reflections_GI_Cubemap;
    #elif _REFLECTIONS_NORMAL
        unity_SpecCube0_HDR.x *= _Reflections_GI_Intensity;
        unity_SpecCube1_HDR.x *= _Reflections_GI_Intensity;
    #elif _REFLECTIONS_SIMPLE
        _GlossyEnvironmentColor.rgb *= _Reflections_Glossy_Tint.rgb;
    #endif
}

#endif