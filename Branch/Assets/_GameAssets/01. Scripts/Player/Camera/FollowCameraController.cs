using Cinemachine;
using Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    // 감도 슬라이더 값(50~300). 저장/UI 표시는 이 범위를 그대로 쓴다.
    private float dragSensitivityX = DefaultSensitivity;
    private float dragSensitivityY = DefaultSensitivity;

    #region 감도 스케일 환산

    // 카메라 계산식들은 슬라이더 값을 그대로 쓰도록 작성되어 있는데,
    // 그 기준이 모바일 드래그라 PC 마우스에는 전 구간이 과했다. (최소치로 내려도 빠르다는 피드백)
    // 계산식을 일일이 고치는 대신, 슬라이더 값을 아래 스케일로 환산해 넘긴다.
    //   슬라이더 50  -> 5
    //   슬라이더 300 -> 200
    // PC 마우스뿐 아니라 게임패드/모바일 경로도 같은 값을 쓰므로 환산을 한곳에 모은다.
    public const float SensitivityMin = 50.0f;
    public const float SensitivityMax = 300.0f;
    public const float DefaultSensitivity = 150.0f;

    private const float LegacyAtMin = 5.0f;
    private const float LegacyAtMax = 200.0f;

    private static float ToLegacyScale(float slider)
    {
        return Mathf.Lerp(LegacyAtMin, LegacyAtMax, Mathf.InverseLerp(SensitivityMin, SensitivityMax, slider));
    }

    private float LegacySensitivityX => ToLegacyScale(dragSensitivityX);
    private float LegacySensitivityY => ToLegacyScale(dragSensitivityY);

    #endregion
    private int _dragFingerId = -1;                             // 현재 카메라를 드래그 중인 손가락 ID
    private Vector2 _lastMousePosition;                         // 이전 프레임의 터치 위치
    [SerializeField] private float assistRadius = 150.0f;       // 화면 중심으로부터의 픽셀 반경
    [SerializeField] private float assistStrength = 0.2f;       // 보정 강도 (0~1)
    [SerializeField] private LayerMask targetLayer;             // 보정을 할 타겟 레이어
    private Coroutine _aimAssistCoroutine = null;
    private Transform _currentAssistTarget = null;
    private bool _isContinuousAssist = false;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilRecoverySpeed = 20.0f;
    private float _currentRecoilX = 0.0f;
    private float _currentRecoilY = 0.0f;

    [Header("Gizmos")]
    private Color deadZoneColor = Color.red;
    private Color softZoneColor = Color.blue;

    [Header("Mobile UI Exception Settings")]
    [Tooltip("UI 레이캐스트를 무시하고 카메라 회전을 허용할 UI 오브젝트의 이름 목록 (예: 사격 버튼 3종)")]
    [SerializeField] private List<string> cameraPermittedUIList = new List<string>() { "Btn_LeftAttack", "Btn_RightAttack", "Btn_BothAttack" };
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

    public CinemachinePOV ActiveCameraAim
    {
        get => _cameraAim;
        set => _cameraAim = value;
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
            ActiveCameraAim.m_HorizontalAxis.Value = _lockedValue.x;
            ActiveCameraAim.m_VerticalAxis.Value = _lockedValue.y;

            // 입력값은 0으로 유지해 회전 입력 중지
            ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
            ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0f;
        }
    }

#if UNITY_EDITOR
    // 기획자가 SO 값을 바꾸면 플레이하지 않고도 바로 보이게 하는 에디터 전용 갱신이다.
    // 플레이 중에는 UpdateFollowCamera -> SmoothChangeCamera가 이미 매 프레임
    // ApplyCameraSettings를 부르므로 중복이고, 플레이 종료 시점에 여기서 다시 쓰면
    // 그 값이 씬에 남아 Bootstrap의 카메라 설정이 계속 수정된 것으로 잡힌다.
    private void Update()
    {
        if (Application.isPlaying) return;

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

        ActiveCameraAim.m_HorizontalAxis.Value = owner.transform.localEulerAngles.y;

        dragSensitivityX = PlayerPrefs.GetFloat("SensitivityX", 150.0f);
        dragSensitivityY = PlayerPrefs.GetFloat("SensitivityY", 150.0f);

        EventManager.Instance.AddListener(EEventType.SensitivityChangeX, OnSensitivityChangeX);
        EventManager.Instance.AddListener(EEventType.SensitivityChangeY, OnSensitivityChangeY);

        ApplyCameraSettings();

        // 현재 기획자 편의를 반영하여 Update 단에서 카메라 설정이 적용되고 있으므로, Camera State가 바뀔 때마다 Default 값 변경 필요
        _defaultCameraDistance = _cameraSettings[currentCameraState].cameraDistance;
    }

    // Update에서 매 프레임마다 실행되는 카메라 관련 함수
    public void UpdateFollowCamera()
    {
        HandleGamepadLook();

#if UNITY_STANDALONE
        HandlePCCameraLook();       // PC: 마우스 커서 고정 기반 회전
#else
        HandleMobileCameraDrag();   // 모바일: 터치 스크린 드래그 회전
#endif

        SmoothChangeCamera();
        ZoomCamera();
        HandleRecoil();
        //HandleContinuousAimAssist();

        ActiveCameraAim.m_HorizontalAxis.m_InputAxisName = ""; // 입력 비활성화
        ActiveCameraAim.m_VerticalAxis.m_InputAxisName = "";

        if (_isLock)
        {
            //_cameraAim.m_HorizontalAxis.m_InputAxisName = "";
            //_cameraAim.m_VerticalAxis.m_InputAxisName = "";
            ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0.0f;
            ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0.0f;
        }
        else
        {
            //_cameraAim.m_HorizontalAxis.m_InputAxisName = "Mouse X";
            //_cameraAim.m_VerticalAxis.m_InputAxisName = "Mouse Y";
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
            _inputProvider.enabled = false;  // 입력 비활성화
        }

        if (ActiveCameraAim != null)
        {
            _lockedValue.x = ActiveCameraAim.m_HorizontalAxis.Value;
            _lockedValue.y = ActiveCameraAim.m_VerticalAxis.Value;

            ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
            ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0f;
        }

        _isLockedByUI = true;
    }

    public void OnUIClose()
    {
        if (_inputProvider)
        {
            _inputProvider.enabled = true;  // 입력 비활성화
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
        return; // 일단은 보류

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

    // 외부 호출용: 보정 시작/중지
    public void SetContinuousAimAssist(bool enable)
    {
        _isContinuousAssist = enable;
        if (enable)
        {
            _currentAssistTarget = FindBestTarget(); // 기존 ApplyAimAssist의 타겟 탐색 로직 분리 필요
        }
        else
        {
            _currentAssistTarget = null;
        }
    }

    public void SetTargetPOV(CinemachinePOV targetPOV)
    {
        if (targetPOV != null)
        {
            // 외부 POV가 들어오면 해당 POV를 타겟으로 설정
            ActiveCameraAim = targetPOV;
        }
        else
        {
            if (vcam != null)
            {
                ActiveCameraAim = vcam.GetCinemachineComponent<CinemachinePOV>();
            }
        }
    }

    public void WarpToTarget()
    {
        if (vcam == null || _cameraBody == null || _cameraTarget == null) return;

        // 1. 댐핑 관성으로 인해 카메라 몸체가 굳어버리는 현상을 방지하기 위해 
        //    가상 카메라 컴포넌트를 순간적으로 껐다 켜서 내부 캐시 상태를 강제 리셋합니다.
        vcam.enabled = false;

        // 2. SmoothChangeCamera()에 의해 보간되던 카메라 트랙킹 인자들을 
        //    현재 카메라 상태 데이터(SO)의 원본 값으로 즉시 강제 갱신합니다.
        if (_cameraSettings != null && _cameraSettings.TryGetValue(currentCameraState, out var currentData))
        {
            vcam.m_Lens.FieldOfView = currentData.FOV;
            _cameraBody.m_ScreenX = currentData.screenX;
            _cameraBody.m_ScreenY = currentData.screenY;
            _cameraBody.m_CameraDistance = currentData.cameraDistance;
        }

        // 3. 카메라 오브젝트의 실시간 물리 위치와 회전을 타겟(플레이어 앵커) 위치에 오프셋 없이 즉시 동기화합니다.
        vcam.transform.position = _cameraTarget.position;
        vcam.transform.rotation = _cameraTarget.rotation;

        vcam.enabled = true;

        // 4. 강제로 바뀐 위치 버퍼를 시네머신 파이프라인의 내부 상태 엔진에 즉시 구워 넣습니다.
        //    이 코드가 수행되면 지하 9999m의 잔상이 완전히 세탁됩니다.
        vcam.InternalUpdateCameraState(Vector3.up, Time.deltaTime);
    }

    public void SyncCameraRotation(CinemachinePOV targetPOV, bool isActive)
    {
        if (targetPOV == null) return;

        var sourcePOV = vcam.GetCinemachineComponent<CinemachinePOV>();
        if (sourcePOV == null || targetPOV == null) return;

        if (isActive)
        {
            // [스킬 시전 시]
            // 1. 먼저 target 가상 카메라 오브젝트 자체의 트랜스폼 회전 값을 현재 메인 카메라의 실제 월드 회전 값과 일치시킵니다.
            //    이렇게 하면 부모 오브젝트의 회전 오차를 물리적으로 상쇄할 수 있습니다.
            targetPOV.VirtualCamera.transform.rotation = vcam.transform.rotation;

            // 2. 가상 카메라의 트랜스폼이 정렬된 상태에서, POV 컴포넌트 내부 축 값을 현재 바라보는 방향에 맞춰 강제로 갱신합니다.
            //    이 처리를 해주어야 다음 프레임에 POV 마우스 조작 입력이 튀지 않고 부드럽게 이어집니다.
            targetPOV.m_HorizontalAxis.Value = vcam.transform.eulerAngles.y - targetPOV.VirtualCamera.transform.parent.eulerAngles.y;
            targetPOV.m_VerticalAxis.Value = sourcePOV.m_VerticalAxis.Value;
        }
        else
        {
            sourcePOV.m_HorizontalAxis.Value = targetPOV.m_HorizontalAxis.Value;
            sourcePOV.m_VerticalAxis.Value = targetPOV.m_VerticalAxis.Value;
        }
    }
    #endregion

    #region Private Methods
    private void ApplyCameraSettings()
    {
        ActiveCameraAim.m_HorizontalAxis.m_MaxValue = _cameraSettings[currentCameraState].maxAimRangeX;
        ActiveCameraAim.m_HorizontalAxis.m_MinValue = _cameraSettings[currentCameraState].minAimRangeX;
        ActiveCameraAim.m_VerticalAxis.m_MaxValue = _cameraSettings[currentCameraState].maxAimRangeY;
        ActiveCameraAim.m_VerticalAxis.m_MinValue = _cameraSettings[currentCameraState].minAimRangeY;
        ActiveCameraAim.m_HorizontalAxis.m_MaxSpeed = _cameraSettings[currentCameraState].sensitivityX;
        ActiveCameraAim.m_VerticalAxis.m_MaxSpeed = _cameraSettings[currentCameraState].sensitivityY;
        ActiveCameraAim.m_HorizontalAxis.m_AccelTime = _cameraSettings[currentCameraState].accelTimeX;
        ActiveCameraAim.m_HorizontalAxis.m_DecelTime = _cameraSettings[currentCameraState].decelTimeX;
        ActiveCameraAim.m_VerticalAxis.m_AccelTime = _cameraSettings[currentCameraState].accelTimeY;
        ActiveCameraAim.m_VerticalAxis.m_DecelTime = _cameraSettings[currentCameraState].decelTimeY;

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
            ActiveCameraAim.m_HorizontalAxis.Value += _currentRecoilX * Time.deltaTime;
            ActiveCameraAim.m_VerticalAxis.Value -= _currentRecoilY * Time.deltaTime;

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
        float startX = ActiveCameraAim.m_HorizontalAxis.Value;
        float targetX = startX + 180.0f; // 180도 뒤로 목표 설정

        while (elapsedTime < quickTurnDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / quickTurnDuration;

            // SmoothStep을 사용하여 부드러운 가속/감속 효과 적용
            float smoothT = Mathf.SmoothStep(0, 1, t);
            ActiveCameraAim.m_HorizontalAxis.Value = Mathf.Lerp(startX, targetX, smoothT);

            yield return null;
        }

        // 최종 각도 보정 및 상태 복구
        ActiveCameraAim.m_HorizontalAxis.Value = targetX;
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

    private bool IsPointerOverUI(int fingerId, Vector2 position)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            pointerId = fingerId,
            position = position
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            // 터치한 UI 중 가장 최상단에 있는 UI 요소를 검사합니다.
            GameObject clickedUI = results[0].gameObject;

            // 허용된 UI 리스트에 포함된 버튼 이름(혹은 부모 오브젝트 이름)이 있는지 확인합니다.
            if (cameraPermittedUIList != null)
            {
                foreach (string uiName in cameraPermittedUIList)
                {
                    // 정확한 이름 매칭 혹은 사격 버튼 자식 프리팹 구조 대응을 위해 Contains 활용
                    if (clickedUI.name.Contains(uiName))
                    {
                        // UI 위에 있지만, 사격 버튼이므로 카메라 조작을 할 수 있도록 'UI 아님' 처리(false)를 반환합니다.
                        return false;
                    }
                }
            }
            return true; // 사격 버튼 외의 일반 UI(이동 패드, 옵션창 등)는 카메라 조작을 차단합니다.
        }

        return false;
    }

    private void HandleGamepadLook()
    {
        // UI 오픈 상태이거나 카메라 잠금 상태면 입력 무시
        if (_isLockedByUI || _isLock || _isQuickTurning) return;

        var gamepad = Gamepad.current;
        if (gamepad == null)
        {
            // 패드가 연결 해제되었을 때를 대비해 입력값 초기화
            ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0.0f;
            ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0.0f;
            return;
        }

        // 오른쪽 스틱(Right Stick) 입력 받기
        Vector2 stickInput = gamepad.rightStick.ReadValue();

        // 입력이 데드존(Deadzone) 이상일 때만 처리
        if (stickInput.magnitude > 0.05f)
        {
            // 시네머신 POV 축에 직접 입력값 전달
            float padSensitivityX = LegacySensitivityX * 0.1f;
            float padSensitivityY = LegacySensitivityY * 0.1f;

            ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = stickInput.x * padSensitivityX;
            ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = stickInput.y * padSensitivityY;
        }
        else
        {
            // 스틱을 놓았을 때(데드존 이내) 반드시 0으로 초기화하여 회전을 멈춤
            ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0.0f;
            ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0.0f;
        }
    }

    // PC 환경: 마우스 커서 고정 상태에서의 카메라 회전 처리
    private void HandlePCCameraLook()
    {
        // UI가 열려있거나, 퀵턴 중이거나, 카메라 조작이 잠긴 상태라면 회전 처리 생략
        if (_isQuickTurning || _isLockedByUI || _isLock || _quickTurnCoroutine != null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // 마우스의 순수 프레임별 이동량(Delta)을 가져옴 (Input System 기준)
        Vector2 mouseDelta = mouse.delta.ReadValue();

        // 환산된 감도에 곱해 실제 회전량을 만드는 계수. (기존 값 유지)
        const float sensitivityMultiplier = 0.005f;

        float finalSensitivityX = LegacySensitivityX * sensitivityMultiplier;
        float finalSensitivityY = LegacySensitivityY * sensitivityMultiplier;

        // 시네머신 POV 축 값(Value)에 델타 변위값을 즉시 더해 굼뜸 현상 해결
        // 마우스 Delta Y는 위로 올릴 때 양수(+)이므로, 카메라를 위로 올리려면 축 값에서 빼주거나 더해주는 등 프로젝트 기준에 맞춰 조정
        ActiveCameraAim.m_HorizontalAxis.Value += mouseDelta.x * finalSensitivityX;
        ActiveCameraAim.m_VerticalAxis.Value -= mouseDelta.y * finalSensitivityY;

        // InputAxisValue는 안전하게 0으로 리셋
        ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
        ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0f;
    }

    private void HandleMobileCameraDrag()
    {
        if (_isQuickTurning || _isLockedByUI || _isLock || _quickTurnCoroutine != null) return;
        if (EventSystem.current == null) return;

        // 기기별 하드웨어 DPI를 반영하여 고해상도 폰에서도 일정한 물리 감도를 유지하도록 헬퍼 계수 계산
        // DPI 정보가 없는 환경(PC 등)을 위해 기본값 96f로 방어 처리
        float deviceDPI = Screen.dpi > 0 ? Screen.dpi : 96f;

        // 시네머신 POV 축의 MaxSpeed가 sensitivity(기본 150 등)로 잡혀있으므로, 
        // 픽셀 델타 변위와 결합할 최종 보정 계수 (이 값을 조절하여 기본 베이스 감도를 튜닝할 수 있습니다)
        float sensitivityMultiplier = 2f;

#if UNITY_STANDALONE
        // PC 환경: 마우스 드래그 처리
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (_dragFingerId == 99)
        {
            if (mouse.leftButton.isPressed)
            {
                Vector2 currentMousePos = mouse.position.ReadValue();

                // 해상도로 나누지 않고, 순수 픽셀 이동량(Delta)을 구한 뒤 DPI 비율로 정규화
                float deltaX = (currentMousePos.x - _lastMousePosition.x) / deviceDPI;
                float deltaY = (currentMousePos.y - _lastMousePosition.y) / deviceDPI;

                // PC와 모바일의 연산 공식을 완전히 일치시킴
                ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = deltaX * LegacySensitivityX * sensitivityMultiplier;
                ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = deltaY * LegacySensitivityY * sensitivityMultiplier;

                _lastMousePosition = currentMousePos;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _dragFingerId = -1;
                ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
                ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0f;
            }
        }
        else if (_dragFingerId == -1)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 currentMousePos = mouse.position.ReadValue();
                if (IsPointerOverUI(-1, currentMousePos)) return;

                _dragFingerId = 99;
                _lastMousePosition = currentMousePos;
            }
        }

#else
        // 모바일 환경: 터치스크린 하드웨어 처리
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        var allTouches = touchscreen.touches;

        if (_dragFingerId != -1)
        {
            bool fingerFound = false;
            foreach (var touch in allTouches)
            {
                if (!touch.isInProgress) continue;
                int fingerId = touch.touchId.ReadValue();

                if (fingerId == _dragFingerId)
                {
                    fingerFound = true;

                    // 수동 위치 차이 계산을 버리고, 하드웨어에서 보장하는 순수 터치 델타(이동량) 값을 사용합니다.
                    // touch.delta는 프레임 유실 없이 터치 하드웨어의 모든 이동 픽셀을 정밀하게 반환합니다.
                    Vector2 touchDelta = touch.delta.ReadValue();

                    // 디바이스 DPI로 나누어 해상도별 편차를 방지합니다. 
                    // touch.delta 자체가 이미 픽셀 단위이므로 DPI 분할 후 감도 멀티플라이어를 곱해줍니다.
                    float deltaX = touchDelta.x / deviceDPI;
                    float deltaY = touchDelta.y / deviceDPI;

                    // 기획 변경 사항인 감도 옵션 수치들을 반영합니다.
                    // 시네머신 인풋 축에 속도로 주입하면 무겁게 밀리므로, 축 자체의 실제 값(Value)에 즉시 더해 줍니다.
                    float finalSensitivityX = LegacySensitivityX * sensitivityMultiplier;
                    float finalSensitivityY = LegacySensitivityY * sensitivityMultiplier;

                    // 가속도가 아닌 값(Value)에 직접 더해줌으로써 굼뜸 현상을 해결하고 감도 옵션 체감을 확실하게 만듭니다.
                    ActiveCameraAim.m_HorizontalAxis.Value += deltaX * finalSensitivityX;
                    ActiveCameraAim.m_VerticalAxis.Value -= deltaY * finalSensitivityY;

                    // 시네머신 값에 직접 더해주므로 InputAxisValue는 안전하게 0으로 리셋해 둡니다.
                    ActiveCameraAim.m_HorizontalAxis.m_InputAxisValue = 0f;
                    ActiveCameraAim.m_VerticalAxis.m_InputAxisValue = 0f;

                    if (touch.press.wasReleasedThisFrame)
                    {
                        _dragFingerId = -1;
                    }
                    break;
                }
            }

            if (!fingerFound) _dragFingerId = -1;
        }

        if (_dragFingerId == -1)
        {
            foreach (var touch in allTouches)
            {
                if (!touch.isInProgress) continue;
                if (!touch.press.wasPressedThisFrame) continue;

                int fingerId = touch.touchId.ReadValue();
                Vector2 currentTouchPos = touch.position.ReadValue();

                if (IsPointerOverUI(fingerId, currentTouchPos)) continue;

                _dragFingerId = fingerId;
                break;
            }
        }
#endif
    }

    // 매 프레임 업데이트되는 보정 로직 (UpdateFollowCamera에서 호출)
    private void HandleContinuousAimAssist()
    {
        if (!_isContinuousAssist || _currentAssistTarget == null) return;

        // 화면 중앙 좌표 계산
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentAssistTarget.position);

        // 타겟이 화면 밖으로 나가거나 카메라 뒤로 가면 해제
        float dist = Vector2.Distance(screenCenter, new Vector2(screenPos.x, screenPos.y));
        if (screenPos.z < 0 || dist > assistRadius * 1.5f) // 약간의 여유 거리를 둠
        {
            _currentAssistTarget = null;
            return;
        }

        // 보정 로직 (코루틴의 내용을 프레임 단위로 실행)
        Vector3 dirToTarget = (_currentAssistTarget.position - Camera.main.transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget);

        float targetX = targetRot.eulerAngles.y;
        float targetY = targetRot.eulerAngles.x;
        if (targetY > 180) targetY -= 360.0f;

        float deltaX = Mathf.DeltaAngle(ActiveCameraAim.m_HorizontalAxis.Value, targetX);
        float deltaY = Mathf.DeltaAngle(ActiveCameraAim.m_VerticalAxis.Value, targetY);

        // Time.deltaTime을 곱해 매 프레임 부드럽게 추적
        ActiveCameraAim.m_HorizontalAxis.Value += deltaX * assistStrength * Time.deltaTime * 10f;
        ActiveCameraAim.m_VerticalAxis.Value += deltaY * assistStrength * Time.deltaTime * 10f;
    }

    // 기존 ApplyAimAssist의 탐색 로직만 따로 뺀 헬퍼 함수
    private Transform FindBestTarget()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Collider[] targets = Physics.OverlapSphere(_cameraTarget.position, 30f, targetLayer);
        Transform best = null;
        float closestDist = assistRadius;

        foreach (var t in targets)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(t.bounds.center);
            if (screenPos.z < 0) continue;
            float dist = Vector2.Distance(screenCenter, new Vector2(screenPos.x, screenPos.y));
            if (dist < closestDist) { closestDist = dist; best = t.transform; }
        }
        return best;
    }

    private void OnSensitivityChangeX(EEventType eventType, Component sender, object param = null)
    {
        dragSensitivityX = PlayerPrefs.GetFloat("SensitivityX", 150.0f);
    }

    private void OnSensitivityChangeY(EEventType eventType, Component sender, object param = null)
    {
        dragSensitivityY = PlayerPrefs.GetFloat("SensitivityY", 150.0f);
    }

    private IEnumerator AimAssistCoroutine(Transform target)
    {
        float elapsed = 0.0f;
        float duration = 0.1f; // 보정 시간

        float startX = ActiveCameraAim.m_HorizontalAxis.Value;
        float startY = ActiveCameraAim.m_VerticalAxis.Value;

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

            float deltaX = Mathf.DeltaAngle(ActiveCameraAim.m_HorizontalAxis.Value, targetX);
            float deltaY = Mathf.DeltaAngle(ActiveCameraAim.m_VerticalAxis.Value, targetY);

            ActiveCameraAim.m_HorizontalAxis.Value += deltaX * assistStrength * smoothT;
            ActiveCameraAim.m_VerticalAxis.Value += deltaY * assistStrength * smoothT;

            yield return null;
        }
    }
    #endregion
}
