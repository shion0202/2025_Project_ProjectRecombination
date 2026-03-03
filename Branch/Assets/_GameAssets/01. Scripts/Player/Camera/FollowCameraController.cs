using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 기획자 작업 편의를 위해, 씬에서 SO 값을 변경하여 바로 적용할 수 있도록 ExecuteInEditMode 속성 추가
[ExecuteInEditMode]
public class FollowCameraController : MonoBehaviour
{
    #region Variables
    [Header("Camera Settings")]
    private CinemachineVirtualCamera vcam;
    private CinemachineFramingTransposer _cameraBody;
    private CinemachinePOV _cameraAim;
    private CinemachineInputProvider _inputProvider;

    [SerializeField] private ECameraState currentCameraState = ECameraState.Normal;
    private Dictionary<ECameraState, FollowCameraData> _cameraSettings = new Dictionary<ECameraState, FollowCameraData>();
    [SerializeField] private GameObject _owner;
    private Transform _cameraTarget;
    private bool _isBeforeZoom = false;
    private bool _isLock = false;
    private float _scrollY = 0.0f;
    private float _defaultCameraDistance = 2.0f;

    private Vector2 _lockedValue = Vector2.zero;
    private bool _isLockedByUI = false;

    [Header("Mobile Camera Settings")]
    [SerializeField] private float quickTurnDuration = 0.1f;
    private bool _isQuickTurning = false;
    private Coroutine _quickTurnCoroutine = null;
    [SerializeField] private float dragSensitivity = 0.15f;
    private int _dragFingerId = -1;                             // 현재 카메라를 드래그 중인 손가락 ID
    private Vector2 _lastMousePosition;                         // 이전 프레임의 터치 위치
    [SerializeField] private float assistRadius = 150.0f;       // 화면 중심으로부터의 픽셀 반경
    [SerializeField] private float assistStrength = 0.2f;       // 보정 강도 (0~1)
    [SerializeField] private LayerMask targetLayer;             // 보정을 할 타겟 레이어
    private Coroutine _aimAssistCoroutine = null;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilRecoverySpeed = 20.0f;
    private float _currentRecoilX = 0.0f;
    private float _currentRecoilY = 0.0f;

    [Header("Gizmos")]
    private Color deadZoneColor = Color.red;
    private Color softZoneColor = Color.blue;
    #endregion

    #region Properties
    public CinemachineVirtualCamera VCam
    {
        get => vcam;
        set => vcam = value;
    }

    public CinemachinePOV CameraAim
    {
        get => _cameraAim;
    }

    public ECameraState CurrentCameraState
    {
        get { return currentCameraState; }
        set
        {
            if (currentCameraState != value)
            {
                currentCameraState = value;
                //ApplyCameraSettings();
            }
        }
    }

    public Transform CameraTarget => _cameraTarget;

    public bool IsBeforeZoom
    {
        get { return _isBeforeZoom; }
        set { _isBeforeZoom = value; }
    }

    public bool IsZoomed
    {
        get { return currentCameraState == ECameraState.Zoom; }
        set
        {
            switch (value)
            {
                case true:
                    currentCameraState = ECameraState.Zoom;
                    break;
                case false:
                    currentCameraState = ECameraState.Normal;
                    break;
            }

            ApplyCameraSettings();
        }
    }

    public float ScrollY
    {
        get { return _scrollY; }
        set { _scrollY = value; }
    }
    #endregion

    #region Editor Methods
    private void Awake()
    {
        vcam = gameObject.GetComponent<CinemachineVirtualCamera>();
        _cameraBody = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        _cameraAim = vcam.GetCinemachineComponent<CinemachinePOV>();
        _inputProvider = gameObject.GetComponent<CinemachineInputProvider>();
    }

    private void LateUpdate()
    {
        if (_isLockedByUI)
        {
            // 축 값 고정
            _cameraAim.m_HorizontalAxis.Value = _lockedValue.x;
            _cameraAim.m_VerticalAxis.Value = _lockedValue.y;

            // 입력값은 0으로 유지해 회전 입력 중지
            _cameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
            _cameraAim.m_VerticalAxis.m_InputAxisValue = 0f;
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        foreach (ECameraState state in Enum.GetValues(typeof(ECameraState)))
        {
            _cameraSettings[state] = Resources.Load<FollowCameraData>($"Camera/FollowCameraData_{state}");
        }

        ApplyCameraSettings();
    }

    void OnDrawGizmos()
    {
        if (vcam == null) return;

        var framingTransposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Dead Zone
        Gizmos.color = deadZoneColor;
        DrawScreenRect(cam, framingTransposer.m_DeadZoneWidth, framingTransposer.m_DeadZoneHeight, framingTransposer.m_CameraDistance);

        // Soft Zone
        Gizmos.color = softZoneColor;
        DrawScreenRect(cam, framingTransposer.m_SoftZoneWidth, framingTransposer.m_SoftZoneHeight, framingTransposer.m_CameraDistance);
    }

    void DrawScreenRect(Camera cam, float width, float height, float distance)
    {
        float width_world = 2.56f;
        float height_world = 1.43f;

        float w = width * width_world * distance;
        float h = height * height_world * distance;
        Vector3 center = cam.transform.position + cam.transform.forward * (distance + 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(w, h, 0.01f));
    }
#endif
    #endregion

    #region Public Methods
    public void InitFollowCamera(GameObject owner)
    {
        _owner = owner;

        CameraTarget target = _owner.gameObject.GetComponentInChildren<CameraTarget>();
        if (target != null)
        {
            _cameraTarget = target.transform;
        }

        if (!vcam)
        {
            vcam = gameObject.GetComponent<CinemachineVirtualCamera>();
        }

        vcam.m_LookAt = _cameraTarget;
        vcam.m_Follow = _cameraTarget;

        _cameraBody = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        _cameraAim = vcam.GetCinemachineComponent<CinemachinePOV>();

        foreach (ECameraState state in Enum.GetValues(typeof(ECameraState)))
        {
            _cameraSettings[state] = Resources.Load<FollowCameraData>($"Camera/FollowCameraData_{state}");
        }

        _cameraAim.m_HorizontalAxis.Value = owner.transform.localEulerAngles.y;

        ApplyCameraSettings();

        // 현재 기획자 편의를 반영하여 Update 단에서 카메라 설정이 적용되고 있으므로, Camera State가 바뀔 때마다 Default 값 변경 필요
        _defaultCameraDistance = _cameraSettings[currentCameraState].cameraDistance;
    }

    // Update에서 매 프레임마다 실행되는 카메라 관련 함수
    public void UpdateFollowCamera()
    {
        HandleMobileCameraDrag();
        SmoothChangeCamera();
        ZoomCamera();
        HandleRecoil();

        if (_isLock)
        {
            _cameraAim.m_HorizontalAxis.m_InputAxisName = ""; // 입력 비활성화
            _cameraAim.m_VerticalAxis.m_InputAxisName = "";
            _cameraAim.m_HorizontalAxis.m_InputAxisValue = 0.0f;
            _cameraAim.m_VerticalAxis.m_InputAxisValue = 0.0f;
        }
        else
        {
            _cameraAim.m_HorizontalAxis.m_InputAxisName = "Mouse X";
            _cameraAim.m_VerticalAxis.m_InputAxisName = "Mouse Y";
        }
    }

    public void ApplyRecoil(CinemachineImpulseSource source, float recoilX, float recoilY, float force = 1.0f)
    {
        _currentRecoilX = 0.0f;
        _currentRecoilY = 0.0f;

        _currentRecoilX += recoilX * (UnityEngine.Random.value > 0.5f ? 1 : -1);
        _currentRecoilY += recoilY;
        ApplyShake(source, force);
    }

    public void ApplyShake(CinemachineImpulseSource source, float force = 1.0f)
    {
        source.m_DefaultVelocity.x = source.m_DefaultVelocity.x * (UnityEngine.Random.value > 0.5f ? 1 : -1);
        source.m_DefaultVelocity.y = source.m_DefaultVelocity.y * (UnityEngine.Random.value > 0.5f ? 1 : -1);
        source.GenerateImpulseWithForce(force);
    }

    public void SetCameraRotatable(bool lockState)
    {
        _isLock = !lockState;
    }

    public void ResetCamera()
    {
        _cameraSettings[currentCameraState].cameraDistance = _defaultCameraDistance;
    }

    public void ZoomCamera()
    {
        if (_cameraSettings[currentCameraState].cameraDistance + _scrollY <= 0.5f && _scrollY < 0)
        {
            _cameraSettings[currentCameraState].cameraDistance = 0.5f;
        }
        else if (_cameraSettings[currentCameraState].cameraDistance + _scrollY >= 3.0f && _scrollY > 0)
        {
            _cameraSettings[currentCameraState].cameraDistance = 3.0f;
        }
        else
        {
            _cameraSettings[currentCameraState].cameraDistance += _scrollY;
        }
    }

    public void OnUIOpen()
    {
        if (_inputProvider)
        {
            _inputProvider.enabled = false;  // 입력 비활성화
        }

        if (_cameraAim != null)
        {
            _lockedValue.x = _cameraAim.m_HorizontalAxis.Value;
            _lockedValue.y = _cameraAim.m_VerticalAxis.Value;

            _cameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
            _cameraAim.m_VerticalAxis.m_InputAxisValue = 0f;
        }

        _isLockedByUI = true;
    }

    public void OnUIClose()
    {
        if (_inputProvider)
        {
            _inputProvider.enabled = true;  // 입력 비활성화
        }

        _isLockedByUI = false;
    }

    public override string ToString()
    {
        string ownerName = _owner != null ? _owner.name : "Null";
        string targetName = _cameraTarget != null ? _cameraTarget.name : "Null";

        FollowCameraData currentSetting = null;
        if (_cameraSettings != null && _cameraSettings.TryGetValue(currentCameraState, out var setting))
        {
            currentSetting = setting;
        }

        // 일부 주요 정보만 표시
        string settingInfo = currentSetting != null
            ? $"Distance: {currentSetting.cameraDistance:F2}, FOV: {currentSetting.FOV:F2}, DeadZone: ({currentSetting.deadZoneWidth:F2}, " +
            $"{currentSetting.deadZoneHeight:F2}), SoftZone: ({currentSetting.softZoneWidth:F2}, {currentSetting.softZoneHeight:F2})" : "Null";

        return $"[{gameObject.name} ({GetType().Name})] State: {currentCameraState}, Owner: {ownerName}, Target: {targetName}, IsLock: {_isLock}, " +
               $"ScrollY: {_scrollY:F2}, CurrentRecoilX: {_currentRecoilX:F2}, CurrentRecoilY: {_currentRecoilY:F2}, CameraSetting: [{settingInfo}]";
    }

    public void StartQuickTurn()
    {
        if (_quickTurnCoroutine != null) return;

        // Quick Turn 중 카메라 회전, 공격, 스킬 사용 불가
        // 카메라 회전, 공격 중일 경우 취소하고 Quick Turn하며, 스킬 사용 중에는 불가능하도록 구현
        _quickTurnCoroutine = StartCoroutine(QuickTurnCoroutine());
    }

    public void ApplyAimAssist()
    {
        // 화면 중앙 좌표 계산
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // 가장 가까운 타겟 탐색
        Collider[] targets = Physics.OverlapSphere(_cameraTarget.position, 30f, targetLayer);
        Transform bestTarget = null;
        float closestScreenDistance = assistRadius;

        foreach (var target in targets)
        {
            // 적의 중심부 위치 계산
            Vector3 targetPos = target.bounds.center;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPos);

            // 카메라 뒤에 있는 적 제외
            if (screenPos.z < 0) continue;

            // 화면 중앙과의 거리 계산
            float dist = Vector2.Distance(screenCenter, new Vector2(screenPos.x, screenPos.y));

            if (dist < closestScreenDistance)
            {
                closestScreenDistance = dist;
                bestTarget = target.transform;
            }
        }

        // 타겟이 있다면 카메라 축 값을 부드럽게 보정
        if (bestTarget != null)
        {
            if (_aimAssistCoroutine != null)
            {
                StopCoroutine(_aimAssistCoroutine);
            }

            _aimAssistCoroutine = StartCoroutine(AimAssistCoroutine(bestTarget));
        }
    }
    #endregion

    #region Private Methods
    private void ApplyCameraSettings()
    {
        _cameraAim.m_HorizontalAxis.m_MaxValue = _cameraSettings[currentCameraState].maxAimRangeX;
        _cameraAim.m_HorizontalAxis.m_MinValue = _cameraSettings[currentCameraState].minAimRangeX;
        _cameraAim.m_VerticalAxis.m_MaxValue = _cameraSettings[currentCameraState].maxAimRangeY;
        _cameraAim.m_VerticalAxis.m_MinValue = _cameraSettings[currentCameraState].minAimRangeY;
        _cameraAim.m_HorizontalAxis.m_MaxSpeed = _cameraSettings[currentCameraState].sensitivityX;
        _cameraAim.m_VerticalAxis.m_MaxSpeed = _cameraSettings[currentCameraState].sensitivityY;
        _cameraAim.m_HorizontalAxis.m_AccelTime = _cameraSettings[currentCameraState].accelTimeX;
        _cameraAim.m_HorizontalAxis.m_DecelTime = _cameraSettings[currentCameraState].decelTimeX;
        _cameraAim.m_VerticalAxis.m_AccelTime = _cameraSettings[currentCameraState].accelTimeY;
        _cameraAim.m_VerticalAxis.m_DecelTime = _cameraSettings[currentCameraState].decelTimeY;

        _cameraBody.m_TrackedObjectOffset = _cameraSettings[currentCameraState].trackedOffset;
        _cameraBody.m_LookaheadTime = _cameraSettings[currentCameraState].lookaheadTime;
        _cameraBody.m_LookaheadSmoothing = _cameraSettings[currentCameraState].lookaheadSmoothing;
        _cameraBody.m_LookaheadIgnoreY = _cameraSettings[currentCameraState].ignoreLookaheadY;
        _cameraBody.m_XDamping = _cameraSettings[currentCameraState].dampingX;
        _cameraBody.m_YDamping = _cameraSettings[currentCameraState].dampingY;
        _cameraBody.m_ZDamping = _cameraSettings[currentCameraState].dampingZ;
        _cameraBody.m_TargetMovementOnly = _cameraSettings[currentCameraState].targetMovementOnly;
        _cameraBody.m_DeadZoneWidth = _cameraSettings[currentCameraState].deadZoneWidth;
        _cameraBody.m_DeadZoneHeight = _cameraSettings[currentCameraState].deadZoneHeight;
        _cameraBody.m_DeadZoneDepth = _cameraSettings[currentCameraState].deadZoneDepth;
        _cameraBody.m_SoftZoneWidth = _cameraSettings[currentCameraState].softZoneWidth;
        _cameraBody.m_SoftZoneHeight = _cameraSettings[currentCameraState].softZoneHeight;
        _cameraBody.m_BiasX = _cameraSettings[currentCameraState].softZoneOffsetX;
        _cameraBody.m_BiasY = _cameraSettings[currentCameraState].softZoneOffsetY;
    }

    private void SmoothChangeCamera()
    {
        vcam.m_Lens.FieldOfView = Mathf.Lerp(vcam.m_Lens.FieldOfView, _cameraSettings[currentCameraState].FOV, _cameraSettings[currentCameraState].convertSpeed * Time.deltaTime);
        _cameraBody.m_ScreenX = Mathf.Lerp(_cameraBody.m_ScreenX, _cameraSettings[currentCameraState].screenX, _cameraSettings[currentCameraState].convertSpeed * Time.deltaTime);
        _cameraBody.m_ScreenY = Mathf.Lerp(_cameraBody.m_ScreenY, _cameraSettings[currentCameraState].screenY, _cameraSettings[currentCameraState].convertSpeed * Time.deltaTime);
        _cameraBody.m_CameraDistance = Mathf.Lerp(_cameraBody.m_CameraDistance, _cameraSettings[currentCameraState].cameraDistance, _cameraSettings[currentCameraState].convertSpeed * Time.deltaTime);

        ApplyCameraSettings();
    }

    private void HandleRecoil()
    {
        if (_currentRecoilX != 0 || _currentRecoilY > 0)
        {
            _cameraAim.m_HorizontalAxis.Value += _currentRecoilX * Time.deltaTime;
            _cameraAim.m_VerticalAxis.Value -= _currentRecoilY * Time.deltaTime;

            float recoveryStep = recoilRecoverySpeed * Time.deltaTime;
            _currentRecoilX = Mathf.MoveTowards(_currentRecoilX, 0, recoveryStep);
            _currentRecoilY = Mathf.Max(0, _currentRecoilY - recoveryStep);
        }
    }

    private IEnumerator QuickTurnCoroutine()
    {
        _isQuickTurning = true;

        // 회전 시작 시 현재의 입력 잠금 상태를 저장하거나 강제로 잠금
        bool wasLocked = _isLock;
        _isLock = true;

        float elapsedTime = 0f;
        float startX = _cameraAim.m_HorizontalAxis.Value;
        float targetX = startX + 180.0f; // 180도 뒤로 목표 설정

        while (elapsedTime < quickTurnDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / quickTurnDuration;

            // SmoothStep을 사용하여 부드러운 가속/감속 효과 적용
            float smoothT = Mathf.SmoothStep(0, 1, t);
            _cameraAim.m_HorizontalAxis.Value = Mathf.Lerp(startX, targetX, smoothT);

            yield return null;
        }

        // 최종 각도 보정 및 상태 복구
        _cameraAim.m_HorizontalAxis.Value = targetX;
        _isLock = wasLocked; // 원래 잠금 상태로 복구

        // Player State도 복구
        PlayerController player = _owner.GetComponent<PlayerController>();
        if (player)
        {
            player.SetPlayerState(EPlayerState.QuickTurning, false);
        }

        _isQuickTurning = false;
        _quickTurnCoroutine = null;
    }

    private bool IsPointerOverUI(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;

        // 포인터 위치 결정
        Vector2 pointerPosition;
        if (fingerId == -1)
            pointerPosition = Input.mousePosition;
        else if (Input.touchCount > fingerId)
            pointerPosition = Input.GetTouch(fingerId).position;
        else
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = pointerPosition;

        // 모든 UI 요소를 대상으로 레이캐스트 실행
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    private void HandleMobileCameraDrag()
    {
        // 특정 상황에서 카메라 회전이 불가능하도록 제어
        if (_isQuickTurning || _isLockedByUI || _isLock || _quickTurnCoroutine != null) return;

        // EventSystem 안전 장치 (씬에 EventSystem이 없거나 초기화 안 된 경우 방지)
        if (UnityEngine.EventSystems.EventSystem.current == null) return;

        // 모바일 터치 처리 (멀티 터치 대응)
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // 터치 시작 시: UI 버튼 위가 아닌 경우에만 드래그 시작
                if (touch.phase == TouchPhase.Began)
                {
                    if (IsPointerOverUI(touch.fingerId)) continue;

                    _dragFingerId = touch.fingerId;
                    _lastMousePosition = touch.position;
                }
                // 드래그 중: 시작할 때 잡은 손가락만 추적 (UI 영역 무시)
                else if (touch.fingerId == _dragFingerId)
                {
                    if (touch.phase == TouchPhase.Moved)
                    {
                        float deltaX = touch.position.x - _lastMousePosition.x;
                        float deltaY = touch.position.y - _lastMousePosition.y;

                        // 시네머신 축 값에 직접 더함 (떨림 방지)
                        _cameraAim.m_HorizontalAxis.Value += deltaX * dragSensitivity;
                        _cameraAim.m_VerticalAxis.Value -= deltaY * dragSensitivity;

                        _lastMousePosition = touch.position;
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        _dragFingerId = -1;
                    }
                }
            }
        }
    }

    private IEnumerator AimAssistCoroutine(Transform target)
    {
        float elapsed = 0.0f;
        float duration = 0.1f; // 보정 시간

        float startX = _cameraAim.m_HorizontalAxis.Value;
        float startY = _cameraAim.m_VerticalAxis.Value;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // 현재 적을 바라보기 위한 목표 회전값 계산
            Vector3 dirToTarget = (target.position - Camera.main.transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);

            // 현재 카메라의 POV 축 값을 타겟 방향으로 살짝 보정
            // POV의 각도는 오일러 각도를 따르므로 직접 값을 부드럽게 섞음 (POV Vertical 축 범위 대응)
            float targetX = targetRot.eulerAngles.y;
            float targetY = targetRot.eulerAngles.x;
            if (targetY > 180)
            {
                targetY -= 360.0f;
            }

            float deltaX = Mathf.DeltaAngle(_cameraAim.m_HorizontalAxis.Value, targetX);
            float deltaY = Mathf.DeltaAngle(_cameraAim.m_VerticalAxis.Value, targetY);

            _cameraAim.m_HorizontalAxis.Value += deltaX * assistStrength * smoothT;
            _cameraAim.m_VerticalAxis.Value += deltaY * assistStrength * smoothT;

            yield return null;
        }
    }
    #endregion
}
