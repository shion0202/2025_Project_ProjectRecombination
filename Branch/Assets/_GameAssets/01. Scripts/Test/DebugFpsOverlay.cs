#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [임시] F7 키로 실제 프레임(프레임 간격 기준)을 화면에 표시한다. 프레임 제한이 적용되는지 확인하는 개발용 컴포넌트다.
/// 에디터 Stats 창의 FPS는 한 프레임을 그리는 데 걸린 시간으로 계산해 제한 대기 시간을 빼므로 실제보다 높게 나온다.
/// 씬 배치 없이 게임 시작 시 자동 생성되며, 에디터와 Development Build에서만 컴파일된다.
/// </summary>
public class DebugFpsOverlay : MonoBehaviour
{
    private const float UpdateInterval = 0.5f;

    private bool _isVisible;
    private float _accumulatedTime;
    private int _frames;
    private float _averageFps;
    private float _worstFrameMs;
    private float _intervalWorstMs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject(nameof(DebugFpsOverlay));
        DontDestroyOnLoad(go);
        go.AddComponent<DebugFpsOverlay>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
        {
            _isVisible = !_isVisible;
        }

        // 일시정지(timeScale 0)에도 실제 프레임을 재야 하므로 unscaledDeltaTime을 쓴다.
        float dt = Time.unscaledDeltaTime;
        _accumulatedTime += dt;
        _frames++;
        _intervalWorstMs = Mathf.Max(_intervalWorstMs, dt * 1000.0f);

        if (_accumulatedTime >= UpdateInterval)
        {
            _averageFps = _frames / _accumulatedTime;
            _worstFrameMs = _intervalWorstMs;
            _accumulatedTime = 0.0f;
            _frames = 0;
            _intervalWorstMs = 0.0f;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible) return;

        string text = $"FPS {_averageFps:F1} (최대 프레임 간격 {_worstFrameMs:F1}ms) | " +
                      $"VSync {QualitySettings.vSyncCount}, 목표 {Application.targetFrameRate}";
        GUI.Label(new Rect(Screen.width - 520.0f, 20.0f, 500.0f, 25.0f), text);
    }
}
#endif
