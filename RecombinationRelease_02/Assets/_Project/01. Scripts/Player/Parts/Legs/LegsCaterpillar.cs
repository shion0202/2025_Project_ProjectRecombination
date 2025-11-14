using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using Cinemachine;

public class LegsCaterpillar : PartBaseLegs
{
    [Header("캐터필러 설정")]
    [SerializeField] protected GameObject impactEffectPrefab;
    [SerializeField] protected Material caterpillarMaterial;
    [SerializeField] private float turnMoveSpeed = 120.0f;
    [SerializeField] private float turnRotateSpeed = 10.0f;
    [SerializeField] protected float backwardThreshold = -0.7f;
    [SerializeField] protected Vector2 animSpeed = Vector2.zero;
    private Vector3 _currentMoveDirection = Vector3.forward;
    private bool _isBackward = false;
    private Quaternion _originalRotation;
    protected CinemachineImpulseSource source;
    private bool _isSiegeMode = false;
    private bool _isCooldown = false;
    protected AudioSource _audioSource;

    protected override void Awake()
    {
        base.Awake();
        _legsAnimType = EAnimationType.Caterpillar;
        _isAnimating = false;
        _isSiegeMode = false;
        source = gameObject.GetComponent<CinemachineImpulseSource>();
        _audioSource = gameObject.GetComponent<AudioSource>();
    }

    protected void OnEnable()
    {
        if (_owner != null)
        {
            _currentMoveDirection = _owner.transform.forward;
        }

        _audioSource.volume = 0.0f;
        _audioSource.Play();
    }

    protected void OnDisable()
    {
        _currentSkillCount = 0;
        _owner.SetMovable(true);

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        if (Managers.GUIManager.IsAliveInstance())
        {
            GUIManager.Instance.SetLegsSkillIcon(false);
            GUIManager.Instance.SetLegsSkillCooldown(0.0f);
            GUIManager.Instance.SetLegsSkillCooldown(false);
        }

        _audioSource.volume = 1.0f;
        _audioSource.Stop();
    }

    public override void UseAbility()
    {
        Impact();
    }

    public override Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        if (moveInput == Vector2.zero)
        {
            if (caterpillarMaterial != null)
            {
                caterpillarMaterial.SetVector("_AnimSpeed", Vector2.zero);
            }

            _audioSource.volume = 0.0f;

            return Vector3.zero;
        }

        // 카메라 기준 목표 이동 방향 설정
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        Debug.DrawRay(_owner.transform.position + Vector3.up, targetDirection * 5.0f, Color.red);
        Debug.DrawRay(_owner.transform.position + Vector3.up, transform.TransformDirection(-transform.forward) * 5.0f, Color.green);

        _audioSource.volume = 1.0f;

        // 현재 하체(캐릭터) 정면 방향과 목표 방향 각도(dot) 계산
        // 후진 모드 전환: dot가 threshold(예: -0.7) 이하이면 true로, threshold 이상이면 false로 딱 한 번만 전환
        float dot = Vector3.Dot(transform.TransformDirection(-transform.forward), targetDirection);
        if (!_isBackward && dot < backwardThreshold)
        {
            _isBackward = true; // 후진 모드 진입
            _currentMoveDirection = -_currentMoveDirection; // 딱 한 번만 뒤집어줌
        }
        else if (_isBackward && dot > -backwardThreshold)
        {
            _isBackward = false; // 전진 모드 복귀
            _currentMoveDirection = -_currentMoveDirection; // 다시 전진
        }

        // 현재 하체 방향에서 목표 방향까지 서서히 lerp (캐터필러 느낌)
        // 후방이라면 _currentMoveDirection이 반대로 뒤집혀야함
        _currentMoveDirection = Vector3.RotateTowards(
            _currentMoveDirection,
            targetDirection,
            turnMoveSpeed * Mathf.Deg2Rad * Time.deltaTime,
            0f);

        HandleCylinderDirection(_currentMoveDirection);

        if (caterpillarMaterial != null)
        {
            caterpillarMaterial.SetVector("_AnimSpeed", animSpeed * (_isBackward ? -1 : 1));
        }

        return _currentMoveDirection * (_owner.Stats.TotalStats[EStatType.WalkSpeed].value + _owner.Stats.TotalStats[EStatType.AddMoveSpeed].value); // 좌우가 서서히 꺾이는 이동방향
    }

    protected void Impact()
    {
        if (_isCooldown) return;

        if (!_isSiegeMode)
        {
            // 시즈 모드 진입 애니메이션 재생
            // 일정 시간 대기
            if (_skillCoroutine != null)
            {
                StopCoroutine(_skillCoroutine);
                _skillCoroutine = null;
            }
            _skillCoroutine = StartCoroutine(CoPlaySiegeMode(true));
            _isSiegeMode = true;
        }
        else
        {
            // 시즈 모드 해제 애니메이션 재생
            if (_skillCoroutine != null)
            {
                StopCoroutine(_skillCoroutine);
                _skillCoroutine = null;
            }
            _skillCoroutine = StartCoroutine(CoPlaySiegeMode(false));
        }
    }

    public void HandleCylinderDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        Quaternion targetRot;

        // 후진 상태일 때는 moveDirection이 뒤집혀야 함
        if (_isBackward)
        {
            // 후진: 정면이 오브젝트의 forward(후면)이 moveDirection을 바라보게
            targetRot = Quaternion.LookRotation(-moveDirection, Vector3.up);
        }
        else
        {
            // 전진: 정면이 오브젝트의 -forward(정면)이 moveDirection을 바라보게
            targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        // x축 -90도 회전을 곱해서 보정용 회전 생성
        Quaternion fix = Quaternion.Euler(-90f, 0f, 0f);
        targetRot *= fix;

        // 현재 회전과 보간해서 부드럽게 회전 처리
        gameObject.transform.rotation = Quaternion.Slerp(
            gameObject.transform.rotation,
            targetRot,
            Time.deltaTime * turnRotateSpeed);
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);

        _currentMoveDirection = _owner.transform.forward;
    }

    protected IEnumerator CoPlaySiegeMode(bool isActivate)
    {
        if (isActivate)
        {
            LookCameraDirection();

            _owner.PlayerAnimator.SetBool("isPlayLegsAnim", true);
            _owner.SetPlayerState(EPlayerState.Skilling, true);
            GUIManager.Instance.SetLegsSkillIcon(true);
            _owner.SetMovable(false);
            _owner.FollowCamera.SetCameraRotatable(false);
            yield return new WaitForSeconds(2.5f);

            _owner.FollowCamera.SetCameraRotatable(true);
            _owner.SetMovable(false, true);
        }
        else
        {
            _isCooldown = true;
            _owner.PlayerAnimator.SetBool("isPlayLegsAnim", false);
            _owner.SetMovable(false);
            yield return new WaitForSeconds(2.5f);

            _currentMoveDirection = _owner.transform.forward;
            _owner.SetPlayerState(EPlayerState.Skilling, false);
            _owner.SetMovable(true);
            _owner.FollowCamera.SetCameraRotatable(true);
            _isSiegeMode = false;

            // 스킬 쿨타임
            float time = 5.0f;
            GUIManager.Instance.SetLegsSkillCooldown(true);
            GUIManager.Instance.SetLegsSkillCooldown(time);
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                time -= 0.1f;
                GUIManager.Instance.SetLegsSkillCooldown(time);
                if (time <= 0.0f)
                {
                    break;
                }
            }

            GUIManager.Instance.SetLegsSkillIcon(false);
            GUIManager.Instance.SetLegsSkillCooldown(false);
            _isCooldown = false;
            Debug.Log("쿨타임 종료");
        }

        _skillCoroutine = null;
    }

    protected IEnumerator CoImpartRoutine()
    {
        GUIManager.Instance.SetLegsSkillIcon(true);
        _owner.PlayerAnimator.SetBool("isPlayLegsAnim", true);
        yield return new WaitForSeconds(3.0f);

        _owner.PlayerAnimator.SetTrigger("heavyShootTrigger");
        _owner.FollowCamera.ApplyShake(source);
        Destroy(Instantiate(impactEffectPrefab, _owner.transform.position, Quaternion.Euler(_owner.transform.rotation.eulerAngles + new Vector3(-90.0f, 0.0f, 0.0f))), 5.0f);

        // 이동 불가 적용
        _owner.SetMovable(false, true);
        Debug.Log("캐터필러 스킬 효과: 이동 불가");
        yield return new WaitForSeconds(skillDuration);

        _owner.PlayerAnimator.SetBool("isPlayLegsAnim", false);
        yield return new WaitForSeconds(2.0f);

        _owner.SetMovable(true);                            // skillDuration 끝나면 다시 이동 가능
        Debug.Log("캐터필러 스킬 효과: 이동 불가 해제");

        // 쿨타임 시작
        float time = (skillCooldown - _owner.Stats.TotalStats[EStatType.CooldownReduction].value);
        GUIManager.Instance.SetLegsSkillCooldown(true);
        GUIManager.Instance.SetLegsSkillCooldown(time);
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            time -= 0.1f;
            GUIManager.Instance.SetLegsSkillCooldown(time);
            if (time <= 0.0f)
            {
                break;
            }
        }

        GUIManager.Instance.SetLegsSkillIcon(false);
        GUIManager.Instance.SetLegsSkillCooldown(false);
        _skillCoroutine = null;
        Debug.Log("캐터필러 스킬 쿨타임 종료");
    }
}
