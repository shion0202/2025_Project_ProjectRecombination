using Cinemachine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    protected CinemachinePOV pov;
    protected CinemachineBrain brain;
    protected CinemachineBlendDefinition defaultBlend;

    public PlayerController Owner
    {
        get => _owner;
        set => _owner = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _cam = Camera.main;

        _playerActions = new PlayerActions();
        _playerActions.JumpAttackActionMap.SetCallbacks(this);

        if (pov == null)
        {
            var vcam = gameObject.GetComponentInChildren<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                pov = vcam.GetCinemachineComponent<CinemachinePOV>();
            }
        }

        brain = Camera.main.GetComponent<CinemachineBrain>();
        defaultBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.3f);
    }

    private void OnEnable()
    {
        _playerActions.JumpAttackActionMap.Enable();
        _currentTime = time;
    }

    private void Start()
    {
        // 캐릭터가 사라지는 효과
        PlayerInput inputComp = _owner.GetComponent<PlayerInput>();
        if (inputComp != null)
        {
            inputComp.enabled = false;
        }
        _owner.gameObject.SetActive(false);

        GUIManager.Instance.GameUIController.SetLegsSkillTimer(new Color(0.45f, 0.59f, 0.59f));
        GUIManager.Instance.GameUIController.SetLegsSkillIcon(true);
        GUIManager.Instance.GameUIController.SetLegsSkillCooldown(true);
        GUIManager.Instance.GameUIController.RapidInfo.SetActive(true);
    }

    private void OnDisable()
    {
        _playerActions.JumpAttackActionMap.Disable();
    }

    private void Update()
    {
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

        if (pov == null)
        {
            var vcam = gameObject.GetComponentInChildren<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                pov.m_HorizontalAxis.Value = horizontalValue;
            }
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
        PlayerInput inputComp = _owner.GetComponent<PlayerInput>();
        if (inputComp != null)
            inputComp.enabled = false;   // 일단 확실히 OFF

        _owner.Controller.enabled = false;
        _owner.transform.position = transform.position;
        _owner.Controller.enabled = true;
        _originalPart.IsAttack = true;
        brain.m_DefaultBlend = defaultBlend;

        // 1) 스킬 카메라의 "실제 월드 forward" 구하기
        // pov가 붙어 있는 vcam의 Transform 사용
        Transform skillVcamTransform = pov.VirtualCamera.transform;
        Vector3 worldForward = skillVcamTransform.forward;
        float yaw = GetYawFromForward(worldForward);

        // 2) 플레이어 POV에 같은 Yaw를 세팅
        var playerPov = _owner.FollowCamera.CameraAim;
        playerPov.m_HorizontalAxis.Value = Mathf.Clamp(
            yaw,
            playerPov.m_HorizontalAxis.m_MinValue,
            playerPov.m_HorizontalAxis.m_MaxValue
        );

        _owner.gameObject.SetActive(true);
        Utils.Destroy(gameObject);

        // 3) 다음 프레임에 인풋 다시 켜기 (축이 확정된 후)
        if (inputComp != null)
        {
            StartCoroutine(ReEnableInputNextFrame(inputComp));
        }
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
            PlayerInput inputComp = _owner.GetComponent<PlayerInput>();
            if (inputComp != null)
            {
                inputComp.enabled = true;
            }
            _originalPart.IsAttack = false;
            brain.m_DefaultBlend = defaultBlend;

            _owner.gameObject.SetActive(true);
            Utils.Destroy(gameObject);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
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
        // XZ 평면에서의 각도
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        // -180 ~ 180 정규화
        return Mathf.DeltaAngle(0f, yaw);
    }

    private IEnumerator ReEnableInputNextFrame(PlayerInput inputComp)
    {
        // 한 프레임 쉬고
        yield return null;

        if (inputComp != null)
        {
            inputComp.enabled = true;
        }

        Utils.Destroy(gameObject);
    }
}
