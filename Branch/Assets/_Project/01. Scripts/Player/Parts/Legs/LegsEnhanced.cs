using _Project._01._Scripts.Monster;
using Cinemachine;
using Managers;
using Monster.AI;
using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegsEnhanced : PartBaseLegs
{
    [Header("롤러 설정")]
    [SerializeField] private GameObject RapidPlayerPrefab;
    [SerializeField] private GameObject jumpEffectPrefab;
    [SerializeField] private GameObject landingEffectPrefab;
    [SerializeField] private float acceleration = 8f;           // 가속 속도 (높을수록 빠른 출발)
    [SerializeField] private float deceleration = 5f;           // 감속 속도 (높을수록 빠른 멈춤)
    [SerializeField] private float skateSpeed = 3.0f;
    [SerializeField] private float skateSway = 0.25f;
    [SerializeField] private float skateRadius = 30.0f;
    [SerializeField] private float sppedMultiplier = 1.2f;
    private Vector3 _currentVelocity = Vector3.zero;            // 현재 속도 벡터
    private float _skateTime = 0f;                              // 시간 누적 변수
    private bool _isCooldown = false;
    private bool _isAttack = false;
    protected CinemachineImpulseSource source;
    protected AudioSource _audioSource;

    public bool IsAttack
    {
        get => _isAttack;
        set => _isAttack = value;
    }

    protected override void Awake()
    {
        base.Awake();
        _legsAnimType = EAnimationType.Roller;
        source = gameObject.GetComponent<CinemachineImpulseSource>();
        _audioSource = gameObject.GetComponent<AudioSource>();
    }

    protected void OnEnable()
    {
        // 스킬 사용 시와 파츠 교체 시를 구분
        if (_isCooldown)
        {
            JumpAttackFinish();

            _currentCooldown = IsAttack ? (skillCooldown - _owner.Stats.TotalStats[EStatType.CooldownReduction].value) : (skillCooldown - _owner.Stats.TotalStats[EStatType.CooldownReduction].value) * 0.5f;
            _isAttack = false;
            _isCooldown = false;
        }
        else
        {
            _currentSkillCount = 0;
            _currentVelocity = Vector3.zero;
            _skateTime = 0.0f;
            _isCooldown = false;
            _isAttack = false;

            _damagedTargets.Clear();

            if (_skillCoroutine != null)
            {
                StopCoroutine(_skillCoroutine);
                _skillCoroutine = null;
            }
        }

        GUIManager.Instance.SetLegsSkillTimer(Color.white);
        GUIManager.Instance.RapidInfo.SetActive(false);

        _audioSource.volume = 0.0f;
        _audioSource.Play();
    }

    protected void OnDisable()
    {
        if (!_isCooldown)
        {
            _currentSkillCount = 0;
            _currentVelocity = Vector3.zero;
            _skateTime = 0.0f;
            _isCooldown = false;
            _isAttack = false;

            _damagedTargets.Clear();

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
        }

        _audioSource.volume = 1.0f;
        _audioSource.Stop();
    }

    private void Update()
    {
        _currentCooldown -= Time.deltaTime;

        GUIManager.Instance.SetLegsSkillCooldown(_currentCooldown);
        if (_currentCooldown <= 0.0f)
        {
            GUIManager.Instance.SetLegsSkillIcon(false);
            GUIManager.Instance.SetLegsSkillCooldown(false);
        }
    }

    public override void UseAbility()
    {
        if (_cooldownRoutine != null) return;
        JumpAttack();
    }

    public override void FinishActionForced()
    {
        base.FinishActionForced();

        _currentSkillCount = 0;
        _currentVelocity = Vector3.zero;
        _skateTime = 0.0f;
        _isCooldown = false;
        _isAttack = false;

        _damagedTargets.Clear();

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

        GUIManager.Instance.SetLegsSkillTimer(Color.white);
        GUIManager.Instance.RapidInfo.SetActive(false);

        _audioSource.volume = 0.0f;
        _audioSource.Play();
    }

    public override void SetCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;

        _currentCooldown = _owner.CooldownDictionary[currentPartType];

        if (_currentCooldown > 0.0f)
        {
            GUIManager.Instance.SetLegsSkillIcon(true);
            GUIManager.Instance.SetLegsSkillCooldown(true);
            GUIManager.Instance.SetLegsSkillCooldown(_currentCooldown);
        }

        if (!_owner) return;
    }

    protected void JumpAttack()
    {
        if (_currentCooldown > 0.0f || _isCooldown) return;

        // 점프 연출 이후 실행
        Utils.Destroy(
            Utils.Instantiate(jumpEffectPrefab, _owner.transform.position + Vector3.up * 10.0f, Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f))), 1.0f);

        _owner.Inven.EquippedItems[EPartType.Shoulder][0].PreserveCurrentCooldown(EPartType.Shoulder);

        GameObject go = Utils.Instantiate(RapidPlayerPrefab, _owner.transform.position, _owner.transform.rotation);
        RapidPlayer rapidPlayer = go.GetComponent<RapidPlayer>();
        if (rapidPlayer != null)
        {
            _isCooldown = true;
            rapidPlayer.Init(_owner, this, _owner.FollowCamera.CameraAim.m_HorizontalAxis.Value);
        }
    }

    protected void JumpAttackFinish()
    {
        // 쿨타임, 재등장/공격 이펙트, 데미지 판정 등
        GUIManager.Instance.SetLegsSkillIcon(true);
        GUIManager.Instance.SetLegsSkillCooldown(true);

        if (_isAttack)
        {
            Utils.Destroy(
                Utils.Instantiate(jumpEffectPrefab, _owner.transform.position + Vector3.up * -5.0f, Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f))), 1.0f);

            Utils.Destroy(Utils.Instantiate(landingEffectPrefab, _owner.transform.position, Quaternion.identity), 0.5f);
            _owner.FollowCamera.ApplyShake(source);

            Collider[] hitColliders = Physics.OverlapSphere(_owner.transform.position, skillRange, targetMask);
            foreach (Collider hit in hitColliders)
            {
                float hitZoneValue = 1.0f;
                PartialBlow partialBlow = hit.GetComponent<PartialBlow>();
                if (partialBlow)
                {
                    hitZoneValue = partialBlow.fValue;
                }

                IDamagable monster = hit.transform.GetComponent<IDamagable>();
                if (monster != null)
                {
                    Transform otherParent = hit.transform;
                    if (_damagedTargets.Contains(otherParent)) continue;
                    _damagedTargets.Add(otherParent);
                    monster.ApplyDamage(skillDamage * hitZoneValue, targetMask);

                    if (_owner.CompareTag("Player"))
                    {
                        Managers.GUIManager.Instance.StartHitCrosshair();
                    }
                }
                else
                {
                    monster = hit.transform.GetComponentInParent<IDamagable>();
                    if (monster != null)
                    {
                        Transform otherParent = hit.transform.GetComponentInParent<FSM>().transform;
                        if (_damagedTargets.Contains(otherParent)) continue;
                        _damagedTargets.Add(otherParent);
                        monster.ApplyDamage(skillDamage * hitZoneValue, targetMask);

                        if (_owner.CompareTag("Player"))
                        {
                            Managers.GUIManager.Instance.StartHitCrosshair();
                        }
                    }
                }
            }
        }

        _owner.Inven.EquippedItems[EPartType.Shoulder][0].SetCurrentCooldown(EPartType.Shoulder);
        _damagedTargets.Clear();
    }

    public GameObject GetTopParent(GameObject obj)
    {
        Transform current = obj.transform;
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current.gameObject;
    }

    public override Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 inputDirection = camForward * moveInput.y + camRight * moveInput.x;
        inputDirection.Normalize();

        // 캐릭터 전진 방향과 각도 (앞방향)
        float angleForward = Vector3.Angle(camForward, inputDirection);
        // 캐릭터 후진 방향과 각도 (뒷방향)
        float angleBackward = Vector3.Angle(-camForward, inputDirection);

        // 30도 이내면 S자 파형 적용(원하는 각도에 맞게 수정 가능)
        bool isSkateStraight = (angleForward < skateRadius || angleBackward < skateRadius) && moveInput.sqrMagnitude > 0.01f;

        // 입력 값이 없을 경우(즉, 애니메이션 재생이 중지된 경우) S자 파형 초기화
        if (moveInput.sqrMagnitude < 0.01f)
        {
            _skateTime = 0f;
            _audioSource.volume = 0.0f;
        }
        else
        {
            // 애니메이션은 재생 중이되 S자 파형 움직임을 멈추는 경우를 고려하여 Skate Time은 계속 누적
            // 롤러스케이트 S자 파형 - 앞방향 키(Y>0)일 때만 S자 진동 추가 (X 입력시 덜 흔들릴 수 있음)
            _skateTime += Time.deltaTime * skateSpeed;                  // 3.0f: S자 횡진동 속도
            _audioSource.volume = 1.0f;

            if (isSkateStraight)
            {
                // 이동 방향(앞/뒤)에 직교하는 벡터를 얻어 흔들림 적용
                Vector3 moveDir = inputDirection.normalized;
                Vector3 swayAxis = Vector3.Cross(Vector3.up, moveDir).normalized; // 월드 업 벡터 기준 오른쪽

                float sideSway = Mathf.Sin(_skateTime) * skateSway;
                inputDirection += swayAxis * sideSway;
                inputDirection.Normalize();

                Vector3 forwardBackward;
                if (Vector3.Dot(camForward, inputDirection) >= 0)
                {
                    // 전진 방향일 때
                    forwardBackward = camForward;
                }
                else
                {
                    // 후진 방향일 때
                    forwardBackward = -camForward;
                }

                // 각도 기반 보간 계수 (0: 좌우, 1: 전후)
                float lerpT = Mathf.InverseLerp(0f, skateRadius, Mathf.Min(angleForward, angleBackward));

                // 흔들림 방향 보간 (좌우 축과 전후 축 사이)
                Vector3 swayDirection = Vector3.Slerp(camRight, forwardBackward, lerpT);    // 전진 방향에 살짝 카메라 오른쪽 벡터 추가
                inputDirection += (inputDirection + swayDirection * sideSway).normalized;             
                inputDirection.Normalize();
            }
        }

        // 감가속 처리
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // 입력방향으로 점진적 가속 (관성 포함)
            float maxSpeed = _owner.Stats.TotalStats[EStatType.WalkSpeed].value + _owner.Stats.TotalStats[EStatType.AddMoveSpeed].value;
            Vector3 targetVelocity = inputDirection * maxSpeed;
            _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            // 입력 없으면 천천히 감속 (롤러스케이트 특유의 관성)
            _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
        }

        return _currentVelocity;
    }
}
