using Cinemachine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RapidPlayer : MonoBehaviour, PlayerActions.IJumpAttackActionMapActions
{
    [SerializeField] private float speed = 10.0f;
    [SerializeField] private float time = 5.0f;
    private Vector2 _moveInput = Vector2.zero;
    private Rigidbody rb;
    private PlayerActions _playerActions;
    private Camera _cam;
    private float _currentTime = 0.0f;

    private PlayerController _owner;
    private LegsEnhanced _originalPart;

    protected CinemachineVirtualCamera vcam; // 컴포넌트 직접 캐싱을 위해 변경
    protected CinemachinePOV pov;
    protected CinemachineBrain brain;
    protected CinemachineBlendDefinition defaultBlend;

    private bool _isExiting = false;
    private Vector3 _originalPlayerPos; // 취소 시 복귀를 위한 위치 백업

    public PlayerController Owner
    {
        get => _owner;
        set => _owner = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _cam = Camera.main;

        // 기존 인풋 에셋 생성 및 콜백 연결 구조 유지
        _playerActions = new PlayerActions();
        _playerActions.JumpAttackActionMap.SetCallbacks(this);

        if (pov == null)
        {
            vcam = gameObject.GetComponentInChildren<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                pov = vcam.GetCinemachineComponent<CinemachinePOV>();
            }
        }

        brain = Camera.main.GetComponent<CinemachineBrain>();
        defaultBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0.0f);
    }

    private void OnEnable()
    {
        _playerActions.JumpAttackActionMap.Enable();
        _currentTime = time;
    }

    private void Start()
    {
        GUIManager.Instance.GameUIController.SetLegsSkillTimer(new Color(0.45f, 0.59f, 0.59f));
        GUIManager.Instance.GameUIController.SetLegsSkillIcon(true);
        GUIManager.Instance.GameUIController.SetLegsSkillCooldown(true);
        GUIManager.Instance.GameUIController.RapidInfo.SetActive(true);
    }

    private void OnDisable()
    {
        _playerActions.JumpAttackActionMap.Disable();
    }

    private void OnDestroy()
    {
        // 브레인의 기본 블렌드는 씬을 넘어 유지되는 전역 설정이다.
        // Awake에서 Cut으로 바꿔놓고 Apply/OnCancel에서만 되돌리면,
        // 그 두 경로를 타지 않고 파괴될 때(씬 언로드 등) Cut이 그대로 남아
        // 이후 모든 카메라 전환이 즉시 잘리는 상태가 된다.
        // 어떤 경로로 사라지든 반드시 복구되도록 여기서 한 번 더 되돌린다.
        if (brain != null) brain.m_DefaultBlend = defaultBlend;
    }

    private void Update()
    {
        if (_isExiting) return;

        _currentTime -= Time.deltaTime;
        GUIManager.Instance.GameUIController.SetLegsSkillCooldown(_currentTime);
        GUIManager.Instance.GameUIController.SetRapidCooldownText(_currentTime);
        if (_currentTime <= 0.0f)
        {
            Apply();
        }

        Move();
    }

    public void Init(PlayerController owner, LegsEnhanced origin, float horizontalValue)
    {
        _owner = owner;
        _originalPart = origin;
        _originalPlayerPos = _owner.transform.position;

        if (vcam != null && pov == null)
        {
            pov = vcam.GetCinemachineComponent<CinemachinePOV>();
        }
        if (pov != null)
        {
            pov.m_HorizontalAxis.Value = horizontalValue;
        }

        if (_owner != null && _owner.FollowCamera != null && pov != null)
        {
            _owner.FollowCamera.SyncCameraRotation(pov, true);
            _owner.FollowCamera.SetTargetPOV(pov);
        }

        // 몬스터가 지하로 내려간 플레이어를 쫓아 맵 밖으로 나가지 않도록,
        // 스킬이 진행되는 동안에는 착지점(이 오브젝트)을 대신 추적하게 한다.
        // 착지점이 곧 플레이어가 내려올 자리이므로 전투 흐름도 자연스럽다.
        RetargetMonsters(gameObject);

        // 시네머신 브레인이 이 카메라의 존재를 인지하고 화면을 장악한 뒤에 플레이어를 치운다.
        // 같은 프레임에 치우면 브레인이 아직 이전 카메라를 물고 있어,
        // 플레이어를 따라가던 카메라가 한순간 맵 밖(지하)을 비춘다.
        StartCoroutine(TeleportOwnerToUndergroundNextFrame());
    }

    private IEnumerator TeleportOwnerToUndergroundNextFrame()
    {
        yield return null;

        // 대기 중에 스킬이 끝났다면(즉시 취소 등) 플레이어를 치우면 안 된다.
        if (_isExiting || _owner == null) yield break;

        _owner.Controller.enabled = false;
        _owner.transform.position = Vector3.down * 9999f;
        _owner.Controller.enabled = true;
    }

    /// 씬에 있는 모든 몬스터의 추적 대상을 교체한다.
    /// 보스(아몬 2페이즈)는 AIController가 없어 MonsterManager 목록에 등록되지 않으므로
    /// 매니저를 경유하지 않고 Blackboard를 직접 찾는다.
    /// 스킬 시작/종료 시 한 번씩만 호출되므로 탐색 비용은 문제되지 않는다.
    private static void RetargetMonsters(GameObject newTarget)
    {
        if (newTarget == null) return;

        foreach (Monster.AI.Blackboard.Blackboard blackboard in FindObjectsOfType<Monster.AI.Blackboard.Blackboard>())
        {
            blackboard.SetTarget(newTarget);
        }
    }

    private void Move()
    {
        if (_moveInput == Vector2.zero)
        {
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(0f, currentVelocity.y, 0f);
            return;
        }

        Vector3 camForward = -_cam.transform.forward;
        Vector3 camRight = _cam.transform.right;
        camForward.y = 0.0f;
        camRight.y = 0.0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * -_moveInput.y + camRight * _moveInput.x;
        rb.velocity = new Vector3(moveDirection.normalized.x * speed, rb.velocity.y, moveDirection.normalized.z * speed);
    }

    private void Apply()
    {
        if (_isExiting) return;
        _isExiting = true;

        // 지하에 있던 플레이어를 지상(현재 스킬 최종 위치)으로 먼저 소환.
        // 좌표를 그대로 대입하면 캡슐이 바닥에 걸쳐 착지 순간 위아래로 튀므로,
        // 지면에 발을 맞춰 배치하고 낙하 속도까지 정리하는 TeleportGrounded를 쓴다.
        _owner.TeleportGrounded(transform.position);

        // 몬스터의 추적 대상을 플레이어로 되돌린다.
        RetargetMonsters(_owner.gameObject);

        _originalPart.IsAttack = true;
        RestoreCameraAngle();

        // 주도권을 돌려주기 전, 플레이어 카메라 내부의 지하 잔상 데이터를 완벽하게 워프 세탁합니다.
        // 복귀하기 전에 제어 대상을 다시 원래 플레이어 POV로 원상복구
        if (_owner.FollowCamera != null)
        {
            _owner.FollowCamera.SyncCameraRotation(pov, false);
            _owner.FollowCamera.SetTargetPOV(null);
            _owner.FollowCamera.WarpToTarget();
        }

        // 플레이어가 완전히 지상에 올라온 것을 확인한 뒤 우선순위를 원복하여 카메라 블렌딩 유도
        if (vcam != null) vcam.Priority = 0;

        _originalPart.OnJumpAttackWindowClosed();

        brain.m_DefaultBlend = defaultBlend;
        Utils.Destroy(gameObject);
    }

    public void OnApply(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Apply();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Cancel();
        }
    }

    /// <summary>
    /// 스킬을 취소하고 플레이어·몬스터·카메라를 시전 전 상태로 되돌린다.
    /// 플레이어의 취소 입력뿐 아니라 컷씬 진입이나 사망처럼 밖에서 강제로 끝낼 때도 이 경로를 쓴다.
    /// 이 정리를 건너뛰고 오브젝트만 남으면 플레이어가 지하에 남고, 스킬 카메라가 우선순위를 쥔 채
    /// brain의 기본 블렌드가 Cut으로 고정되어 이후 모든 카메라 전환이 즉시 잘린다.
    /// </summary>
    public void Cancel()
    {
        if (_isExiting) return;
        _isExiting = true;

        // 1) 취소 시 플레이어를 원래 시전했던 제자리로 먼저 소환
        _owner.TeleportGrounded(_originalPlayerPos);

        // 몬스터의 추적 대상을 플레이어로 되돌린다.
        RetargetMonsters(_owner.gameObject);

        _originalPart.IsAttack = false;
        RestoreCameraAngle();

        if (_owner.FollowCamera != null)
        {
            _owner.FollowCamera.SetTargetPOV(null);
            _owner.FollowCamera.WarpToTarget();
        }

        // 2) 플레이어 지상 안착 후 카메라 주도권 반환
        if (vcam != null) vcam.Priority = 0;

        _originalPart.OnJumpAttackWindowClosed();

        brain.m_DefaultBlend = defaultBlend;
        Utils.Destroy(gameObject);
    }

    private void RestoreCameraAngle()
    {
        if (pov != null && _owner.FollowCamera != null)
        {
            Transform skillVcamTransform = pov.VirtualCamera.transform;
            Vector3 worldForward = skillVcamTransform.forward;
            float yaw = GetYawFromForward(worldForward);

            var playerPov = _owner.FollowCamera.CameraAim;
            playerPov.m_HorizontalAxis.Value = Mathf.Clamp(
                yaw,
                playerPov.m_HorizontalAxis.m_MinValue,
                playerPov.m_HorizontalAxis.m_MaxValue
            );
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isExiting) return;

        if (context.canceled)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    float GetYawFromForward(Vector3 forward)
    {
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        return Mathf.DeltaAngle(0f, yaw);
    }
}
