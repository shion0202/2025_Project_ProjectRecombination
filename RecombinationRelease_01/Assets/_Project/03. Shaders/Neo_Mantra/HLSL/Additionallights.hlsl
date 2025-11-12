#ifndef ADDITIONALLIGHTS_INCLUDED
#define ADDITIONALLIGHTS_INCLUDED

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void GetPointLights_float(float3 PositionWS, float3 NormalWS, out float3 OutColor, out float3 AvgDirection)
{
#if defined(SHADERGRAPH_PREVIEW)
    OutColor = float3(0, 0, 0);
    AvgDirection = float3(0, 0, 1); // 기본 방향 설정
#else
    // 출력 변수 초기화
    OutColor = float3(0, 0, 0);

    float3 sumDirection = float3(0, 0, 0);
    float totalIntensity = 0;
    
    // 메인 라이트 계산
    Light mainLight = GetMainLight();
    float mainLightIntensity = saturate(dot(NormalWS, mainLight.direction));
    OutColor += mainLight.color * mainLightIntensity;
    
    float mainLightWeight = mainLightIntensity; // 또는 mainLightIntensity * mainLight.color의 밝기
    sumDirection += mainLight.direction * mainLightWeight;
    totalIntensity += mainLightWeight;

    // 추가 라이트 계산
    int lightCount = GetAdditionalLightsCount();
    for (int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, PositionWS);
        float3 lightDirection = light.direction; // .position 대신 .direction 사용
        float nDotL = saturate(dot(NormalWS, lightDirection));
        OutColor += light.color * light.distanceAttenuation * nDotL;
        
        float weight = nDotL * light.distanceAttenuation; // 필요 시 color의 세기도 곱하기
        sumDirection += light.direction * weight;
        totalIntensity += weight;
    }
    
    // 합산 벡터 정규화
    AvgDirection = (totalIntensity > 0) ? normalize(sumDirection / totalIntensity) : float3(0, 0, 1);
#endif
}

void GetFirstPointLight_float(float3 PositionWS, float3 NormalWS, out float3 OutDirection)
{
#if defined(SHADERGRAPH_PREVIEW)
    OutDirection = float3(0, 0, 0);
#else
    // 출력 변수 초기화
    OutDirection = float3(0, 0, 0);

    float maxWeight = 0;
    float3 selectedDirection = float3(0, 0, 1);
    
    // 추가 라이트 계산
    int lightCount = GetAdditionalLightsCount();
    for (int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, PositionWS);

        float nDotL = saturate(dot(NormalWS, light.direction));
        float weight = nDotL * light.distanceAttenuation;

        if (weight > maxWeight)
        {
            maxWeight = weight;
            selectedDirection = light.direction;
        }
    }
    
    // Spot Light일 경우
    OutDirection = selectedDirection;
#endif
}

// 메시 중앙 혹은 월드 기준점(예: BoundsCenter) 등 고정 위치 사용
void GetSubLight_float(float3 ReferencePosWS, float3 ReferenceNormalWS, out float3 OutColor, out float3 OutDirection)
{
#if defined(SHADERGRAPH_PREVIEW)
    OutDirection = float3(0, 0, 0);
    OutColor = 0;
#else

// 기준점에서 최강 라이트 한 번만 선택
    float maxWeight = 0;
    float3 selectedDirection = float3(0, 0, 1);
    float3 selectedColor = float3(0, 0, 0);

// main light
    Light mainLight = GetMainLight();
    float nDotL = saturate(dot(ReferenceNormalWS, mainLight.direction));
    float weight = nDotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
    if (weight > maxWeight)
    {
        maxWeight = weight;
        selectedDirection = mainLight.direction;
        selectedColor = mainLight.color.rgb * weight;
    }

// 추가 라이트들
    for (int i = 0; i < GetAdditionalLightsCount(); ++i)
    {
        Light light = GetAdditionalLight(i, ReferencePosWS);
        float nDotL = saturate(dot(ReferenceNormalWS, light.direction));
        float weight = nDotL * light.distanceAttenuation * light.shadowAttenuation;
        if (weight > maxWeight)
        {
            maxWeight = weight;
            selectedDirection = light.direction;
            selectedColor = light.color.rgb * weight;
        }
    }

// 해당 라이트 정보를 머터리얼 전체에 사용
    OutDirection = selectedDirection;
    OutColor = selectedColor;
#endif
}
#endif