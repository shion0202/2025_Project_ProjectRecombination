// 사망~부활 연출용 전체 화면 노이즈 (아날로그 TV 노이즈 스타일).
// URP Full Screen Pass Renderer Feature의 Pass Material로 사용한다.
// 세기는 전역 값 _GlitchIntensity(0~1)로만 제어한다. (ScreenGlitch.cs)
// _GlitchIntensity를 Properties에 넣으면 머티리얼 값이 전역 값을 가리므로 넣지 않는다.
//
// 모든 파라미터는 "강도 1일 때의 세기"이며, 실제 적용량은 여기에 _GlitchIntensity를 곱한다.
Shader "Recombination/FullScreen/ScreenGlitch"
{
    Properties
    {
        [Header(Snow)]
        _NoiseStrength("Noise Strength (화면을 덮는 정도)", Range(0, 1)) = 0.85
        _NoisePixelSize("Noise Pixel Size (px)", Range(1, 8)) = 2
        _NoiseRate("Noise Rate (초당 패턴 변경 횟수)", Float) = 30

        [Header(Rolling Band)]
        _BandSpeed("Band Speed (초당 화면 비율)", Float) = 0.35
        _BandWidth("Band Width (화면 비율)", Range(0.01, 0.5)) = 0.12
        _BandBrightness("Band Brightness", Range(0, 1)) = 0.25

        [Header(Line Jitter)]
        _LineJitter("Line Jitter (UV)", Range(0, 0.02)) = 0.004
        _LineHeight("Line Height (px)", Range(1, 8)) = 2

        [Header(Flicker)]
        _Flicker("Brightness Flicker", Range(0, 1)) = 0.15

        [Header(Scanline)]
        _ScanlineCount("Scanline Count", Float) = 360
        _ScanlineStrength("Scanline Strength", Range(0, 1)) = 0.3

        [Header(Color)]
        _Desaturate("Desaturate", Range(0, 1)) = 0.5
        _Darken("Darken", Range(0, 1)) = 0.2

        [Header(Digital Glitch)]
        _TearChance("Tear Chance (가로 블록 찢어짐, 0이면 끔)", Range(0, 1)) = 0.1
        _BlockCount("Block Count", Float) = 30
        _MaxShift("Max Shift (UV)", Range(0, 0.2)) = 0.03
        _RGBSplit("RGB Split (UV)", Range(0, 0.05)) = 0.004
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ScreenGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // _BlitTexture, sampler_LinearClamp, Vert, Varyings 정의
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _GlitchIntensity;

            float _NoiseStrength;
            float _NoisePixelSize;
            float _NoiseRate;
            float _BandSpeed;
            float _BandWidth;
            float _BandBrightness;
            float _LineJitter;
            float _LineHeight;
            float _Flicker;
            float _ScanlineCount;
            float _ScanlineStrength;
            float _Desaturate;
            float _Darken;
            float _TearChance;
            float _BlockCount;
            float _MaxShift;
            float _RGBSplit;

            float Hash11(float n)
            {
                return frac(sin(n * 12.9898) * 43758.5453);
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half3 SampleSource(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = input.texcoord;
                float intensity = saturate(_GlitchIntensity);

                // 연출 중이 아닐 때는 원본을 그대로 내보낸다.
                if (intensity <= 0.0001)
                {
                    return half4(SampleSource(screenUV), 1.0);
                }

                float2 pixel = screenUV * _ScreenParams.xy;

                // 시간을 계단식으로 끊어 부드럽게 흐르지 않고 지지직 튀게 만든다.
                // 해시 입력이 커지면 sin 정밀도가 무너져 패턴이 굳으므로 범위를 제한한다.
                // (변수 이름을 step으로 지으면 HLSL 내장 함수 step()을 가려 컴파일 에러가 난다)
                float tick = fmod(floor(_Time.y * _NoiseRate), 1000.0);

                float2 uv = screenUV;

                // 1. 줄 떨림: 가로줄마다 좌우로 미세하게 흔들린다.
                float lineRow = floor(pixel.y / _LineHeight);
                uv.x += (Hash21(float2(lineRow, tick)) - 0.5) * 2.0 * _LineJitter * intensity;

                // 2. 가로 블록 찢어짐 (디지털 글리치, 기본값은 약하게)
                float blockRow = floor(screenUV.y * _BlockCount);
                float isTorn = step(1.0 - intensity * _TearChance, Hash21(float2(blockRow, tick + 3.0)));
                uv.x += (Hash21(float2(blockRow, tick + 7.0)) - 0.5) * 2.0 * _MaxShift * intensity * isTorn;

                // 3. RGB 분리
                float split = _RGBSplit * intensity;
                half3 color = half3(
                    SampleSource(uv + float2(split, 0.0)).r,
                    SampleSource(uv).g,
                    SampleSource(uv - float2(split, 0.0)).b);

                // 4. 채도 저하와 어두워짐
                half luma = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(color, luma.xxx, _Desaturate * intensity);
                color *= 1.0 - _Darken * intensity;

                // 5. 롤링 띠: 밝은 가로 띠가 아래에서 위로 흘러간다. 띠 안쪽은 노이즈도 더 짙다.
                float bandCenter = frac(_Time.y * _BandSpeed);
                float bandDistance = abs(screenUV.y - bandCenter);
                bandDistance = min(bandDistance, 1.0 - bandDistance);   // 화면 위아래를 이어서 끊기지 않게
                float band = 1.0 - smoothstep(0.0, _BandWidth, bandDistance);

                // 6. 스노우 노이즈: 흑백 점이 화면을 덮는다.
                float2 noiseCell = floor(pixel / _NoisePixelSize);
                half snow = Hash21(noiseCell + tick * 17.0);
                float snowAmount = saturate(_NoiseStrength * intensity * (1.0 + band * 0.5));
                color = lerp(color, snow.xxx, snowAmount);

                color += band * _BandBrightness * intensity;

                // 7. 스캔라인
                float scanline = sin(screenUV.y * _ScanlineCount * PI) * 0.5 + 0.5;
                color *= lerp(1.0, scanline, _ScanlineStrength * intensity);

                // 8. 밝기 깜빡임
                color *= 1.0 + (Hash11(tick) - 0.5) * 2.0 * _Flicker * intensity;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}
