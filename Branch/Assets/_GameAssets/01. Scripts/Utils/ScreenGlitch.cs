using UnityEngine;

/// <summary>
/// 전체 화면 글리치 셰이더(Recombination/FullScreen/ScreenGlitch)의 세기를 제어한다.
///
/// 전역 셰이더 값은 씬이 바뀌어도, 에디터에서는 플레이 모드가 끝나도 남는다.
/// 연출 도중 판이 끝나면 다음 판 화면에 글리치가 남으므로, 판 종료/시작 경로에서 Clear()를 호출한다.
/// </summary>
public static class ScreenGlitch
{
    private static readonly int IntensityId = Shader.PropertyToID("_GlitchIntensity");

    public static float Intensity { get; private set; }

    public static void SetIntensity(float intensity)
    {
        Intensity = Mathf.Clamp01(intensity);
        Shader.SetGlobalFloat(IntensityId, Intensity);
    }

    public static void Clear()
    {
        SetIntensity(0.0f);
    }

    // 도메인 리로드를 끈 에디터 설정에서도 플레이 시작 시 항상 0에서 출발하게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Clear();
    }
}
