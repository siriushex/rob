#ifndef DRAGON_WATER_CBUFFER
#define DRAGON_WATER_CBUFFER

float4 _World_Origin_Offset;

float4 _Wave_Texture_Projection;
float _Water_Depth;
float _Smoothness;
float4 _Specular;

float4 _Shallow_Water_Color;
float4 _Deep_Water_Color;

float4 _Color_Hillness_Params;
float4 _Color_Noise_Color;
float4 _Color_Noise_Params;
float4 _Color_Noise_Texture_1_Params;
float4 _Color_Noise_Texture_2_Params;

float _SSS_Global_Intensity;
float4 _SSS_Params;

float _Normal_Global_Strength;
float4 _Normal_Map_1_Params;
float4 _Normal_Map_2_Params;

float4 _Foam_Color;
float4 _Foam_Color_Extra_1;
float4 _Foam_Color_Extra_2;
float4 _Foam_Params;
float4 _Foam_Normal_Params;
float4 _Foam_Hillness_Params;
float4 _Foam_Hillness_Params_Extra_1;
float4 _Foam_Hillness_Params_Extra_2;
float4 _Foam_Edge_Params;

float4 _Ripple_Projection;
float4 _Ripple_Noise_Color;
float4 _Ripple_Noise_Params;
float4 _Ripple_Color_Color;
float4 _Ripple_Color_Params;
float4 _Ripple_Normal_Params;
float4 _Ripple_Foam_Params;
float4 _Ripple_Material_Params;

float4 _Refraction_Params;

float _Cutout_Water_Volume_Mode;
float _Cutout_Water_Reverse;

float _Reflections_GI_Intensity;
float4 _Reflections_Glossy_Tint;

#endif