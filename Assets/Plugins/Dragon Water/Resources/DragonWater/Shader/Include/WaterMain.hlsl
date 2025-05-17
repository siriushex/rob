#ifndef DRAGON_WATER_MAIN
#define DRAGON_WATER_MAIN
#include "./WaterCommon.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    float3 viewDirectionWS : TEXCOORD4;

	#ifdef _ADDITIONAL_LIGHTS_VERTEX
		half4 fogFactorAndVertexLight : TEXCOORD5; // x: fogFactor, yzw: vertex light
	#else
		half fogFactor	: TEXCOORD5;
	#endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord : TEXCOORD6;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
    #ifdef DYNAMICLIGHTMAP_ON
        float2 dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
    #endif


    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO

    float3 originalWorldPosition : INTERP4;
    float distanceAttenuation : INTERP5;
};

void InitializeInputData(Varyings input, float3 normalWS, out InputData inputData)
{
	inputData = (InputData)0; // avoids "not completely initalized" errors

	inputData.positionWS = input.positionWS;

	float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
	inputData.normalWS = NormalizeNormalPerPixel(normalWS);
	inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS);

	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
		inputData.fogCoord = input.fogFactorAndVertexLight.x;
		inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
	#else
		inputData.fogCoord = input.fogFactor;
		inputData.vertexLighting = half3(0, 0, 0);
	#endif

    #if defined(DYNAMICLIGHTMAP_ON)
        inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    #else
        inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    #endif

	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

Varyings vert(Attributes IN)
{
	Varyings OUT;

	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

	VertexPositionInputs positionInputs;
	OUT.originalWorldPosition = TransformObjectToWorld(IN.positionOS.xyz);
	WaveDisplacementVertex(OUT.originalWorldPosition, positionInputs.positionWS, OUT.distanceAttenuation);

	positionInputs.positionVS = TransformWorldToView(positionInputs.positionWS);
    positionInputs.positionCS = TransformWorldToHClip(positionInputs.positionWS);
    float4 ndc = positionInputs.positionCS * 0.5f;
    positionInputs.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    positionInputs.positionNDC.zw = positionInputs.positionCS.zw;

	VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS.xyz, IN.tangentOS);

	OUT.positionCS = positionInputs.positionCS;
	OUT.positionWS = positionInputs.positionWS;

	half3 viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
	half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
	half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
				
	OUT.normalWS = half4(normalInputs.normalWS, viewDirWS.x);
	OUT.tangentWS = half4(normalInputs.tangentWS, viewDirWS.y);
	OUT.viewDirectionWS = viewDirWS;
	
	OUTPUT_LIGHTMAP_UV(IN.staticLightmapUV, unity_LightmapST, OUT.staticLightmapUV);
	#ifdef DYNAMICLIGHTMAP_ON
		OUT.dynamicLightmapUV = IN.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
    OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);

	#ifdef _ADDITIONAL_LIGHTS_VERTEX
		OUT.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
	#else
		OUT.fogFactor = fogFactor;
	#endif

	#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
		OUT.shadowCoord = GetShadowCoord(positionInputs);
	#endif

	return OUT;
}


half4 frag(Varyings IN) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

	float3 viewDirWS = normalize(IN.viewDirectionWS);
	float4 screenPosition = ComputeScreenPos(TransformWorldToHClip(IN.positionWS), _ProjectionParams.x);
	float waterDepth = GetWaterDepth(screenPosition);
	float3 positionOWS = TransformOriginWS(IN.positionWS.xyz, _World_Origin_Offset.xyz);

	CutoutWaterVolume(screenPosition);

	float hillness;
	float3 normal;
	WaveDisplacementFragment(IN.originalWorldPosition, IN.distanceAttenuation, normal, hillness);

	ColorNoiseOutput noise;
	noise = CalculateColorNoise(IN.positionWS, positionOWS);

	RippleOutput ripple;
	ripple = CalculateRipple(IN.positionWS);

	FoamOutput foam;
	foam = CalculateFoam(IN.positionWS, positionOWS, hillness, waterDepth, ripple);

	normal = CalculateNormal(IN.positionWS, positionOWS, normal, ripple, foam);

	RefractionOutput refraction;
	refraction = CalculateRefraction(screenPosition, normal, waterDepth);

	float4 color;
	color = CalculateBaseColor(IN.positionWS, waterDepth, hillness, refraction, noise);
	color = CombineColor(color, ripple, foam);

	MaterialOutput material;
	material = GetMaterialOutput(color, normal, viewDirWS, ripple, foam, refraction, noise);

	PatchReflections();

	SurfaceData surface;
    surface.albedo              = saturate(material.albedo);
    surface.metallic            = 0;
    surface.specular            = saturate(material.specular);
    surface.smoothness          = saturate(material.smoothness),
    surface.occlusion           = 1,
    surface.emission            = saturate(material.emission),
    surface.alpha               = saturate(material.alpha);
    surface.normalTS            = 0;
    surface.clearCoatMask       = 0;
    surface.clearCoatSmoothness = 1;

	InputData input;
	InitializeInputData(IN, normal, input);

	half4 finalColor = UniversalFragmentPBR(input, surface);
	finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

	return finalColor;
}

#endif