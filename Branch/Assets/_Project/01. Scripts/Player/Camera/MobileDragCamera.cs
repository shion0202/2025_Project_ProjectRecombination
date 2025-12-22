using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MobileDragCamera : MonoBehaviour
{
    public CinemachineVirtualCamera vcam;

    [Header("회전 설정")]
    public float rotationSpeed = 260f;    // 초당 최대 회전 각도(대략 값, 필요시 조절)
    public float maxDeltaInches = 1.0f;   // 한 프레임에 반영할 최대 손가락 이동(인치)
    public float deadZoneInches = 0.01f;  // 이보다 작은 움직임은 무시
    public bool useRightHalfOnly = true;  // 화면 오른쪽 절반만 카메라용

    private CinemachinePOV _pov;
    private Finger _cameraFinger;

    private void Awake()
    {
        if (vcam == null)
            vcam = GetComponent<CinemachineVirtualCamera>();

        if (vcam != null)
            _pov = vcam.GetCinemachineComponent<CinemachinePOV>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
    }

    private void OnDisable()
    {
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return; // PC/에디터에서는 기존 마우스/패드 입력 사용
#endif
        if (_pov == null)
            return;

        // 아직 카메라용 손가락이 없으면 새로 선택
        if (_cameraFinger == null)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                    continue;

                if (useRightHalfOnly && touch.screenPosition.x < Screen.width * 0.5f)
                    continue;

                _cameraFinger = touch.finger;
                break;
            }
        }

        if (_cameraFinger == null)
            return;

        var currentTouch = _cameraFinger.currentTouch;

        if (!currentTouch.valid)
        {
            _cameraFinger = null;
            return;
        }

        if (currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
            currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            _cameraFinger = null;
            return;
        }

        if (currentTouch.phase != UnityEngine.InputSystem.TouchPhase.Moved)
            return;

        // 픽셀 → 인치로 정규화 (기기 DPI 차이 보정)
        float dpi = Screen.dpi > 0 ? Screen.dpi : 200f;
        Vector2 deltaInches = currentTouch.delta / dpi;

        // 데드존 처리 (미세 떨림 제거)
        if (deltaInches.sqrMagnitude < deadZoneInches * deadZoneInches)
            return;

        // 너무 큰 스와이프는 일정 값까지만 반영
        deltaInches = Vector2.ClampMagnitude(deltaInches, maxDeltaInches);

        float dt = Time.deltaTime;

        // 인치당 회전 속도 * 시간
        _pov.m_HorizontalAxis.Value += deltaInches.x * rotationSpeed * dt;
        _pov.m_VerticalAxis.Value -= deltaInches.y * rotationSpeed * dt;
    }
}
