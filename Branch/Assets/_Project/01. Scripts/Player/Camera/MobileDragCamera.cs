using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// 모바일 화면 드래그(거리 기반)로 Cinemachine POV 카메라를 회전시키는 최종 개선 스크립트.
/// (수정됨: PointerEventData 컴파일 에러 해결)
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
[DisallowMultipleComponent]
public class MobileDragCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("Rotation Settings")]
    [Tooltip("화면을 1인치 드래그했을 때 회전하는 각도 계수(감도).")]
    [SerializeField] private float sensitivity = 15f;

    [Tooltip("아래로 드래그 시 위를 보는 반전 옵션.")]
    [SerializeField] private bool invertY = false;

    [Tooltip("프레임당 반영할 최대 손가락 이동(인치). 급격한 스와이프 튐 방지.")]
    [SerializeField] private float maxDeltaInches = 1.0f;

    [Tooltip("미세 떨림 제거용 데드존(인치).")]
    [SerializeField] private float deadZoneInches = 0.005f;

    [Header("Input Policy")]
    [Tooltip("오른쪽 절반에서만 카메라 드래그 허용.")]
    [SerializeField] private bool useRightHalfOnly = true;

    [Tooltip("안전영역(노치 등)을 고려하여 조작 영역 계산.")]
    [SerializeField] private bool considerSafeArea = true;

    [Tooltip("UI 위 터치는 무시.")]
    [SerializeField] private bool ignoreUI = true;

    [Tooltip("다중 터치 시 회전 중단 (핀치 줌 등과 충돌 방지).")]
    [SerializeField] private bool blockOnMultiTouch = true;

    [Tooltip("드래그 중 지정 영역을 벗어나면 제어 해제.")]
    [SerializeField] private bool releaseOnAreaExit = false;

    [Tooltip("드래그 중 UI 위로 진입하면 제어 해제.")]
    [SerializeField] private bool releaseOnUIEnter = true;

    [Header("Smoothing")]
    [Tooltip("스무딩 시간(초). 0이면 즉각 반응.")]
    [SerializeField] private float smoothingTime = 0.05f;

    [Header("Normalization")]
    [Tooltip("DPI 정보를 가져올 수 없을 때 사용할 기본값.")]
    [SerializeField] private float dpiFallback = 200f;

    [Header("Cinemachine Integration")]
    [Tooltip("활성화 시 CinemachineInputProvider 비활성화 (이중 입력 방지).")]
    [SerializeField] private bool disableInputProvider = true;

    [Tooltip("POV 축 설정 초기화 (내부 댐핑 제거, Wrap 설정 등).")]
    [SerializeField] private bool configurePOVAxesOnAwake = true;

#if UNITY_EDITOR
    [Header("Editor Simulation")]
    [Tooltip("에디터에서 마우스로 터치 시뮬레이션.")]
    [SerializeField] private bool editorEnableTouchSimulation = true;
#endif

    // --- Internal State ---
    private CinemachinePOV _pov;
    private Finger _cameraFinger;
    private Vector2 _filteredDelta;

    // Restore Data
    private string _xAxisName;
    private string _yAxisName;
    private CinemachineInputProvider _inputProvider;
    private bool _inputProviderWasEnabled;

    // Cache Data
    private Rect _rightAreaRect;
    private Vector2 _lastScreenSize;
    private Rect _lastSafeArea;

    // GC Free Raycast & EventSystem Caching
    private List<RaycastResult> _raycastResults;
    private PointerEventData _pointerEventData;
    private EventSystem _cachedEventSystem; // [수정] EventSystem 변경 감지용

    private void Awake()
    {
        if (vcam == null)
            vcam = GetComponent<CinemachineVirtualCamera>();

        _pov = vcam != null ? vcam.GetCinemachineComponent<CinemachinePOV>() : null;
        if (_pov == null)
        {
            Debug.LogWarning($"[{nameof(MobileDragCamera)}] CinemachinePOV가 없습니다.", this);
            return;
        }

        // 기존 축 이름 백업 및 제거 (충돌 방지)
        _xAxisName = _pov.m_HorizontalAxis.m_InputAxisName;
        _yAxisName = _pov.m_VerticalAxis.m_InputAxisName;
        _pov.m_HorizontalAxis.m_InputAxisName = string.Empty;
        _pov.m_VerticalAxis.m_InputAxisName = string.Empty;

        // POV 내부 설정 최적화
        if (configurePOVAxesOnAwake)
            ConfigurePOVAxes(_pov);

        // InputProvider 제어
        _inputProvider = vcam.GetComponent<CinemachineInputProvider>();
        if (_inputProvider != null)
        {
            _inputProviderWasEnabled = _inputProvider.enabled;
            if (disableInputProvider)
                _inputProvider.enabled = false;
        }

        // GC 방지용 객체 미리 할당
        _raycastResults = new List<RaycastResult>();
        
        // [수정] EventSystem 초기 캐싱
        if (EventSystem.current != null)
        {
            _cachedEventSystem = EventSystem.current;
            _pointerEventData = new PointerEventData(_cachedEventSystem);
        }

        // 초기화
        UpdateRightAreaRect();
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _lastSafeArea = Screen.safeArea;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
        if (editorEnableTouchSimulation)
            TouchSimulation.Enable();
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        TouchSimulation.Disable();
#endif
        // EnhancedTouchSupport.Disable(); // 안전을 위해 주석 처리

        RestoreAxisNames();

        if (_inputProvider != null && disableInputProvider)
            _inputProvider.enabled = _inputProviderWasEnabled;
    }

    private void OnDestroy()
    {
        RestoreAxisNames();
        if (_inputProvider != null && disableInputProvider)
            _inputProvider.enabled = _inputProviderWasEnabled;
    }

    private void Update()
    {
        if (_pov == null) return;

        // 해상도/SafeArea 변경 감지 시에만 영역 재계산
        bool screenChanged = (_lastScreenSize.x != Screen.width) || (_lastScreenSize.y != Screen.height);
        bool safeAreaChanged = _lastSafeArea != Screen.safeArea;

        if (screenChanged || safeAreaChanged)
        {
            UpdateRightAreaRect();
            _lastScreenSize.x = Screen.width;
            _lastScreenSize.y = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        // 멀티터치 시 회전 중단
        if (blockOnMultiTouch && Touch.activeTouches.Count >= 2)
        {
            ReleaseFinger();
            return;
        }

        // 손가락 획득 시도
        if (_cameraFinger == null)
            TryAcquireFinger();

        if (_cameraFinger == null) return;

        var currentTouch = _cameraFinger.currentTouch;

        // 유효성 검사
        if (!currentTouch.valid ||
            currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
            currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            ReleaseFinger();
            return;
        }

        // 영역 이탈 검사
        if (releaseOnAreaExit && useRightHalfOnly && !IsInRightArea(currentTouch.screenPosition))
        {
            ReleaseFinger();
            return;
        }

        // UI 진입 검사
        if (releaseOnUIEnter && ignoreUI && IsTouchOverUI(currentTouch))
        {
            ReleaseFinger();
            return;
        }

        if (currentTouch.phase != UnityEngine.InputSystem.TouchPhase.Moved) return;

        // --- 회전 로직 ---

        // 1. 델타 계산 및 정규화
        Vector2 deltaInches = ComputeDeltaInches(currentTouch.delta);

        // 2. 데드존 및 클램프
        if (deltaInches.sqrMagnitude < deadZoneInches * deadZoneInches) return;
        deltaInches = Vector2.ClampMagnitude(deltaInches, maxDeltaInches);

        // 3. 지수 스무딩
        float dt = Time.unscaledDeltaTime;
        float alpha = (smoothingTime < 1e-4f) ? 1f : 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, smoothingTime));
        _filteredDelta = Vector2.Lerp(_filteredDelta, deltaInches, alpha);

        // 4. 값 적용
        float yFactor = invertY ? 1f : -1f;
        _pov.m_HorizontalAxis.Value += _filteredDelta.x * sensitivity;
        _pov.m_VerticalAxis.Value   += _filteredDelta.y * sensitivity * yFactor;

        // 수평 축 값 정규화
        if (_pov.m_HorizontalAxis.m_Wrap)
        {
            _pov.m_HorizontalAxis.Value %= 360f;
        }

        // 수직 축 클램프
        if (!_pov.m_VerticalAxis.m_Wrap)
        {
            _pov.m_VerticalAxis.Value = Mathf.Clamp(
                _pov.m_VerticalAxis.Value,
                _pov.m_VerticalAxis.m_MinValue,
                _pov.m_VerticalAxis.m_MaxValue
            );
        }
    }

    // --- Helper Methods ---

    private void TryAcquireFinger()
    {
        // 1. Began 우선
        foreach (var t in Touch.activeTouches)
        {
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began && ShouldUseTouch(t))
            {
                _cameraFinger = t.finger;
                _filteredDelta = Vector2.zero;
                return;
            }
        }
        // 2. Moved/Stationary 차선
        foreach (var t in Touch.activeTouches)
        {
            if ((t.phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                 t.phase == UnityEngine.InputSystem.TouchPhase.Stationary) && ShouldUseTouch(t))
            {
                _cameraFinger = t.finger;
                _filteredDelta = Vector2.zero;
                return;
            }
        }
    }

    private bool ShouldUseTouch(Touch t)
    {
        if (ignoreUI && IsTouchOverUI(t)) return false;
        if (useRightHalfOnly && !IsInRightArea(t.screenPosition)) return false;
        return true;
    }

    private bool IsTouchOverUI(Touch t)
    {
        if (EventSystem.current == null) return false;

        // 1차: 포인터 ID 확인
        if (EventSystem.current.IsPointerOverGameObject(t.touchId)) return true;

        // 2차: Raycast (GC Free, EventSystem 캐싱 활용)
        // [수정] EventSystem이 바뀌었거나 PointerEventData가 없는 경우 재생성
        if (_pointerEventData == null || _cachedEventSystem != EventSystem.current)
        {
            _cachedEventSystem = EventSystem.current;
            _pointerEventData = new PointerEventData(_cachedEventSystem);
        }

        _pointerEventData.position = t.screenPosition;
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        return _raycastResults.Count > 0;
    }

    private bool IsInRightArea(Vector2 screenPos)
    {
        if (!useRightHalfOnly) return true;
        return _rightAreaRect.Contains(screenPos);
    }

    private void UpdateRightAreaRect()
    {
        if (!useRightHalfOnly)
        {
            _rightAreaRect = new Rect(0, 0, Screen.width, Screen.height);
            return;
        }

        if (considerSafeArea)
        {
            var sa = Screen.safeArea;
            _rightAreaRect = new Rect(sa.xMin + sa.width * 0.5f, sa.yMin, sa.width * 0.5f, sa.height);
        }
        else
        {
            _rightAreaRect = new Rect(Screen.width * 0.5f, 0f, Screen.width * 0.5f, Screen.height);
        }
    }

    private Vector2 ComputeDeltaInches(Vector2 pixelDelta)
    {
        float dpi = Screen.dpi > 0f ? Screen.dpi : dpiFallback;
        return pixelDelta / dpi;
    }

    private void ReleaseFinger()
    {
        _cameraFinger = null;
        _filteredDelta = Vector2.zero;
    }

    private void RestoreAxisNames()
    {
        if (_pov == null) return;
        _pov.m_HorizontalAxis.m_InputAxisName = _xAxisName;
        _pov.m_VerticalAxis.m_InputAxisName = _yAxisName;
    }

    private void ConfigurePOVAxes(CinemachinePOV pov)
    {
        pov.m_HorizontalAxis.m_Wrap = true;
        pov.m_VerticalAxis.m_Wrap = false;

        // 이중 스무딩 제거
        pov.m_HorizontalAxis.m_AccelTime = 0f;
        pov.m_HorizontalAxis.m_DecelTime = 0f;
        pov.m_VerticalAxis.m_AccelTime = 0f;
        pov.m_VerticalAxis.m_DecelTime = 0f;

        if (pov.m_VerticalAxis.m_MaxValue <= pov.m_VerticalAxis.m_MinValue)
        {
            pov.m_VerticalAxis.m_MinValue = -80f;
            pov.m_VerticalAxis.m_MaxValue = 80f;
        }
        
        // 리센터링 비활성화 (API 수정됨)
        pov.m_HorizontalAxis.m_Recentering.m_enabled = false;
        pov.m_HorizontalAxis.m_Recentering.m_WaitTime = 0f;
        pov.m_HorizontalAxis.m_Recentering.m_RecenteringTime = 0f;

        pov.m_VerticalAxis.m_Recentering.m_enabled = false;
        pov.m_VerticalAxis.m_Recentering.m_WaitTime = 0f;
        pov.m_VerticalAxis.m_Recentering.m_RecenteringTime = 0f;
    }
}