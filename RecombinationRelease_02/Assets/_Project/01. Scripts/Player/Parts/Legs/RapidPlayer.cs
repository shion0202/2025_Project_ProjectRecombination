using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RapidPlayer : MonoBehaviour, PlayerActions.IJumpAttackActionMapActions
{
    [SerializeField] private float speed = 10.0f;
    private Vector2 _moveInput = Vector2.zero;
    private Rigidbody rb;
    private PlayerActions _playerActions;

    private PlayerController _owner;
    private LegsEnhanced _originalPart;

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

        _playerActions = new PlayerActions();
        _playerActions.JumpAttackActionMap.SetCallbacks(this);

        brain = Camera.main.GetComponent<CinemachineBrain>();
        defaultBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.3f);
    }

    private void OnEnable()
    {
        _playerActions.JumpAttackActionMap.Enable();
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
    }

    private void OnDisable()
    {
        _playerActions.JumpAttackActionMap.Disable();
    }

    private void Update()
    {
        Move();
    }

    public void Init(PlayerController owner, LegsEnhanced origin)
    {
        _owner = owner;
        _originalPart = origin;
    }

    private void Move()
    {
        if (_moveInput == Vector2.zero)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        Vector3 moveDirection = transform.up * -_moveInput.y + transform.right * _moveInput.x;
        rb.velocity = moveDirection.normalized * speed;
    }

    public void OnApply(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            PlayerInput inputComp = _owner.GetComponent<PlayerInput>();
            if (inputComp != null)
            {
                inputComp.enabled = true;
            }
            _owner.Controller.enabled = false;
            _owner.transform.position = transform.position;
            _owner.Controller.enabled = true;
            _originalPart.IsAttack = true;
            brain.m_DefaultBlend = defaultBlend;

            _owner.gameObject.SetActive(true);
            Utils.Destroy(gameObject);
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
}
