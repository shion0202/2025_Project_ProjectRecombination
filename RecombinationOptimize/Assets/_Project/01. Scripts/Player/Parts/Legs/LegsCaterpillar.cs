using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using Cinemachine;
using _Project._01._Scripts.Monster;
using Monster.AI.FSM;

public class LegsCaterpillar : PartBaseLegs
{
    [Header("캐터필러 설정")]
    [SerializeField] protected GameObject subWheelObject;
    [SerializeField] protected GameObject impactEffectPrefab;
    [SerializeField] protected Material caterpillarMaterial;
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected GameObject hitEffectPrefab;
    [SerializeField] private float turnMoveSpeed = 120.0f;
    [SerializeField] private float turnRotateSpeed = 10.0f;
    [SerializeField] protected float backwardThreshold = -0.7f;
    [SerializeField] protected Vector2 animSpeed = Vector2.zero;
    [SerializeField] protected Vector2 shootOffset = Vector2.zero;
    private Vector3 _currentMoveDirection = Vector3.forward;
    private bool _isBackward = false;
    private Quaternion _originalRotation;
    protected CinemachineImpulseSource source;
    private bool _isCooldown = false;
    protected AudioSource _audioSource;

    protected override void Awake()
    {
        base.Awake();

        _legsAnimType = EAnimationType.Caterpillar;
        _isAnimating = false;
        source = gameObject.GetComponent<CinemachineImpulseSource>();
        _audioSource = gameObject.GetComponent<AudioSource>();

        _partModifiers.Add(new StatModifier(EStatType.IntervalBetweenShots, EStackType.PercentMul, -0.5f, this));
        _partModifiers.Add(new StatModifier(EStatType.DamageReductionRate, EStackType.PercentMul, 0.5f, this));
    }

    protected void OnEnable()
    {
        if (_owner != null)
        {
            _currentMoveDirection = _owner.transform.forward;
        }

        if (subWheelObject)
        {
            subWheelObject.SetActive(true);
        }

        _audioSource.volume = 0.0f;
        _audioSource.Play();

        _owner.Stats.RemoveModifier(this);
        _damagedTargets.Clear();
    }

    protected void OnDisable()
    {
        _currentSkillCount = 0;
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayLegsAnim", false);
        _owner.SetPlayerState(EPlayerState.Nuking, false);
        GUIManager.Instance.SetLegsSkillIcon(false);
        _owner.FollowCamera.SetCameraRotatable(true);
        _isCooldown = false;

        _audioSource.volume = 1.0f;
        _audioSource.Stop();

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

        _owner.Stats.RemoveModifier(this);
        _damagedTargets.Clear();
    }

    public override void UseAbility()
    {
        if (_cooldownRoutine != null) return;
        Impact(true);
    }

    public override void FinishActionForced()
    {
        base.FinishActionForced();

        _currentMoveDirection = _owner.transform.forward;
        _audioSource.volume = 0.0f;
        _audioSource.Play();

        _currentSkillCount = 0;
        _owner.SetMovable(true);
        _owner.PlayerAnimator.SetBool("isPlayLegsAnim", false);
        _owner.SetPlayerState(EPlayerState.Nuking, false);
        GUIManager.Instance.SetLegsSkillIcon(false);
        _owner.FollowCamera.SetCameraRotatable(true);
        _isCooldown = false;

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

        if (subWheelObject)
        {
            subWheelObject.SetActive(true);
        }

        _owner.Stats.RemoveModifier(this);
        _damagedTargets.Clear();
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

    protected void Impact(bool isOn)
    {
        if (_isCooldown) return;

        if (isOn)
        {
            // 시즈 모드 진입 애니메이션 재생
            // 일정 시간 대기
            if (_skillCoroutine == null)
            {
                _skillCoroutine = StartCoroutine(CoPlaySiegeMode(true));
            }
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

            if (subWheelObject)
            {
                subWheelObject.SetActive(false);
            }

            _owner.PlayerAnimator.SetBool("isPlayLegsAnim", true);
            _owner.SetPlayerState(EPlayerState.Nuking, true);
            GUIManager.Instance.SetLegsSkillIcon(true);
            _owner.SetMovable(false);
            _owner.FollowCamera.SetCameraRotatable(false);
            yield return new WaitForSeconds(2.0f);

            _owner.FollowCamera.SetCameraRotatable(true);
            _owner.SetMovable(false, true);

            _owner.Stats.AddModifier(_partModifiers);

            // N초간 바라보는 방향으로 누킹 딜
            float time = skillDuration;
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                // 공격 로직
                Vector3 startPoint = _owner.transform.position + _owner.transform.forward * shootOffset.x + _owner.transform.up * shootOffset.y;
                Camera cam = Camera.main;
                float maxDistance = skillRange;
                float radius = 10.0f;
                Vector3 targetDirection = _owner.transform.forward;
                Collider[] hitResults = new Collider[1000];

                Utils.Destroy(
                    Utils.Instantiate(
                        impactEffectPrefab,
                        startPoint,
                        Quaternion.LookRotation(-_owner.transform.forward)),
                    1.0f
                    );
                _owner.FollowCamera.ApplyShake(source);

                // 적 데미지 처리
                int hitCount = Physics.OverlapCapsuleNonAlloc(
                    startPoint,
                    startPoint + targetDirection * maxDistance,
                    radius,
                    hitResults,
                    targetMask
                );
                for (int i = 0; i < hitCount; i++)
                {
                    var collider = hitResults[i];

                    IDamagable enemy = collider.transform.GetComponent<IDamagable>();
                    if (enemy != null)
                    {
                        Transform otherParent = collider.transform;
                        if (_damagedTargets.Contains(otherParent)) continue;
                        _damagedTargets.Add(otherParent);
                        enemy.ApplyDamage(skillDamage, targetMask);

                        Utils.Destroy(Utils.Instantiate(hitEffectPrefab, otherParent.transform.position + Vector3.up * 1.0f, Quaternion.identity), 1.0f);

                        if (_owner.CompareTag("Player"))
                        {
                            Managers.GUIManager.Instance.StartHitCrosshair();
                        }
                    }
                    else
                    {
                        enemy = collider.transform.GetComponentInParent<IDamagable>();
                        if (enemy != null)
                        {
                            FSM fsm = collider.transform.GetComponentInParent<FSM>();
                            if (fsm)
                            {
                                Transform otherParent = fsm.transform;
                                if (_damagedTargets.Contains(otherParent)) continue;
                                _damagedTargets.Add(otherParent);
                                enemy.ApplyDamage(skillDamage, targetMask);

                                Utils.Destroy(Utils.Instantiate(hitEffectPrefab, otherParent.transform.position + Vector3.up * 1.0f, Quaternion.identity), 1.0f);

                                if (_owner.CompareTag("Player"))
                                {
                                    Managers.GUIManager.Instance.StartHitCrosshair();
                                }
                            }
                        }
                    }

                    Utils.Destroy(Utils.Instantiate(bulletPrefab, collider.transform.position, Quaternion.identity), 0.1f);
                }

                _damagedTargets.Clear();

                time -= 1.0f;
                if (time <= 0.0f)
                {
                    break;
                }
            }

            Impact(false);
        }
        else
        {
            _isCooldown = true;
            _owner.PlayerAnimator.SetBool("isPlayLegsAnim", false);
            _owner.SetMovable(false);
            yield return new WaitForSeconds(2.0f);

            _owner.Stats.RemoveModifier(this);

            if (subWheelObject)
            {
                subWheelObject.SetActive(true);
            }

            _damagedTargets.Clear();

            _currentMoveDirection = _owner.transform.forward;
            _owner.SetPlayerState(EPlayerState.Nuking, false);
            _owner.SetMovable(true);
            _owner.FollowCamera.SetCameraRotatable(true);

            // 스킬 쿨타임
            _currentCooldown = skillCooldown;
            GUIManager.Instance.SetLegsSkillCooldown(true);
            GUIManager.Instance.SetLegsSkillCooldown(_currentCooldown);
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                _currentCooldown -= 0.1f;
                GUIManager.Instance.SetLegsSkillCooldown(_currentCooldown);
                if (_currentCooldown <= 0.0f)
                {
                    _currentCooldown = 0.0f;
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

    void OnDrawGizmosSelected()
    {
        if (_owner == null)
            return;

        Vector3 startPoint = _owner.transform.position + _owner.transform.forward * shootOffset.x + _owner.transform.up * shootOffset.y;
        Camera cam = Camera.main;
        float maxDistance = 20.0f;
        float radius = 10.0f;
        Vector3 targetDirection = _owner.transform.forward;

        Vector3 point1 = _owner.transform.position;
        Vector3 point2 = _owner.transform.position + targetDirection.normalized * maxDistance;

        Gizmos.color = Color.red;

        // 캡슐 끝단 두 개의 구 그리기
        Gizmos.DrawWireSphere(point1, radius);
        Gizmos.DrawWireSphere(point2, radius);

        // 두 구를 연결하는 선을 원통처럼 보이게 라인으로 표시 
        DrawCapsuleLines(point1, point2, radius);
    }

    // 캡슐 형태의 옆선을 그리는 헬퍼 함수
    void DrawCapsuleLines(Vector3 start, Vector3 end, float radius)
    {
        // start와 end를 잇는 방향 벡터
        Vector3 direction = (end - start).normalized;

        // 임의의 벡터를 사용해 캡슐 원주 부분을 그리기 위한 기준 벡터 3개 선택
        Vector3 up = Vector3.up * radius;
        Vector3 right = Vector3.right * radius;
        Vector3 forward = Vector3.forward * radius;

        // 각 벡터를 start와 end 둘 다 더하고 빼서 라인 그리기
        Gizmos.DrawLine(start + up, end + up);
        Gizmos.DrawLine(start - up, end - up);

        Gizmos.DrawLine(start + right, end + right);
        Gizmos.DrawLine(start - right, end - right);

        Gizmos.DrawLine(start + forward, end + forward);
        Gizmos.DrawLine(start - forward, end - forward);
    }
}
