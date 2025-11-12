using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RapidPlayer : MonoBehaviour, PlayerActions.IJumpAttackActionMapActions
{
    [SerializeField] private float speed = 10.0f;
    private Vector2 _moveInput = Vector2.zero;
    private Rigidbody rb;
    private PlayerActions _playerActions;

    private PlayerController _owner;
    private FollowCameraController _camera;

    public PlayerController Owner
    {
        get => _owner;
        set => _owner = value;
    }

    public FollowCameraController FollowCamera
    {
        get => _camera;
        set => _camera = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        _playerActions = new PlayerActions();
        _playerActions.JumpAttackActionMap.SetCallbacks(this);
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
        _camera.InitFollowCamera(gameObject);
    }

    private void OnDisable()
    {
        _playerActions.JumpAttackActionMap.Disable();
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        if (_moveInput == Vector2.zero)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        Vector3 camForward = _camera.transform.forward;
        Vector3 camRight = _camera.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * _moveInput.y + camRight * _moveInput.x;
        rb.velocity = moveDirection.normalized * speed;
    }

    public void Init(PlayerController owner, FollowCameraController followCamera)
    {
        _owner = owner;
        _camera = followCamera;
    }

    public void OnApply(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            //_owner.gameObject.SetActive(true);
            PlayerInput inputComp = _owner.GetComponent<PlayerInput>();
            if (inputComp != null)
            {
                inputComp.enabled = true;
            }
            _camera.InitFollowCamera(_owner.gameObject);
            _owner.Controller.enabled = false;
            _owner.transform.position = transform.position;
            _owner.Controller.enabled = true;

            _owner.gameObject.SetActive(true);
            Destroy(gameObject);
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _owner.gameObject.SetActive(true);
            PlayerInput inputComp = _owner.GetComponent<PlayerInput>();
            if (inputComp != null)
            {
                inputComp.enabled = true;
            }
            _camera.InitFollowCamera(_owner.gameObject);

            Destroy(gameObject);
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
