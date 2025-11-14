using Cinemachine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

// 기획자 작업 편의를 위해, 씬에서 SO 값을 변경하여 바로 적용할 수 있도록 ExecuteInEditMode 속성 추가
[ExecuteInEditMode]
public class FollowCameraController : MonoBehaviour
{
    #region Variables
    [Header("Camera Settings")]
    private CinemachineVirtualCamera vcam;
    private CinemachineFramingTransposer _cameraBody;
    private CinemachinePOV _cameraAim;

    [SerializeField] private ECameraState currentCameraState = ECameraState.Normal;
    private Dictionary<ECameraState, FollowCameraData> _cameraSettings = new Dictionary<ECameraState, FollowCameraData>();
    private GameObject _owner;
    private Transform _cameraTarget;
    private bool _isBeforeZoom = false;
    private bool _isLock = false;
    private float _scrollY = 0.0f;
    private float _defaultCameraDistance = 2.0f;

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
    }

#if UNITY_EDITOR
    private void Update()
    {
        foreach (ECameraState state in Enum.GetValues(typeof(ECameraState)))
        {
            _cameraSettings[state] = Resources.Load<FollowCameraData>($"Camera/FollowCameraData_{state}");
        }

        _cameraBody = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        _cameraAim = vcam.GetCinemachineComponent<CinemachinePOV>();

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
    #endregion
}
