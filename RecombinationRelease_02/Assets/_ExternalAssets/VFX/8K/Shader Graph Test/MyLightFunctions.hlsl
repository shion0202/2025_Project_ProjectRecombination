#ifndef MY_CUSTOM_LIGHTING_INCLUDED
#define MY_CUSTOM_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

/*
 * 1. 정점 셰이더(Vertex Stage)용 함수
 * - 정점 셰이더에서 섀도우 좌표를 계산합니다.
 */
void GetShadowCoord_float(float3 positionOS, out float4 shadowCoord)
{
    VertexPositionInputs posInputs = GetVertexPositionInputs(positionOS.xyz);
    shadowCoord = GetShadowCoord(posInputs);
}


/*
 * 2. 프래그먼트 셰이더(Fragment Stage)용 함수
 * - 그림자를 포함한 메인 라이트와 추가 라이트의 디퓨즈(NdotL) 값을 계산합니다.
 */
void GetPointLights_float(float3 PositionWS, float3 NormalWS, float4 shadowCoord, out float3 OutColor)
{
    // 출력 변수 초기화
    OutColor = float3(0, 0, 0);

// 셰이더 그래프 미리보기 오류 방지
#ifndef SHADERGRAPH_PREVIEW

    // --- 1. 메인 라이트 계산 (그림자 포함) ---
    Light mainLight = GetMainLight(shadowCoord);
    float mainLightIntensity = saturate(dot(NormalWS, mainLight.direction));
    
    // 최종 색상 = 라이트색상 * 그림자감쇠 * NdotL
    OutColor += mainLight.color * mainLight.shadowAttenuation * mainLightIntensity;

    // --- 2. 추가 라이트 계산 ---
    int lightCount = GetAdditionalLightsCount();
    for (int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, PositionWS);
        float3 lightDirection = light.direction;
        float nDotL = saturate(dot(NormalWS, lightDirection));
        
        // 최종 색상 = 라이트색상 * 거리감쇠 * NdotL
        OutColor += light.color * light.distanceAttenuation * nDotL;
    }

#endif // SHADERGRAPH_PREVIEW
}

#endif // MY_CUSTOM_LIGHTING_INCLUDED