using UnityEditor;

/// <summary>
/// 플레이하지 않고 글리치 강도를 바꿔 보며 FS_ScreenGlitch 머티리얼 파라미터를 조정하기 위한 메뉴.
/// Game 뷰에 풀스크린 패스가 적용되므로 Game 뷰를 보면서 사용한다.
/// 플레이 모드를 빠져나갈 때도 강도를 0으로 되돌려, 연출 도중 플레이를 멈췄을 때 에디터 화면에 글리치가 남지 않게 한다.
/// </summary>
[InitializeOnLoad]
public static class ScreenGlitchPreviewMenu
{
    static ScreenGlitchPreviewMenu()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ScreenGlitch.Clear();
            }
        };
    }

    [MenuItem("Tools/Screen Glitch/Preview 1.0 (사망 직후)")]
    private static void PreviewFull() => Preview(1.0f);

    [MenuItem("Tools/Screen Glitch/Preview 0.25 (리부팅 끝)")]
    private static void PreviewRecovered() => Preview(0.25f);

    [MenuItem("Tools/Screen Glitch/Clear")]
    private static void ClearPreview() => Preview(0.0f);

    private static void Preview(float intensity)
    {
        ScreenGlitch.SetIntensity(intensity);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}
