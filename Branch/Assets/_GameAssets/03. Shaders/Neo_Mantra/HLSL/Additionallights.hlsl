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

// SDF 방향 가중치의 지수. 클수록 가장 강한 라이트가 방향을 독차지하고, 세기가 비슷할 때만 섞인다.
#define SDF_LIGHT_WEIGHT_POWER 3.0
// 가중 평균 방향이 이 비율보다 약하면(반대편 라이트끼리 상쇄) 가장 강한 라이트 방향을 쓴다.
#define SDF_LIGHT_CANCEL_THRESHOLD 0.3
// 카메라에서 이 거리(m) 안에 있는 라이트는 카메라에 붙은 라이트로 보고 SDF 방향에서 뺀다.
#define SDF_CAMERA_LIGHT_RADIUS 0.5

// 추가 라이트가 카메라에 붙어 있는지. FollowCamera의 스포트 라이트처럼 카메라와 함께 움직이는 라이트를
// SDF 방향에 쓰면 카메라를 돌릴 때 얼굴 그림자가 같이 돈다.
bool IsCameraAttachedLight(int additionalLightIndex)
{
#if USE_FORWARD_PLUS
    // Forward+에서는 라이트 순회 방식이 달라 판별하지 않는다. (현재 렌더러는 Forward)
    return false;
#else
    int lightIndex = GetPerObjectLightIndex(additionalLightIndex);
  #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float3 lightPositionWS = _AdditionalLightsBuffer[lightIndex].position.xyz;
  #else
    float3 lightPositionWS = _AdditionalLightsPosition[lightIndex].xyz;
  #endif
    return distance(lightPositionWS, _WorldSpaceCameraPos) < SDF_CAMERA_LIGHT_RADIUS;
#endif
}

void GetPointLightsForSDF_float(float3 PositionWS, float3 NormalWS,
                          out float3 OutColor, out float3 SDFDirection)
{
#if defined(SHADERGRAPH_PREVIEW)
    OutColor     = float3(0, 0, 0);
    SDFDirection = float3(0, 0, 1);
#else
    OutColor = float3(0, 0, 0);

    // 1) 메인 라이트는 색/밝기에만 더한다.
    // 메인 라이트는 카메라(FollowCamera)에 붙은 디렉셔널 라이트라, SDF 방향에 쓰면 카메라를 돌릴 때 얼굴 그림자가 같이 돈다.
    Light mainLight = GetMainLight();

    float mainNdotL = saturate(dot(NormalWS, mainLight.direction));
    OutColor += mainLight.color * mainNdotL;

    // 2) 추가 라이트: 색/밝기를 더하고, SDF 방향은 추가 라이트들로 정한다.
    // 가중치는 라이트 밝기와 거리 감쇠만 쓴다. 표면 방향(NdotL)을 넣으면 픽셀마다 방향이 달라져 SDF가 일그러진다.
    // 가중치를 거듭제곱해 가장 강한 라이트 위주로 두고, 세기가 비슷할 때만 부드럽게 섞는다.
    float3 weightedDirection = float3(0, 0, 0);
    float totalWeight = 0;
    float strongestWeight = 0;
    float3 strongestDirection = mainLight.direction;

    int lightCount = GetAdditionalLightsCount();
    for (int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, PositionWS);
        float nDotL = saturate(dot(NormalWS, light.direction));

        OutColor += light.color * light.distanceAttenuation * nDotL;

        // 카메라에 붙은 라이트는 밝기에만 반영하고 방향에서는 뺀다.
        if (IsCameraAttachedLight(i)) continue;

        float strength = dot(light.color, float3(0.2126, 0.7152, 0.0722)) * light.distanceAttenuation;
        float weight = pow(max(strength, 0.0), SDF_LIGHT_WEIGHT_POWER);
        weightedDirection += light.direction * weight;
        totalWeight += weight;

        if (strength > strongestWeight)
        {
            strongestWeight = strength;
            strongestDirection = light.direction;
        }
    }

    // 3) SDF 방향 결정
    // 주변에 추가 라이트가 없으면 메인 라이트 방향을 쓴다. (카메라를 따라 돌지만 달리 기준이 없다)
    // 반대편 라이트끼리 상쇄되어 평균 방향이 약하면 가장 강한 라이트 방향을 쓴다.
    float weightedLength = length(weightedDirection);
    if (totalWeight <= 1e-5)
    {
        SDFDirection = normalize(mainLight.direction);
    }
    else if (weightedLength < totalWeight * SDF_LIGHT_CANCEL_THRESHOLD)
    {
        SDFDirection = normalize(strongestDirection);
    }
    else
    {
        SDFDirection = weightedDirection / weightedLength;
    }
#endif
}
#endif