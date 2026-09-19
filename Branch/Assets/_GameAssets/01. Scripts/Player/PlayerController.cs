using Cinemachine;
using FIMSpace.FProceduralAnimation;
using Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

[Serializable]
public struct BaseAnimation
{
    public AnimatorOverrideController overrideController;
    public bool isOnlyLoop;
}

public class PlayerController : MonoBehaviour, PlayerActions.IPlayerActionMapActions, IDamagable
{
    #region Variables
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private LegsAnimator legsAnimator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Scripts")]
    [SerializeField] private CharacterStat stats;
    [SerializeField] private Inventory inventory;
    [SerializeField] private RigAimController rigAimController;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private GameObject followCameraPrefab;
    [SerializeField] private CinemachineVirtualCamera startCam;
    [Tooltip("시작 카메라 연출이 끝난 뒤 키 가이드(F1 화면)를 자동으로 띄운다.")]
    [SerializeField] private bool showKeyGuideOnStart = true;
    [SerializeField] private Volume volume;
    [SerializeField] private ParticleFollower navi;
    private FollowCameraController _followCamera;
    private MotionBlur _motionBlur;
    private MaterialPropertyController lowHpController;

    [Header("State")]
    [SerializeField] private EPlayerState movementBlockMask = EPlayerState.Dashing;
    [SerializeField] private EPlayerState dashBlockMask;
    [SerializeField] private EPlayerState skillBlockMask;
    [SerializeField] private EPlayerState shootBlockMask = EPlayerState.Dashing;
    [SerializeField] private EPlayerState zoomBlockMask = EPlayerState.Dashing;
    [SerializeField] private EPlayerState partChangeBlockMask;
    [SerializeField] private EPlayerState quickTurnBlockMask;
    private EPlayerState _currentPlayerState = EPlayerState.Idle;
    private int _invincibilityRefCount = 0;
    private EPlayerState _previousState = 0;
    private bool _isLeftAttackReady = false;
    private bool _isRightAttackReady = false;
    private bool _isLowHp = false;

    [Header("Movement")]
    [SerializeField, Range(0.0f, 100.0f)] private float jumpVelocity = 50.0f;
    [SerializeField, Range(0.01f, 100.0f)] private float rotationSpeed = 40.0f;
    private PlayerActions _playerActions;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;
    private Vector3 _totalDirection = Vector3.zero;
    private bool _canMove = true;
    private bool _canRotatable = true;
    private ILegsMovement _currentMovement;
    private Vector2 _postMoveInput = Vector2.zero;
    private Vector2 _currentMoveInput = Vector2.zero;

    [Header("Gravity")]
    [SerializeField] private Vector3 boxSize = new Vector3(0.2f, 0.1f, 0.2f);
    [SerializeField] private float gravityScale = 2.0f;
    [SerializeField] private LayerMask groundLayerMask;
    private bool _isGrounded = false;
    private Vector3 _fallVelocity;
    private float _groundCheckBufferTime = 0.1f;  // 0.1초까지 낙하 감지 지연
    private float _groundCheckTimer = 0.0f;
    private bool _isOnPlatform = false;
    private bool _isPlatformEnd = false;
    private Transform _postPlatform = null;
    private Vector3 _lastPlatformPosition = Vector3.zero;
    private Vector3 _platformVelocity = Vector3.zero;

    [Header("Dash")]
    private Vector3 _dashDirection = Vector3.zero;
    private float _dashSpeed = 0.0f;

    [Header("Animation")]
    [SerializeField] private List<BaseAnimation> animations = new();
    private EAnimationType _currentAnimType = EAnimationType.Base;
    private EAnimationType _shootAnimType = EAnimationType.ShootingBase;

    // 사격 중 이동 애니메이션 전환. 컨트롤러를 교체하면 모든 레이어가 초기화되어 이동 모션이 처음부터 다시 재생되므로,
    // BT_Move 블렌드 트리 안에서 aimWeight 파라미터로 일반 이동과 사격 중 이동을 섞는다.
    [SerializeField] private float aimMoveBlendTime = 0.15f;
    private static readonly int AimWeightHash = Animator.StringToHash("aimWeight");
    private bool _isAimMove = false;
    private float _aimMoveWeight = 0.0f;

    // 사격 애니메이션은 한 팔 기준이라, 팔을 뻗으면서 허리도 그쪽으로 돌린다.
    // 팔 레이어(LeftArmLayer/RightArmLayer)는 팔만 담당하고, 허리는 팔 레이어와 Sync된 허리 레이어가 따로 담당한다.
    // 한 팔 사격에서는 그쪽 허리 레이어를 켜 허리 움직임을 살리고,
    // 양팔 사격에서는 허리를 펴야 두 팔이 함께 앞을 향할 수 있으므로 허리 레이어를 낮춘다.
    [Tooltip("한 팔 사격 시 허리 레이어 가중치. 낮추면 사격할 때 허리가 덜 돌아간다.")]
    [SerializeField, Range(0.0f, 1.0f)] private float singleShootTorsoWeight = 1.0f;
    [Tooltip("양팔 사격 시 허리 레이어 가중치. 0이면 허리는 이동 애니메이션을 그대로 따른다.")]
    [SerializeField, Range(0.0f, 1.0f)] private float dualShootTorsoWeight = 0.0f;
    [Tooltip("허리 레이어 가중치가 바뀌는 시간(초). 사격을 누르고 뗄 때 허리가 튀지 않게 한다.")]
    [SerializeField] private float shootTorsoBlendTime = 0.2f;
    private const string LeftTorsoLayerName = "LeftTorsoLayer";
    private const string RightTorsoLayerName = "RightTorsoLayer";

    [Header("Parts")]
    [SerializeField] private List<SkinnedMeshRenderer> bodyRenderers = new();
    [SerializeField] private List<Material> basicMaterials = new();
    [SerializeField] private List<Material> laserMaterials = new();
    [SerializeField] private List<Material> rapidMaterials = new();
    [SerializeField] private List<Material> heavyMaterials = new();
    private Dictionary<EPartType, float> cooldownDict = new();

    [Header("Sounds")]
    [SerializeField] private List<AudioClip> hitClips = new();
    [SerializeField] private AudioClip deadClip;
    [SerializeField] private AudioSource seSource;

    [Header("Revive")]
    // 부활 순간 플레이어 위치에 동시에 생성할 이펙트들 (예: 전기 + 충격파)
    [SerializeField] private List<GameObject> reviveEffectPrefabs = new();
    [SerializeField] private float reviveEffectLifetime = 3.0f;
    // 복구 완료 알림음. 부활 순간에는 이펙트 소리에 묻히므로 System Restored 문구가 뜰 때 먼저 재생한다.
    [FormerlySerializedAs("reviveClip")]
    [SerializeField] private AudioClip restoredClip;
    // 음원 앞부분 무음 구간을 건너뛸 시간(초). 문구가 켜지는 순간 소리가 바로 들리도록 맞춘다.
    [SerializeField, Min(0.0f)] private float restoredClipStartTime = 0.0f;

    private Coroutine _indicatorRoutine = null;
    private Coroutine _hitRoutine = null;
    
    // isInit
    private bool _isInit;
    #endregion

    #region Properties
    public Animator PlayerAnimator
    {
        get => animator;
    }

    public CharacterStat Stats
    {
        get => stats;
    }

    public FollowCameraController FollowCamera
    {
        get => _followCamera;
    }

    public CharacterController Controller
    {
        get => characterController;
        set => characterController = value;
    }

    public ParticleFollower Navi
    {
        get => navi;
    }

    public EPlayerState CurrentPlayerState
    {
        get { return _currentPlayerState; }
    }

    public Vector3 FallVelocity
    {
        get { return _fallVelocity; }
    }

    public Vector3 DashDirection
    {
        get { return _dashDirection; }
        set { _dashDirection = value; }
    }

    public float DashSpeed
    {
        get { return _dashSpeed; }
        set { _dashSpeed = value; }
    }

    public bool IsGrounded
    {
        get { return _isGrounded; }
    }

    public Inventory Inven
    {
        get => inventory;
    }

    public Dictionary<EPartType, float> CooldownDictionary
    {
        get => cooldownDict;
        set => cooldownDict = value;
    }
    #endregion

    #region Unity Methods

    private void Update()
    {
        if (!_isInit) return;
        
        AnimCheckShoot();
        UpdateAimMoveWeight();
        UpdateShootTorsoBlend();
        GUIManager.Instance.GameUIController.SetHpSlider(stats.CurrentHealth, stats.MaxHealth);
    }

    private void LateUpdate()
    {
        if (!_isInit) return;
        
        // To-do: 애니메이션 이벤트로 변경할 것
        CheckSpawnAnimationEnd();

        HandleMove();
        DashMove();
        HandleGravity();

        _followCamera.UpdateFollowCamera();
        RotateCharacter();

        if (_currentMovement is LegsCaterpillar legsCaterpillar)
        {
            legsCaterpillar.LateUpdateCaterpillarRotation(transform);
        }

        characterController.Move(_totalDirection * Time.deltaTime);
        _totalDirection = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlatformIn"))
        {
            Debug.Log("와! 플랫폼!");
            var comp = other.GetComponent<PlatformCheck>();
            if (comp != null)
            {
                _isOnPlatform = true;
                _postPlatform = comp.Platform;
                _lastPlatformPosition = _postPlatform.position;

                legsAnimator.enabled = false;
            }
        }
    }

    private void OnDisable()
    {
        if (_hitRoutine != null)
        {
            StopCoroutine(_hitRoutine);
            _hitRoutine = null;
        }

        if (_isLowHp && lowHpController != null)
        {
            lowHpController.SetEffectActive(false);
        }

        _playerActions.PlayerActionMap.Disable();
        
        _isInit = false;
    }
    #endregion

    #region Input Actions
    /// <summary>
    /// 일시정지, 키 가이드, 맵처럼 시간이 멈춘 동안에는 전투 조작이 들어가면 안 된다.
    /// UI를 닫는 입력(Esc, F1 등)은 계속 받아야 하므로 액션맵 자체를 끄지 않고 여기서만 걸러낸다.
    /// 파츠 교체 메뉴는 timeScale을 0.1로 쓰므로 이 조건에 걸리지 않는다.
    /// </summary>
    private bool IsTimeStopped => Time.timeScale <= 0.0f;

    void PlayerActions.IPlayerActionMapActions.OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();

        if (context.canceled)
        {
            // 키를 뗐을 때 반드시 0으로
            _postMoveInput = Vector2.zero;
        }
        else if (_moveInput != null && _moveInput != Vector2.zero)
        {
            // 키 입력이 있을 때만 갱신
            _postMoveInput = _moveInput;
        }
    }

    // 이름은 Dash이나 실제로는 Legs 파츠 스킬을 사용
    void PlayerActions.IPlayerActionMapActions.OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsTimeStopped) return;
            if ((_currentPlayerState & dashBlockMask) != 0) return;

            if (_moveInput == null || _moveInput == Vector2.zero)
            {
                Vector3 camForward = _followCamera.transform.forward;
                camForward.y = 0.0f;
                _dashDirection = camForward.normalized;
            }
            else
            {
                _dashDirection = CalculateInputDirection();
            }

            inventory.EquippedItems[EPartType.Legs][0].UseAbility();
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnShoulderSkill(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsTimeStopped) return;
            if ((_currentPlayerState & skillBlockMask) != 0) return;

            inventory.EquippedItems[EPartType.Shoulder][0].UseAbility();
        }
    }


    void PlayerActions.IPlayerActionMapActions.OnLeftAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsTimeStopped) return;
            if ((_currentPlayerState & EPlayerState.UnmanipulableState) != 0) return;

            PartBaseArm weapon = inventory.EquippedItems[EPartType.ArmL][0].GetComponent<PartBaseArm>();
            if (weapon && (weapon.IsOverheat || !weapon.IsAnimating)) return;

            _currentPlayerState |= EPlayerState.LeftShooting;
            Shoot(true);
        }

        if (context.canceled)
        {
            CancleAttack(true);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnRightAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsTimeStopped) return;
            if ((_currentPlayerState & EPlayerState.UnmanipulableState) != 0) return;

            PartBaseArm weapon = inventory.EquippedItems[EPartType.ArmR][0].GetComponent<PartBaseArm>();
            if (weapon && (weapon.IsOverheat || !weapon.IsAnimating)) return;

            _currentPlayerState |= EPlayerState.RightShooting;
            Shoot(false);
        }

        if (context.canceled)
        {
            CancleAttack(false);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnBothAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsTimeStopped) return;
            if ((_currentPlayerState & EPlayerState.UnmanipulableState) != 0) return;

            PartBaseArm left = inventory.EquippedItems[EPartType.ArmL][0].GetComponent<PartBaseArm>();
            if (!left.IsOverheat && left.IsAnimating)
            {
                _currentPlayerState |= EPlayerState.LeftShooting;
                Shoot(true);
            }

            PartBaseArm right = inventory.EquippedItems[EPartType.ArmR][0].GetComponent<PartBaseArm>();
            if (!right.IsOverheat && right.IsAnimating)
            {
                _currentPlayerState |= EPlayerState.RightShooting;
                Shoot(false);
            }
        }

        if (context.canceled)
        {
            CancleAttack(true);
            CancleAttack(false);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnInteraction(InputAction.CallbackContext context)
    {
        if ((_currentPlayerState & EPlayerState.UnmanipulableState) != 0) return;

        if (context.started)
        {
            if (IsTimeStopped) return;

            EventManager.Instance.PostNotification(EEventType.Interaction, this, null);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnRadialMenu(InputAction.CallbackContext context)
    {
        if (Managers.GUIManager.Instance.GameUIController.HelpUI.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.WorldMap.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.PauseUI.activeSelf) return;

#if UNITY_STANDALONE
        // PC 버전 기준, 추후 빌드 플랫폼에 따라 다르게 적용되도록 수정 필요
        if (context.started)
        {
            if ((_currentPlayerState & partChangeBlockMask) != 0) return;

            _currentPlayerState |= EPlayerState.Toggle;

            // UI 활성화 시 커서 보이기, 자유롭게
            Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(true);
            Managers.GUIManager.Instance.GameUIController.ActivateRedDot(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // To-do: 공격 등 다른 조작도 불가능하도록 설정
            SetMovable(false);
            _followCamera.SetCameraRotatable(false);

            Time.timeScale = 0.1f;
        }

        if (context.canceled)
        {
            if (!Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;
            CloseRadialUI();
        }
#else
// 모바일 버전 기준, 버튼을 누를 때마다 On/Off
        if (context.started)
        {
            // Radial UI가 꺼져있을 경우
            if (!Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf)
            {
                // UI 활성화 시 커서 보이기, 자유롭게
                Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(true);
                Managers.GUIManager.Instance.GameUIController.ActivateRedDot(false);
                //Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;

                // To-do: 공격 등 다른 조작도 불가능하도록 설정
                // 컨트롤러를 바꿔버리는 것도 방법인 듯
                SetMovable(false);
                _followCamera.SetCameraRotatable(false);

                Time.timeScale = 0.1f;
            }
            else
            {
                // UI가 켜져있을 경우
                CloseRadialUI();
            }
        }
#endif
    }

    void PlayerActions.IPlayerActionMapActions.OnBaseSet(InputAction.CallbackContext context)
    {
        if (!Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;

        if (context.started)
        {
            for (int i = 0; i < 4; ++i)
            {
                SelectAndChangePart(i, 0);
            }
            Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(false);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnLaserSet(InputAction.CallbackContext context)
    {
        if (!Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;
        if (!Managers.GUIManager.Instance.GameUIController.UnlockSets[0]) return;

        if (context.started)
        {
            for (int i = 0; i < 4; ++i)
            {
                SelectAndChangePart(i, 1);
            }
            Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(false);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnRapidSet(InputAction.CallbackContext context)
    {
        if (!Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;
        if (!Managers.GUIManager.Instance.GameUIController.UnlockSets[1]) return;

        if (context.started)
        {
            for (int i = 0; i < 4; ++i)
            {
                SelectAndChangePart(i, 2);
            }
            Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(false);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnHeavySet(InputAction.CallbackContext context)
    {
        if (!Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;
        if (!Managers.GUIManager.Instance.GameUIController.UnlockSets[2]) return;

        if (context.started)
        {
            for (int i = 0; i < 4; ++i)
            {
                SelectAndChangePart(i, 3);
            }
            Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(false);
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnIndicator(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Managers.GUIManager.Instance.GameUIController.IndicatorUI.gameObject.activeSelf)
            {
                Managers.GUIManager.Instance.GameUIController.SetIndicator(false);
            }
            else
            {
                Managers.GUIManager.Instance.GameUIController.SetIndicator(true);
            }

            //if (_indicatorRoutine != null)
            //{
            //    StopCoroutine(_indicatorRoutine);
            //    _indicatorRoutine = null;
            //}
            //_indicatorRoutine = StartCoroutine(CoStartIndicatorTimer(5.0f));
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnHelp(InputAction.CallbackContext context)
    {
        if (Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.WorldMap.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.Tutorial.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.Option.activeSelf) return;

        if (context.started)
        {
            if (Managers.GUIManager.Instance.GameUIController.PauseUI.activeSelf && !Managers.GUIManager.Instance.GameUIController.HelpUI.activeSelf) return;

            if (!Managers.GUIManager.Instance.GameUIController.HelpUI.activeSelf)
            {
                _followCamera.OnUIOpen();

                Managers.GUIManager.Instance.GameUIController.HelpUI.SetActive(true);
                Managers.GUIManager.Instance.GameUIController.HUD.SetActive(false);
                Time.timeScale = 0.0f;
            }
            else
            {
                Managers.GUIManager.Instance.GameUIController.HelpUI.SetActive(false);

                if (!Managers.GUIManager.Instance.GameUIController.PauseUI.activeSelf)
                {
                    _followCamera.OnUIClose();

                    Managers.GUIManager.Instance.GameUIController.RestoreHUD();
                    Time.timeScale = 1.0f;
                }
            }
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnMap(InputAction.CallbackContext context)
    {
        if (Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.HelpUI.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.Tutorial.activeSelf) return;
        if (Managers.GUIManager.Instance.GameUIController.Option.activeSelf) return;

        if (context.started)
        {
            if (Managers.GUIManager.Instance.GameUIController.PauseUI.activeSelf && !Managers.GUIManager.Instance.GameUIController.WorldMap.activeSelf) return;

            if (!Managers.GUIManager.Instance.GameUIController.WorldMap.activeSelf)
            {
                _followCamera.OnUIOpen();

                Managers.GUIManager.Instance.GameUIController.WorldMap.SetActive(true);
                Managers.GUIManager.Instance.GameUIController.HUD.SetActive(false);
                Time.timeScale = 0.0f;
            }
            else
            {
                Managers.GUIManager.Instance.GameUIController.WorldMap.SetActive(false);

                if (!Managers.GUIManager.Instance.GameUIController.PauseUI.activeSelf)
                {
                    _followCamera.OnUIClose();

                    Managers.GUIManager.Instance.GameUIController.RestoreHUD();
                    Time.timeScale = 1.0f;
                }
            }
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnPause(InputAction.CallbackContext context)
    {
        if (Managers.GUIManager.Instance.GameUIController.RadialUI.activeSelf) return;

        if (context.started)
        {
            // 모든 UI 전환(활성화/비활성화) 및 Time.timeScale 연산을 
            // 온스크린 버튼의 가상 입력 처리가 완료된 후 안전하게 처리하기 위해 코루틴으로 일괄 위임합니다.
            StartCoroutine(CoProcessPauseInput());
        }
    }

    void PlayerActions.IPlayerActionMapActions.OnLook(InputAction.CallbackContext context)
    {
        
    }

    void PlayerActions.IPlayerActionMapActions.OnQuickTurn(InputAction.CallbackContext context)
    {
        //if (context.started)
        //{
        //    // 특정 상태일 때 뒤돌기 불가능
        //    if ((_currentPlayerState & quickTurnBlockMask) != 0) return;

        //    SetPlayerState(EPlayerState.QuickTurning, true);
        //    _followCamera.StartQuickTurn();
        //}
    }

    // 우클릭을 통해 사격을 위한 줌을 준비하는 기능
    // 양팔 파츠로 변경되면서 줌 기능이 삭제되어 현재는 사용 X
    void PlayerActions.IPlayerActionMapActions.OnZoom(InputAction.CallbackContext context)
    {
        //if ((_currentPlayerState & zoomBlockMask) != 0) return;

        //if (context.started)
        //{
        //    _currentPlayerState &= ~EPlayerState.LeftShooting;
        //    _currentPlayerState &= ~EPlayerState.RightShooting;
        //    _currentPlayerState |= EPlayerState.Zooming;
        //    _followCamera.IsBeforeZoom = true;
        //    _followCamera.IsZoomed = true;
        //    animator.SetBool("isAim", true);
        //}

        //if (context.canceled)
        //{
        //    _currentPlayerState &= ~EPlayerState.LeftShooting;
        //    _currentPlayerState &= ~EPlayerState.RightShooting;
        //    _currentPlayerState &= ~EPlayerState.Zooming;
        //    _followCamera.IsBeforeZoom = false;
        //    _followCamera.IsZoomed = false;
        //    animator.SetBool("isAim", false);
        //}
    }

    // 디버그용 점프 함수
    // 정식 빌드에서는 삭제될 예정
    void PlayerActions.IPlayerActionMapActions.OnJump(InputAction.CallbackContext context)
    {
        //if ((_currentPlayerState & EPlayerState.UnmanipulableState) != 0) return;

        //if (context.started)
        //{
        //    // 바닥에 있을 때만 점프 실행
        //    if (_isGrounded || _isOnPlatform)
        //    {
        //        _fallVelocity.y = jumpVelocity;

        //        // 점프 상태 갱신
        //        _currentPlayerState |= EPlayerState.Falling;
        //    }
        //}
    }

    // 마우스 휠을 통한 줌 기능 (임시)
    // 파츠 별 카메라를 고려하지 않은 상태이므로 사용한다면 추후 수정 필요
    void PlayerActions.IPlayerActionMapActions.OnMouseScroll(InputAction.CallbackContext context)
    {
        if ((_currentPlayerState & EPlayerState.UnmanipulableState) != 0) return;

        if (context.performed)
        {
            // 120 또는 -120 (-0.008을 곱하면 1과 근사한 0.96 또는 -0.96)
            float scrollValue = context.ReadValue<float>();
            //_followCamera.ScrollY = scrollValue * -0.008f;
        }
    }

    // 마우스 휠 줌 상태를 초기화하는 기능 (임시)
    // 추후 적용한다면 입력 뿐만이 아니라 캐릭터가 대기 상태가 아닐 때에도 적용되도록 변경할 것
    void PlayerActions.IPlayerActionMapActions.OnResetCamera(InputAction.CallbackContext context)
    {
        //if (context.started)
        //{
        //    _followCamera.ResetCamera();
        //}
    }
#endregion

    #region Public Methods
    public void Spawn()
    {
        _isLowHp = false;
        _invincibilityRefCount = 0;
        if (lowHpController != null)
        {
            lowHpController.SetEffectActive(false);
        }

        // Spawn은 게임 시작 또는 리스폰 시에만 호출
        _currentPlayerState &= ~(EPlayerState.Dead);
        _currentPlayerState |= EPlayerState.Spawning;
        animator.SetBool("isDead", false);
        animator.SetTrigger("spawnTrigger");

        rigAimController.ClearWeight(0.0f);

        SetMovable(false);
    }

    // 사망 후 부활 연출. Spawn()은 부활 외의 경로에서도 호출될 수 있으므로
    // 연출은 분리해 부활 흐름(GameManager.RebirthGame)에서만 호출한다.

    // 리부팅이 끝나 System Restored 문구가 뜨는 순간의 알림음
    public void PlayRestoredSound()
    {
        if (restoredClip == null) return;

        seSource.Stop();
        seSource.clip = restoredClip;
        // 클립 길이를 넘기면 AudioSource.time이 에러를 내므로 끝 직전으로 제한한다.
        seSource.time = Mathf.Min(restoredClipStartTime, Mathf.Max(0.0f, restoredClip.length - 0.01f));
        seSource.Play();
    }

    // 부활 순간 플레이어 위치에 이펙트 생성
    public void PlayReviveEffect()
    {
        foreach (GameObject prefab in reviveEffectPrefabs)
        {
            if (prefab == null) continue;

            GameObject effect = Utils.Instantiate(prefab, transform.position, Quaternion.identity);
            Utils.Destroy(effect, reviveEffectLifetime);
        }
    }

    public void Die()
    {
        seSource.Stop();
        seSource.clip = deadClip;
        seSource.Play();

        // 사망 로직
        _currentPlayerState = 0;
        _invincibilityRefCount = 0;
        _currentPlayerState |= EPlayerState.Dead;

        animator.SetTrigger("deadTrigger");

        SetMovable(false);

        rigAimController.IsAim = false;
        // SetAllWeight는 진행 중인 가중치 보간 코루틴을 멈추지 않아, 사격 시작 직후 사망하면 팔 조준 가중치가 다시 올라간다.
        rigAimController.ClearWeight(0.0f);

        // 거의 없는 상황이지만 공중에 있을 떄 사망하는 경우도 고려할 것
        // 버프, 디버프도 마찬가지
        CancelAllActions();

        _isLowHp = false;
        if (lowHpController != null)
        {
            lowHpController.SetEffectActive(false);
        }
    }

    // 컷씬 등으로 조작이 잠기는 순간 호출해 기본 서 있는 상태로 되돌린다.
    // 사격 취소와 이동 정지는 키를 뗄 때(canceled)만 처리되므로, 누른 채로 잠기면 잠금 중에도 사격이 계속되고
    // HandleMove가 멈춰 애니메이터 이동 값이 마지막 입력에 고정된 채 걷기 애니메이션(과 발소리)이 이어진다.
    // 잠금이 풀린 뒤 이동은 키를 누르고 있으면 바로 이어지고, 사격은 다시 눌러야 한다.
    public void ResetToIdle()
    {
        CancelAllActions();

        _followCamera.CurrentCameraState = (ECameraState)(_currentAnimType);
        rigAimController.SmoothChangeWeight("ArmLAim", false);
        rigAimController.SmoothChangeWeight("ArmRAim", false);

        _currentMoveInput = Vector2.zero;
        SwitchStateToIdle();
    }

    // 사격, 대시, 스킬 등 진행 중인 행동을 모두 강제로 끝낸다. (사망, 컷씬 공용)
    // CancleAttack은 과열 전 기본 팔이면 취소를 건너뛰므로 여기서는 쓰지 않는다.
    private void CancelAllActions()
    {
        // 대시 중이면 아래 FinishActionForced -> FinishDash가 _previousState와 현재 사격 플래그로 사격을 다시 시작하므로 먼저 비운다.
        _previousState = 0;
        _currentPlayerState &= ~(EPlayerState.ShootState | EPlayerState.Rotating);

        inventory.EquippedItems[EPartType.ArmL][0].UseCancleAbility();
        inventory.EquippedItems[EPartType.ArmR][0].UseCancleAbility();
        animator.SetBool("isLeftAttack", false);
        animator.SetBool("isRightAttack", false);
        _isLeftAttackReady = false;
        _isRightAttackReady = false;
        Stats.RemoveModifier(this);
        _isAimMove = false;
        _aimMoveWeight = 0.0f;
        SetOvrrideAnimator(_currentAnimType);

        for (int i = 0; i < Enum.GetValues(typeof(EPartType)).Length; ++i)
        {
            inventory.EquippedItems[(EPartType)(1 << i)][0].FinishActionForced();
        }
    }

    // To-do: 추후 애니메이션 이벤트로 변경할 것
    public void CheckSpawnAnimationEnd()
    {
        if ((_currentPlayerState & EPlayerState.Spawning) == 0) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // 스폰 애니메이션 이름 또는 해시코드로 비교
        if (!stateInfo.IsName("Spawn"))
        {
            // 스폰 애니메이션이 종료됨
            _currentPlayerState &= ~EPlayerState.Spawning;

            SetMovable(true);

            rigAimController.IsAim = true;
            rigAimController.SmoothChangeBaseWeight(true);

            // To-do: 상태이상이 추가될 경우 해당 로직으로 변경
            StartCoroutine(CoStartInvincibility(3.0f));
        }
    }

    public void SetMovable(bool canMove)
    {
        _canMove = canMove;
        _canRotatable = canMove;
    }

    public void SetMovable(bool canMove, bool canRotatable)
    {
        _canMove = canMove;
        _canRotatable = canRotatable;
    }

    public void Dash(float dashSpeed)
    {
        legsAnimator.enabled = false;
        SetMotionBlur(true);

        _dashSpeed = dashSpeed;

        // 대시 중 재대시하면 사격 플래그가 이미 지워진 상태이므로, 덮어쓰면 첫 대시에서 저장한 사격 상태가 사라진다.
        _previousState |= _currentPlayerState & EPlayerState.ShootState;
        animator.SetBool("isLeftAttack", false);
        animator.SetBool("isRightAttack", false);

        if ((_currentPlayerState & EPlayerState.Dashing) != 0)
        {
            animator.SetTrigger("dashTrigger");
        }
        animator.SetBool("isDashing", true);

        _currentPlayerState &= ~EPlayerState.ActionState;
        _currentPlayerState |= EPlayerState.Dashing;

        if (_moveInput == null || _moveInput == Vector2.zero)
        {
            animator.SetFloat("dashX", 0.0f);
            animator.SetFloat("dashY", 1.0f);
        }
        else
        {
            animator.SetFloat("dashX", _moveInput.x);
            animator.SetFloat("dashY", _moveInput.y);
        }
    }

    public void FinishDash()
    {
        legsAnimator.enabled = true;
        SetMotionBlur(false);

        _dashDirection = Vector3.zero;
        _dashSpeed = 0.0f;

        _currentPlayerState |= _previousState;
        _currentPlayerState &= ~EPlayerState.Dashing;

        if ((_currentPlayerState & EPlayerState.LeftShooting) != 0)
        {
            Shoot(true);
        }
            
        if ((_currentPlayerState & EPlayerState.RightShooting) != 0)
        {
            Shoot(false);
        }

        animator.SetBool("isDashing", false);

        _previousState = 0;
        SwitchStateToIdle();
    }

    public void CancleAttack(bool isLeft)
    {
        // 대시 중에는 사격 플래그가 _previousState로 옮겨져 있으므로 함께 확인한다.
        // 현재 상태만 보면 대시 중 버튼을 떼거나 탄이 떨어졌을 때 취소가 무시된다.
        if (((_currentPlayerState | _previousState) & EPlayerState.ShootState) == 0) return;

        // 기본 무기일 경우 자동으로 사격을 종료하므로 사격 취소 로직을 실행하지 않도록 함
        PartBaseArm weapon = (PartBaseArm)(isLeft ? inventory.EquippedItems[EPartType.ArmL][0] : inventory.EquippedItems[EPartType.ArmR][0]);
        if (weapon is ArmBasic && !weapon.IsOverheat) return;

        if (isLeft)
        {
            inventory.EquippedItems[EPartType.ArmL][0].UseCancleAbility();

            animator.SetBool("isLeftAttack", false);
            _previousState &= ~EPlayerState.LeftShooting;
            _currentPlayerState &= ~EPlayerState.LeftShooting;
            _isLeftAttackReady = false;  // 상태 초기화
            rigAimController.SmoothChangeWeight("ArmLAim", false);
        }
        else
        {
            inventory.EquippedItems[EPartType.ArmR][0].UseCancleAbility();

            animator.SetBool("isRightAttack", false);
            _previousState &= ~EPlayerState.RightShooting;
            _currentPlayerState &= ~EPlayerState.RightShooting;
            _isRightAttackReady = false;
            rigAimController.SmoothChangeWeight("ArmRAim", false);
        }

        if (((_currentPlayerState | _previousState) & EPlayerState.ShootState) == 0)
        {
            Stats.RemoveModifier(this);
            _followCamera.CurrentCameraState = (ECameraState)(_currentAnimType);

            _isAimMove = false;

            if (inventory.EquippedItems[EPartType.ArmL][0] is ArmBasic && isLeft) return;

            rigAimController.SmoothChangeWeight("ArmLAim", false);
            rigAimController.SmoothChangeWeight("ArmRAim", false);
        }
    }

    // To-do: 데미지가 아니라 Stat을 넘겨주는 방식은 어떤가?
    // 방어 무시 등 공격자에 의존적인 스탯이 있을 경우에도 별 다른 참조 없이 바로 계산 가능
    public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1.0f, float defenceIgnoreRate = 0.0f)
    {
        if ((targetMask & (LayerMask)(1 << gameObject.layer)) == 0) return;
        TakeDamage(inDamage, defenceIgnoreRate, unitOfTime);
    }

    public void TakeDamage(float takeDamage, float defenceIgnoreRate, float unitOfTime)
    {
        if ((_currentPlayerState & EPlayerState.Spawning) != 0 ||
            (_currentPlayerState & EPlayerState.Invincibility) != 0 ||
            (_currentPlayerState & EPlayerState.Dead) != 0 ||
            (_currentPlayerState & EPlayerState.Cutscene) != 0) return;
        if (stats.CurrentHealth <= 0) return;

        var damage = Utils.GetDamage(takeDamage, defenceIgnoreRate, unitOfTime, stats.TotalStats);
        if (damage > 0)
        {
            if (!_isLowHp)
            {
                if (_hitRoutine != null)
                {
                    StopCoroutine(_hitRoutine);
                    _hitRoutine = null;
                }
                _hitRoutine = StartCoroutine(CoStartHitEffect(0.1f));
            }

            seSource.Stop();
            int randIndex = UnityEngine.Random.Range(0, hitClips.Count);
            seSource.clip = hitClips[randIndex];
            seSource.Play();

            stats.CurrentHealth -= damage;

            float damageRatio = damage / stats.MaxHealth;
            _followCamera.ApplyShake(impulseSource, damageRatio);
        }
        else
        {
            // TODO: 데미지가 음수일때 어떻게 처리할 것인지 논의 필요 (힐을 시킬 것인지 무시할 것인지)
        }

        if (!_isLowHp && (stats.CurrentHealth / stats.MaxHealth) <= 0.25f)
        {
            _isLowHp = true;

            // 피격 코루틴이 켜져 있었다면 상시 점등을 위해 코루틴을 끊어줍니다.
            if (_hitRoutine != null)
            {
                StopCoroutine(_hitRoutine);
                _hitRoutine = null;
            }

            // 항상 켜짐 상태 유지
            if (lowHpController != null)
            {
                lowHpController.SetEffectActive(true);
            }
        }

        if (stats.CurrentHealth <= 0)
        {
            if (lowHpController != null)
            {
                lowHpController.SetEffectActive(false);
            }
            Die();
        }
    }

    public void HealHp(float healAmount, EHealType healType)
    {
        if (healAmount <= 0.0f) return;
        if (stats.CurrentHealth <= 0) return;

        float amount = 0;
        switch (healType)
        {
            case EHealType.Flat:
                amount = healAmount;
                break;
            case EHealType.Percentage:
                amount = stats.MaxHealth * healAmount * 0.01f;
                break;
        }

        stats.CurrentHealth = Mathf.Clamp(stats.CurrentHealth + amount, 0.0f, stats.MaxHealth);

        if ((stats.CurrentHealth / stats.MaxHealth) >= 0.25f)
        {
            _isLowHp = false;
            if (lowHpController != null)
            {
                lowHpController.SetEffectActive(false);
            }
        }
    }

    public bool SetOvrrideAnimator(EAnimationType type)
    {
        if (animations.Count <= (int)type) return false;

        _currentAnimType = type;
        _shootAnimType = type + 4;

        animator.runtimeAnimatorController = animations[(int)type].overrideController;
        animator.SetBool("isOnlyLoop", animations[(int)type].isOnlyLoop);
        // 컨트롤러 교체로 파라미터가 기본값으로 초기화되므로, 사격 중 다리 파츠를 바꿔도 이동 블렌드가 끊기지 않게 즉시 되돌린다.
        animator.SetFloat(AimWeightHash, _aimMoveWeight);

        rigBuilder.enabled = false;
        rigBuilder.enabled = true;

        // Die()도 이 함수를 호출하므로 사망 상태를 함께 제외한다.
        // 그렇지 않으면 Die()에서 0으로 내린 머리/가슴 조준 가중치가 다시 올라가 사망 후에도 상체가 카메라를 따라간다.
        if ((_currentPlayerState & (EPlayerState.Spawning | EPlayerState.Dead)) == 0)
        {
            rigAimController.SmoothChangeBaseWeight(true);
        }

        // 사격 중 파츠 교체 시 취소하도록 (하체 파츠에 의존하는데 사격이 변경되지 않는 경우도 있지 않나?)
        //MultiAimConstraint aimObj = aimObjects[0].GetComponent<MultiAimConstraint>();
        //if (aimObj != null)
        //{
        //    aimObj.weight = _isLeftAttackReady ? 1.0f : 0.0f;
        //}

        //aimObj = aimObjects[0].GetComponent<MultiAimConstraint>();
        //if (aimObj != null)
        //{
        //    aimObj.weight = _isRightAttackReady ? 1.0f : 0.0f;
        //}

        SwitchStateToIdle();

        return true;
    }

    public bool SetOvrrideAnimator()
    {
        // 기본 팔 파츠일 경우 애니메이션 전환 X
        // 단발 사격이라 사격 상태가 짧아, 이동 블렌드가 잠깐 섞였다 돌아가는 것이 오히려 부자연스럽다.
        if (inventory.EquippedItems[EPartType.ArmL][0] is ArmBasic) return false;

        // 컨트롤러를 교체하지 않고 사격 중 이동 블렌드만 켠다. (실제 보간은 UpdateAimMoveWeight)
        _isAimMove = true;

        return true;
    }

    private void UpdateAimMoveWeight()
    {
        float target = _isAimMove ? 1.0f : 0.0f;
        float step = aimMoveBlendTime > 0.0f ? Time.deltaTime / aimMoveBlendTime : 1.0f;
        _aimMoveWeight = Mathf.MoveTowards(_aimMoveWeight, target, step);
        animator.SetFloat(AimWeightHash, _aimMoveWeight);
    }

    // 사격 중인 팔에 따라 허리 레이어 가중치와 팔 조준 IK 상한을 바꾼다.
    private void UpdateShootTorsoBlend()
    {
        bool isLeft = (_currentPlayerState & EPlayerState.LeftShooting) != 0;
        bool isRight = (_currentPlayerState & EPlayerState.RightShooting) != 0;
        bool isDual = isLeft && isRight;
        rigAimController.IsDualAim = isDual;

        // 사격하지 않는 쪽 허리 레이어는 꺼 둔다. 팔 레이어가 대기 상태일 때 허리를 덮어쓰지 않게 한다.
        float torsoWeight = isDual ? dualShootTorsoWeight : singleShootTorsoWeight;
        BlendLayerWeight(LeftTorsoLayerName, isLeft ? torsoWeight : 0.0f);
        BlendLayerWeight(RightTorsoLayerName, isRight ? torsoWeight : 0.0f);
    }

    private void BlendLayerWeight(string layerName, float target)
    {
        // 파츠 교체로 컨트롤러가 바뀌어도 레이어 구성은 같지만, 순서가 바뀔 수 있어 이름으로 찾는다.
        int layerIndex = animator.GetLayerIndex(layerName);
        if (layerIndex < 0) return;

        float step = shootTorsoBlendTime > 0.0f ? Time.deltaTime / shootTorsoBlendTime : 1.0f;
        float weight = Mathf.MoveTowards(animator.GetLayerWeight(layerIndex), target, step);
        animator.SetLayerWeight(layerIndex, weight);
    }

    public void ApplyRecoil(CinemachineImpulseSource source, float recoilX, float recoilY)
    {
        _followCamera.ApplyRecoil(source, recoilX, recoilY);
    }

    public void SetPartStat(PartBase part)
    {
        stats.SetPartStats(part);

        // 사격 중 파츠 교체하면 사격 취소
        // 스킬 중에는 파츠 교체 불가능하도록 반영

        if (part.PartType == EPartType.Legs)
        {
            _currentMovement = part as ILegsMovement;
            if (_currentMovement is LegsHover || _currentMovement is LegsCaterpillar)
            {
                legsAnimator.enabled = false;
            }
            else
            {
                legsAnimator.enabled = true;
            }
        }
    }

    // Ball Legs를 위한 임시 점프 함수
    public void PartJump(float jumpVelocity)
    {
        if (!_isGrounded) return;

        Vector3 forward = _followCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        _fallVelocity = forward * jumpVelocity * 0.5f;
        _fallVelocity.y = jumpVelocity;

        _fallVelocity.y = jumpVelocity;
        _totalDirection += _fallVelocity;
    }

    public void TriggerPlatformEnd()
    {
        _isPlatformEnd = true;

        _isOnPlatform = false;
        _postPlatform = null;
        _platformVelocity = Vector3.zero;

        if (_currentMovement is LegsHover || _currentMovement is LegsCaterpillar)
        {
            legsAnimator.enabled = false;
        }
        else
        {
            legsAnimator.enabled = true;
        }
    }

    public void SetPlayerState(EPlayerState newState, bool isAdd)
    {
        if (isAdd)
        {
            _currentPlayerState |= newState;
        }
        else
        {
            _currentPlayerState &= ~(newState);
        }
    }

    // 무적을 부여한 출처별로 누적 카운트한다. 서로 다른 출처(스폰 i-frame, 안전지대 등)가
    // 같은 Invincibility 플래그를 통째로 끄지 않도록 한다.
    public void AddInvincibility()
    {
        _invincibilityRefCount++;
        _currentPlayerState |= EPlayerState.Invincibility;
    }

    public void RemoveInvincibility()
    {
        _invincibilityRefCount = Mathf.Max(0, _invincibilityRefCount - 1);
        // 남은 출처가 없고 테스트 무적도 아닐 때만 실제로 해제한다.
        if (_invincibilityRefCount == 0 && !TestManager.PlayerInvincible)
        {
            _currentPlayerState &= ~EPlayerState.Invincibility;
        }
    }

    public void CloseRadialUI()
    {
        // UI 비활성화 시 커서 숨기고 고정
        SelectAndChangePart(Managers.GUIManager.Instance.GameUIController.SelectedIndex, Managers.GUIManager.Instance.GameUIController.SelectedPartIndex);
        Managers.GUIManager.Instance.GameUIController.ToggleRadialUI(false);
    }

    /// <summary>
    /// 지정 위치의 지면에 발을 맞춰 플레이어를 순간이동시킨다.
    ///
    /// 좌표를 그대로 대입하면 캡슐이 바닥에 파묻히거나 살짝 뜬 상태로 놓여,
    /// CharacterController의 침투 해소와 접지 판정(groundCheck 박스 겹침)이 서로 밀고 당기며
    /// 착지 순간 위아래로 튕기는 현상이 생긴다.
    /// 아래로 레이를 쏴 실제 지면을 찾고, 발밑 기준점이 그 위에 오도록 보정한다.
    /// 지면을 찾지 못하면 원래 좌표를 그대로 사용한다(공중 배치 등).
    /// </summary>
    public void TeleportGrounded(Vector3 targetPosition)
    {
        const float rayUpOffset = 3.0f;     // 목표 지점보다 이만큼 위에서 아래로 탐색
        const float rayDistance = 20.0f;
        const float groundSkin = 0.02f;     // 바닥에 정확히 붙이면 파묻히므로 살짝 띄운다

        // 루트와 발밑 기준점의 간격. 캐릭터마다 원점이 발이 아닐 수 있으므로 실측값을 쓴다.
        Vector3 footOffset = groundCheck != null
            ? transform.position - groundCheck.position
            : Vector3.zero;

        if (Physics.Raycast(targetPosition + Vector3.up * rayUpOffset, Vector3.down,
                out RaycastHit hit, rayDistance, groundLayerMask))
        {
            targetPosition = hit.point + footOffset + Vector3.up * groundSkin;
        }

        // 위치를 강제로 바꾸는 동안에는 컨트롤러를 꺼둔다. (코드베이스의 다른 순간이동과 동일한 패턴)
        bool wasEnabled = characterController != null && characterController.enabled;
        if (characterController != null) characterController.enabled = false;

        transform.position = targetPosition;
        Physics.SyncTransforms();

        if (characterController != null) characterController.enabled = wasEnabled;

        // 이동 중 누적된 낙하 속도를 지운다. 남아 있으면 배치 직후 바닥을 뚫거나 튄다.
        ResetGravityAndFalling();
    }

    public void ResetGravityAndFalling()
    {
        _fallVelocity = Vector3.zero;
        _groundCheckTimer = 0.0f;

        // 낙하 상태 플래그 해제 및 애니메이션 초기화
        _currentPlayerState &= ~EPlayerState.Falling;
        if (animator != null)
        {
            animator.SetBool("isFalling", false);
        }
    }
    #endregion

    #region Private Methods
    private void HandleMove()
    {
        if (!_canMove) return;
        if ((_currentPlayerState & movementBlockMask) != 0) return;

        // Lerp 블렌딩 적용 (0.1f는 변화 속도, 필요에 따라 수정)
        _currentMoveInput = Vector2.Lerp(_currentMoveInput, _moveInput, 0.1f);

        if (_moveInput == null || _moveInput == Vector2.zero)
        {
            SwitchStateToIdle();
        }
        else
        {
            _currentPlayerState &= ~EPlayerState.Idle;
            _currentPlayerState |= EPlayerState.Moving;
        }

        _moveDirection = new Vector3(_moveInput.x, 0.0f, _moveInput.y).normalized;
        if (inventory.EquippedItems[EPartType.Legs][0].IsAnimating)
        {
            animator.SetFloat("moveX", _currentMoveInput.x);
            animator.SetFloat("moveY", _currentMoveInput.y);
            animator.SetFloat("moveMagnitude", _moveDirection.magnitude);
        }
        
        _totalDirection += _currentMovement.GetMoveDirection(_moveInput, transform, _followCamera.transform);
    }

    private void SwitchStateToIdle()
    {
        _currentPlayerState &= ~EPlayerState.Moving;
        _currentPlayerState |= EPlayerState.Idle;
        _moveDirection = Vector3.zero;
        animator.SetFloat("moveX", _postMoveInput.x);
        animator.SetFloat("moveY", _postMoveInput.y);
        animator.SetFloat("moveMagnitude", 0.0f);
    }

    private Vector3 CalculateInputDirection()
    {
        Vector3 camForward = _followCamera.transform.forward;
        Vector3 camRight = _followCamera.transform.right;
        camForward.y = 0.0f;
        camRight.y = 0.0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 camDirection = camForward * _moveInput.y + camRight * _moveInput.x;

        return camDirection.normalized;
    }

    private void DashMove()
    {
        if (!_canMove) return;
        if ((_currentPlayerState & EPlayerState.Dashing) == 0) return;

        _totalDirection += _dashDirection * _dashSpeed;
        _followCamera.CameraTarget.position = transform.position + new Vector3(0.0f, 1.2f, 0.0f);
    }

    private void HandleGravity()
    {
        // 대시 중이라면 중력 무시
        if (((_currentPlayerState & EPlayerState.Dashing)) != 0)
        {
            // 대시 중에는 중력 벡터를 초기화하거나 그대로 유지(중력 영향 없음)
            _fallVelocity = Vector3.zero;
            _groundCheckTimer = 0.0f;
            return;
        }

        // 호버링 중이라면 따로 중력 로직 적용
        if (_currentMovement is LegsHover hoverLegs)
        {
            Vector3 hoverDelta = hoverLegs.CalculateHoverDeltaY();
            _totalDirection += hoverDelta;

            _currentPlayerState &= ~EPlayerState.Falling;
            animator.SetBool("isFalling", false);

            if (_isOnPlatform)
            {
                Vector3 platformDelta = (_postPlatform.position - _lastPlatformPosition);
                _lastPlatformPosition = _postPlatform.position;

                platformDelta.x = 0.0f;
                platformDelta.z = 0.0f;
                _platformVelocity = platformDelta;
                characterController.Move(_platformVelocity);
                _groundCheckTimer = 0.0f;
            }

            return;
        }

        // 플랫폼 위에 있는 플랫폼 상태일 경우 따로 로직 적용
        if (_isOnPlatform)
        {
            Vector3 platformDelta = (_postPlatform.position - _lastPlatformPosition);
            _lastPlatformPosition = _postPlatform.position;

            platformDelta.x = 0.0f;
            platformDelta.z = 0.0f;
            _platformVelocity = platformDelta;
            characterController.Move(_platformVelocity);
            _groundCheckTimer = 0.0f;
            return;
        }

        _isGrounded = Physics.CheckBox(groundCheck.position, boxSize, Quaternion.identity, groundLayerMask);
        if (_isGrounded && _fallVelocity.y <= 0.0f)
        {
            _currentPlayerState &= ~EPlayerState.Falling;
            animator.SetBool("isFalling", false);
            _groundCheckTimer = 0.0f;

            _fallVelocity.y = -20.0f * gravityScale * Time.deltaTime; // 약간의 하강력 유지로 땅에 붙어있게 함
            _totalDirection += _fallVelocity;
            return;
        }

        _groundCheckTimer += Time.deltaTime;
        if (_groundCheckTimer >= _groundCheckBufferTime)
        {
            _currentPlayerState |= EPlayerState.Falling;
            animator.SetBool("isFalling", true);

            _fallVelocity.y += -9.8f * gravityScale * Time.deltaTime;
            _totalDirection += _fallVelocity;
        }
    }

    private void RotateCharacter()
    {
        if (!_canRotatable) return;

        if ((_currentPlayerState & EPlayerState.RotateState) == 0)
        {
            _currentPlayerState &= ~EPlayerState.Rotating;
            return;
        }

        Vector3 lookDirection = _followCamera.transform.forward;
        lookDirection.y = 0;

        if (lookDirection.sqrMagnitude > 0.1f)
        {
            // 회전 중 상태 활성화
            _currentPlayerState |= EPlayerState.Rotating;

            // 목표 회전 방향
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // 현재 회전과 목표 회전 사이의 각도
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            // Slerp로 부드럽게 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (angleDifference < 10f)
            {
                _currentPlayerState &= ~EPlayerState.Rotating;
            }

            // 각도가 충분히 줄면 회전 종료 처리
            if (angleDifference < 1f)  // 1도 이내면 회전 완료
            {
                transform.rotation = targetRotation;  // 완전히 맞춤
            }
        }
        else
        {
            _currentPlayerState &= ~EPlayerState.Rotating;
        }
    }

    private void Shoot(bool isLeft)
    {
        if ((_currentPlayerState & shootBlockMask) != 0) return;
        _currentPlayerState |= EPlayerState.Rotating;
        _isLeftAttackReady = false;
        _isRightAttackReady = false;

        // Shoot State가 아닐 경우, 즉 왼팔 사격이나 오른팔 사격만을 하는 경우
        // 특정 로직의 중복 실행을 막기 위함이며 Shoot 함수가 실행될 떄 사격 상태가 아닌 경우는 없으므로 그 부분은 고려하지 않았음
        if ((_currentPlayerState & EPlayerState.ShootState) != EPlayerState.ShootState)
        {
            SetOvrrideAnimator();
            stats.AddModifier(new StatModifier(EStatType.WalkSpeed, EStackType.PercentMul, -0.3f, this));
        }

        //_followCamera.ApplyAimAssist();

        if ((_currentPlayerState & EPlayerState.LeftShooting) != 0 && isLeft)
        {
            if (inventory.EquippedItems[EPartType.ArmL][0].IsZooming)
            {
                _followCamera.CurrentCameraState = (ECameraState)(_shootAnimType);
            }

            if (inventory.EquippedItems[EPartType.ArmL][0].IsAnimating)
            {
                animator.SetBool("isLeftAttack", true);
            }
        }

        if ((_currentPlayerState & EPlayerState.RightShooting) != 0 && !isLeft)
        {
            if (inventory.EquippedItems[EPartType.ArmR][0].IsZooming)
            {
                _followCamera.CurrentCameraState = (ECameraState)(_shootAnimType);
            }

            if (inventory.EquippedItems[EPartType.ArmR][0].IsAnimating)
            {
                animator.SetBool("isRightAttack", true);
            }
        }
    }

    private void AnimCheckShoot()
    {
        // Left attack 애니메이션 상태 체크
        if ((_currentPlayerState & EPlayerState.LeftShooting) != 0 && !_isLeftAttackReady)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);
            if (stateInfo.IsName("Shoot") && !animator.IsInTransition(1))
            {
                _isLeftAttackReady = true;
                rigAimController.SmoothChangeWeight("ArmLAim", true);
                inventory.EquippedItems[EPartType.ArmL][0].UseAbility();
            }
        }

        if ((_currentPlayerState & EPlayerState.RightShooting) != 0 && !_isRightAttackReady)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(2);
            if (stateInfo.IsName("Shoot") && !animator.IsInTransition(2))
            {
                _isRightAttackReady = true;
                rigAimController.SmoothChangeWeight("ArmRAim", true);
                inventory.EquippedItems[EPartType.ArmR][0].UseAbility();
            }
        }   
    }

    private void SetMotionBlur(bool isOn)
    {
        if (_motionBlur)
        {
            _motionBlur.active = isOn; // 또는 blur.intensity.overrideState = enabled;
        }
    }

    public void SelectAndChangePart(int partType, int attackType)
    {
        if (attackType < 0) return;

        PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
        if (player)
        {
            switch (partType)
            {
                case 0:
                    // 등/어깨
                    player.Inven.EquipItem(EPartType.Shoulder, (EAttackType)(1 << attackType));
                    player.Inven.EquipItem(EPartType.Back, (EAttackType)(1 << attackType));
                    break;
                case 1:
                    // 다리
                    player.Inven.EquipItem(EPartType.Legs, (EAttackType)(1 << attackType));
                    break;
                case 2:
                    // 왼팔
                    player.Inven.EquipItem(EPartType.ArmL, (EAttackType)(1 << attackType));
                    break;
                case 3:
                    // 오른팔
                    player.Inven.EquipItem(EPartType.ArmR, (EAttackType)(1 << attackType));
                    break;
            }
        }

        Managers.GUIManager.Instance.GameUIController.SetCurrentPartIcon(partType, attackType);

        if (IsFullSet((EAttackType)(1 << attackType), inventory.EquippedItems[EPartType.Shoulder][0].AttackType,
            inventory.EquippedItems[EPartType.ArmL][0].AttackType,
            inventory.EquippedItems[EPartType.ArmR][0].AttackType,
            inventory.EquippedItems[EPartType.Legs][0].AttackType)
            )
        {
            player.Inven.EquipItem(EPartType.Mask, (EAttackType)(1 << attackType));

            switch ((EAttackType)(1 << attackType))
            {
                case EAttackType.Basic:
                    for (int i = 0; i < bodyRenderers.Count; ++i)
                    {
                        bodyRenderers[i].material = basicMaterials[i];
                    }
                    Managers.GUIManager.Instance.GameUIController.SetPartSetIcon(0);
                    break;
                case EAttackType.Laser:
                    for (int i = 0; i < bodyRenderers.Count; ++i)
                    {
                        bodyRenderers[i].material = laserMaterials[i];
                    }
                    Managers.GUIManager.Instance.GameUIController.SetPartSetIcon(1);
                    break;
                case EAttackType.Rapid:
                    for (int i = 0; i < bodyRenderers.Count; ++i)
                    {
                        bodyRenderers[i].material = rapidMaterials[i];
                    }
                    Managers.GUIManager.Instance.GameUIController.SetPartSetIcon(2);
                    break;
                case EAttackType.Heavy:
                    for (int i = 0; i < bodyRenderers.Count; ++i)
                    {
                        bodyRenderers[i].material = heavyMaterials[i];
                    }
                    Managers.GUIManager.Instance.GameUIController.SetPartSetIcon(3);
                    break;
            }
        }
        else
        {
            player.Inven.EquipItem(EPartType.Mask, EAttackType.Basic);
            for (int i = 0; i < bodyRenderers.Count; ++i)
            {
                bodyRenderers[i].material = basicMaterials[i];
            }

            Managers.GUIManager.Instance.GameUIController.SetPartSetIcon(0);
        }
    }

    private bool IsFullSet(EAttackType main, EAttackType back, EAttackType leftArm, EAttackType rightArm, EAttackType leg)
    {
        return (main == back) && ((back == leftArm) && (leftArm == rightArm) && (rightArm == leg));
    }

    private IEnumerator CoStartInvincibility(float duration)
    {
        AddInvincibility();

        yield return new WaitForSeconds(duration);

        RemoveInvincibility();
    }

    private IEnumerator CoStartIndicatorTimer(float duration)
    {
        yield return new WaitForSeconds(duration);

        Managers.GUIManager.Instance.GameUIController.SetIndicator(false);
        _indicatorRoutine = null;
    }

    private IEnumerator CoStartHitEffect(float duration)
    {
        if (lowHpController != null)
        {
            lowHpController.SetEffectActive(true);
        }

        yield return new WaitForSeconds(duration);

        if (!_isLowHp && lowHpController != null)
        {
            lowHpController.SetEffectActive(false);
        }
        _hitRoutine = null;
    }

    /// 온스크린 버튼의 입력 버퍼 예외를 방지하기 위해 한 프레임 지연 후 UI 상태를 제어하는 통합 코루틴입니다.
    private System.Collections.IEnumerator CoProcessPauseInput()
    {
        // 닫기/열기 버튼을 누른 순간의 가상 패드 입력 연산이 안전하게 끝날 때까지 한 프레임 대기합니다.
        yield return null;

        var uiController = Managers.GUIManager.Instance.GameUIController;

        // 1. 도움말(조작법) UI가 켜져 있는 경우
        if (uiController.HelpUI.activeSelf)
        {
            uiController.HelpUI.SetActive(false);
            uiController.RestoreHUD();

            if (!uiController.PauseUI.activeSelf)
            {
                _followCamera.OnUIClose();
                uiController.RestoreHUD();
                Time.timeScale = 1.0f;
            }
            yield break;
        }

        // 2. 월드맵 UI가 켜져 있는 경우
        if (uiController.WorldMap.activeSelf)
        {
            uiController.WorldMap.SetActive(false);
            uiController.RestoreHUD();

            if (!uiController.PauseUI.activeSelf)
            {
                _followCamera.OnUIClose();
                uiController.RestoreHUD();
                Time.timeScale = 1.0f;
            }
            yield break;
        }
        
        // 3. 튜토리얼 UI가 켜져 있는 경우
        if (uiController.Tutorial.activeSelf)
        {
            uiController.Tutorial.SetActive(false);
            yield break;
        }

        // 4. 옵션 UI가 켜져 있는 경우
        if (uiController.Option.activeSelf)
        {
            uiController.Option.SetActive(false);
            yield break;
        }

        // 5. 그 외 기본 일시정지 UI 토글 처리
        if (!uiController.PauseUI.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _followCamera.OnUIOpen();
            uiController.PauseUI.SetActive(true);
            uiController.HUD.SetActive(false);
            Time.timeScale = 0.0f;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _followCamera.OnUIClose();
            uiController.PauseUI.SetActive(false);
            uiController.RestoreHUD();
            Time.timeScale = 1.0f;
        }
    }
    #endregion

    public void Init(Dictionary<EPlayerPrefabType, GameObject> createdObj)
    {
        if (_isInit) return;
        
        if (volume is null)
        {
            GameObject volumeObj = createdObj[EPlayerPrefabType.Volume];
            volume = volumeObj.GetComponent<Volume>();
        }

        if (Navi is null)
        {
            GameObject naviObj = createdObj[EPlayerPrefabType.Navi];
            navi = naviObj.GetComponent<ParticleFollower>();
        }

        lowHpController = null;
        if (lowHpController is null)
        {
            GameObject lowHpObj = createdObj[EPlayerPrefabType.LowHp];
            lowHpController = lowHpObj.GetComponent<MaterialPropertyController>();
        }
        
        // 이전 Awake 내용
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null)
        {
            rigBuilder = GetComponentInChildren<RigBuilder>();
        }
        legsAnimator = GetComponent<LegsAnimator>();
        if (legsAnimator == null)
        {
            legsAnimator = GetComponentInChildren<LegsAnimator>();
        }
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = GetComponentInChildren<CharacterController>();
        }

        impulseSource = GetComponent<CinemachineImpulseSource>();

        stats = GetComponent<CharacterStat>();
        inventory = GetComponent<Inventory>();
        rigAimController = GetComponent<RigAimController>();

        foreach (EPartType partType in Enum.GetValues(typeof(EPartType)))
        {
            cooldownDict.Add(partType, 0.0f);
        }

        GroundCheck gc = GetComponentInChildren<GroundCheck>();
        if (gc != null)
        {
            groundCheck = gc.transform;
        }

        navi.gameObject.SetActive(false);

        // 비트 마스크 방식으로 레이케스트를 관리할 레이어를 설정
        // 마스크 값이 비어있다면 기본 값(모든 레이어 - 일부 레이어)로 설정
        if (groundLayerMask == 0)
        {
            groundLayerMask = ~0;
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("UI"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Face"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Hair"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Outline"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Player"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("PlayerMesh"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Bullet"));
            groundLayerMask &= ~(1 << LayerMask.NameToLayer("Minimap"));
        }

        _playerActions = new PlayerActions();
        _playerActions.PlayerActionMap.SetCallbacks(this);

        // VolumeProfile 가져오기
        VolumeProfile profile = volume.profile;     // 공유 프로필을 쓸 경우 sharedProfile을 사용                      
        profile.TryGet<MotionBlur>(out _motionBlur);   // MotionBlur 오버라이드 얻기
        
        _followCamera = FindFirstObjectByType<FollowCameraController>();
        if (_followCamera == null)
        {
            GameObject cameraObject = Instantiate(followCameraPrefab);
            cameraObject.name = followCameraPrefab.name;
            _followCamera = cameraObject.GetComponent<FollowCameraController>();
        }
        _followCamera.InitFollowCamera(gameObject);
        inventory.Init();
        SetOvrrideAnimator(EAnimationType.Base);

        Spawn();
        _followCamera.SetCameraRotatable(false);

        // 씬 로딩 시점에는 인게임 조작 인풋을 기본적으로 비활성화해 둡니다.
        // 프롤로그 상영 중이나 로딩 중에 플레이어가 임의로 움직이거나 입력을 받는 것을 방지합니다.
        _playerActions.PlayerActionMap.Disable();

        _isInit = true;
    }

    // GameManager 등 외부 핵심 매니저 계층에서 플레이어의 조작 권한을 직접 제어할 수 있도록 인터페이스를 노출합니다.
    public void TogglePlayerInput(bool enable)
    {
        if (_playerActions == null) return;

        if (enable)
        {
            _playerActions.PlayerActionMap.Enable();
            Debug.Log("<color=green>[PlayerInput]</color> 인게임 플레이어 조작이 활성화되었습니다.");
        }
        else
        {
            _playerActions.PlayerActionMap.Disable();
            Debug.Log("<color=red>[PlayerInput]</color> 인게임 플레이어 조작이 차단되었습니다.");
        }
    }

    public void PlayIntroSequence(float duration, Action onComplete)
    {
        if (lowHpController != null)
        {
            lowHpController.SetEffectActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(IntroCameraRoutine(duration, onComplete));
    }

    private IEnumerator IntroCameraRoutine(float duration, Action onComplete)
    {
        yield return null;

        if (_followCamera != null)
        {
            _followCamera.SetCameraRotatable(false);
        }

        if (animator != null)
        {
            animator.SetBool("isIntro", true);
        }

        var brain = Camera.main?.GetComponent<CinemachineBrain>();
        CinemachineBlendDefinition originalBlend = default;
        if (brain != null)
        {
            originalBlend = brain.m_DefaultBlend;

            // defaultCam에서 startCam으로 넘어갈 때는 화면이 튀지 않고 즉시 고정되도록 블렌딩 스타일을 Cut으로 강제 변경합니다.
            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.Cut, 0f);
        }

        startCam.Priority = 20;
        GUIManager.Instance.GameUIController.HUD.SetActive(false);
        yield return new WaitForSeconds(1.0f);

        float customBlendTime = 0.5f;
        if (brain != null)
        {
            // 스타일을 originalBlend에서 가져오면, 직전에 누군가 브레인을 Cut으로 바꿔둔 상태였을 때
            // (Cut, 0.5초)가 되어 시간이 무시되고 화면이 즉시 전환된다.
            // 인트로 종료 후에는 항상 부드럽게 넘어가야 하므로 스타일을 명시한다.
            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.EaseInOut, customBlendTime);
        }

        GUIManager.Instance.GameUIController.FadeIn(4.0f);

        // 연출 부분
        if (startCam != null)
        {
            float elapsed = 0f;

            // 기획 변수 설정
            float radius = 0.4f;           // 플레이어와 카메라 사이의 수평 유지 거리
            float startAngle = -60f;       // 초기 위치: 우측 대각선 뒤 (플레이어 기준 대략 -45도 혹은 원하는 각도로 조율)
            float endAngle = 25f;         // 목표 위치: 좌측 대각선 앞까지 (총 180도 시계 방향 회전)

            float startHeightY = 0.1f;     // 초기 높이: 발목/바닥 부근
            float endHeightY = 1.7f;       // 목표 높이: 머리/눈높이 부근

            float startLookHeightY = 0.2f;
            float endLookHeightY = 1.6f;

            // 앙상블 회전을 위해 플레이어의 중심(피벗) 위치를 참조합니다.
            // 만약 캐릭터 발바닥이 피벗이라면 Vector3.zero를, 필요 시 오프셋을 더해 기준점을 잡습니다.
            Vector3 centerTargetPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 시간에 따른 각도 및 높이 선형 보간 (Lerp)
                float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                float currentHeightY = Mathf.Lerp(startHeightY, endHeightY, t);

                // 시선 타겟의 높이도 시간에 따라 별도로 보간합니다.
                float currentLookHeightY = Mathf.Lerp(startLookHeightY, endLookHeightY, t);

                // 삼각함수를 이용한 플레이어 기준 수평 원형 좌표(X, Z) 계산
                // 유니티의 각도 계산(라디안)에 맞춰 세팅 (시계 방향 회전을 유도)
                float rad = currentAngle * Mathf.Deg2Rad;
                float offsetX = Mathf.Sin(rad) * radius;
                float offsetZ = Mathf.Cos(rad) * radius;

                // 최종 월드 좌표를 조립하여 startCam 위치에 강제 주입
                Vector3 newCameraPos = new Vector3(
                    centerTargetPos.x + offsetX,
                    centerTargetPos.y + currentHeightY,
                    centerTargetPos.z + offsetZ
                );
                startCam.transform.position = newCameraPos;

                // 분리된 시선 높이 좌표를 적용하여 카메라 회전값을 정렬합니다.
                Vector3 lookTarget = centerTargetPos + Vector3.up * currentLookHeightY;
                startCam.transform.LookAt(lookTarget);

                yield return null;
            }

            // 상승 종료 후 높이를 유지한 채 부드럽게 추가 회전하는 시퀀스
            float lingerElapsed = 0f;
            float lingerDuration = 1.5f;    // 여운을 즐길 총 대기 시간 (원하시는 만큼 늘리셔도 됩니다)
            float lingerEndAngle = 45f;     // 여운 회전의 최종 도달 각도

            float startRadius = radius;     // 0.4f에서 시작
            float endRadius = 0.35f;         // 2초 동안 서서히 0.8f까지 멀어짐 (원하는 거리로 조율 가능)

            while (lingerElapsed < lingerDuration)
            {
                lingerElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(lingerElapsed / lingerDuration);

                // 높이는 상승 단계의 최종 목적지(endHeightY)로 완전히 고정하고, 각도만 endAngle(30도)에서 lingerEndAngle(55도)까지 느긋하게 이어 돌립니다.
                float currentAngle = Mathf.Lerp(endAngle, lingerEndAngle, t);

                // 시간에 따라 카메라와 플레이어 사이의 거리를 서서히 벌려줍니다.
                float currentRadius = Mathf.Lerp(startRadius, endRadius, t);

                // 고정된 radius 대신 매 프레임 멀어지는 currentRadius를 적용하여 삼각함수 좌표를 계산합니다.
                float rad = currentAngle * Mathf.Deg2Rad;
                float offsetX = Mathf.Sin(rad) * currentRadius;
                float offsetZ = Mathf.Cos(rad) * currentRadius;

                // Y축 높이 값들은 최종 상태로 고정하여 수평 패닝만 유도합니다.
                Vector3 newCameraPos = new Vector3(
                    centerTargetPos.x + offsetX,
                    centerTargetPos.y + endHeightY,
                    centerTargetPos.z + offsetZ
                );
                startCam.transform.position = newCameraPos;

                // 시선 역시 머리 높이(endLookHeightY)에 고정된 상태로 캐릭터를 유지합니다.
                Vector3 lookTarget = centerTargetPos + Vector3.up * endLookHeightY;
                startCam.transform.LookAt(lookTarget);

                yield return null;
            }
        }

        // 시네머신 브레인이 화면을 전환하는 최소한의 컷 블렌딩 시간을 확보합니다.
        startCam.Priority = 10;
        yield return new WaitForSeconds(customBlendTime + 0.2f);

        if (brain != null)
        {
            brain.m_DefaultBlend = originalBlend;
        }

        if (animator != null)
        {
            animator.SetBool("isIntro", false);
        }
        GUIManager.Instance.GameUIController.HUD.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        // 카메라 전환이 완전히 끝난 시점에 인게임 조작 인풋을 최종 활성화합니다.
        if (_followCamera != null)
        {
            _followCamera.SetCameraRotatable(true);
        }
        _playerActions.PlayerActionMap.Enable();

        // 시작 연출이 끝나면 조작법을 먼저 보여준다. F1로 여는 것과 같은 상태(일시정지)이며 F1로 닫는다.
        if (showKeyGuideOnStart)
        {
            GUIManager.Instance.GameUIController.ShowKeyGuide();
        }

        onComplete?.Invoke();
    }

    #region Debug
    // [임시] 사격 불가 버그 추적용 상태 덤프. DebugPlayerStateDump(F10)가 호출한다. 원인 확정 후 함께 제거한다.
    public string BuildShootDebugReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("===== [ShootDebug] 플레이어 상태 덤프 =====");
        sb.AppendLine($"CurrentState: {_currentPlayerState}");
        sb.AppendLine($"PreviousState: {_previousState}");
        sb.AppendLine($"AttackReady L/R: {_isLeftAttackReady} / {_isRightAttackReady}");
        sb.AppendLine($"CanMove/CanRotate: {_canMove} / {_canRotatable}, TimeScale: {Time.timeScale}");
        sb.AppendLine($"WalkSpeed(Total): {stats.TotalStats[EStatType.WalkSpeed].value}");

        foreach (EPartType type in new[] { EPartType.ArmL, EPartType.ArmR })
        {
            PartBase part = inventory.EquippedItems[type][0];
            string armState = part is PartBaseArm arm ? arm.BuildDebugState() : "(PartBaseArm 아님)";
            sb.AppendLine($"{type}: {part.GetType().Name} {armState}");
        }

        if (animator != null)
        {
            string controllerName = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null";
            sb.AppendLine($"Animator Controller: {controllerName} (CurrentAnimType: {_currentAnimType}, ShootAnimType: {_shootAnimType})");
            sb.AppendLine($"Animator Params: isLeftAttack={animator.GetBool("isLeftAttack")}, isRightAttack={animator.GetBool("isRightAttack")}, isDashing={animator.GetBool("isDashing")}, isDead={animator.GetBool("isDead")}");

            for (int layer = 0; layer < animator.layerCount; ++layer)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
                AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layer);
                string clipName = clips.Length > 0 ? clips[0].clip.name : "-";
                sb.AppendLine($"Layer {layer} ({animator.GetLayerName(layer)}): weight={animator.GetLayerWeight(layer):F2}, IsName(Shoot)={info.IsName("Shoot")}, clip={clipName}, normalizedTime={info.normalizedTime:F2}, inTransition={animator.IsInTransition(layer)}");
            }
        }

        if (rigAimController != null)
        {
            sb.AppendLine($"Rig: {rigAimController}");
        }

        return sb.ToString();
    }
    #endregion
}
