using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Monster;
using UnityEngine.InputSystem;
using Monster.AI;
using Managers;

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
    private float _currentCooldownTime = 0.0f;

    protected override void Awake()
    {
        base.Awake();
        _legsAnimType = EAnimationType.Roller;
    }

    protected void OnEnable()
    {
        if (_isCooldown)
        {
            JumpAttackFinish();
        }
    }

    private void Update()
    {
        if (_currentCooldownTime > 0.0f)
        {
            _currentCooldownTime -= Time.deltaTime;
            GUIManager.Instance.SetLegsSkillCooldown(_currentCooldownTime);
            if (_currentCooldownTime <= 0.0f)
            {
                GUIManager.Instance.SetLegsSkillIcon(false);
                GUIManager.Instance.SetLegsSkillCooldown(false);
            }
        }
    }

    public override void UseAbility()
    {
        JumpAttack();
    }

    protected void JumpAttack()
    {
        if (_currentCooldownTime > 0.0f || _isCooldown) return;

        // 점프 연출 이후 실행
        Instantiate(jumpEffectPrefab, _owner.transform.position + Vector3.up * 2.0f, Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f)));

        GameObject go = Instantiate(RapidPlayerPrefab, _owner.transform.position, Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f)));
        RapidPlayer rapidPlayer = go.GetComponent<RapidPlayer>();
        if (rapidPlayer != null)
        {
            _isCooldown = true;
            rapidPlayer.Init(_owner, _owner.FollowCamera);
        }
    }

    protected void JumpAttackFinish()
    {
        // 쿨타임, 재등장/공격 이펙트, 데미지 판정 등
        GUIManager.Instance.SetLegsSkillIcon(true);
        GUIManager.Instance.SetLegsSkillCooldown(true);
        Destroy(Instantiate(landingEffectPrefab, _owner.transform.position, Quaternion.identity), 0.5f);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, skillRange);
        foreach (Collider hit in hitColliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(10.0f, transform.position, skillRange);
            }

            IDamagable monster = hit.transform.GetComponent<IDamagable>();
            if (monster != null)
            {
                monster.ApplyDamage(skillDamage);
            }
            else
            {
                monster = hit.transform.GetComponentInParent<IDamagable>();
                if (monster != null)
                {
                    monster.ApplyDamage(skillDamage);
                }
            }
        }

        // To-do: 현재는 쿨타임이 동일하지만, 취소할 경우 더 적은 쿨타임을 적용할 필요가 있음
        _currentCooldownTime = skillCooldown - _owner.Stats.TotalStats[EStatType.CooldownReduction].value;
        _isCooldown = false;
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
        }
        else
        {
            // 애니메이션은 재생 중이되 S자 파형 움직임을 멈추는 경우를 고려하여 Skate Time은 계속 누적
            // 롤러스케이트 S자 파형 - 앞방향 키(Y>0)일 때만 S자 진동 추가 (X 입력시 덜 흔들릴 수 있음)
            _skateTime += Time.deltaTime * skateSpeed;                  // 3.0f: S자 횡진동 속도

            if (isSkateStraight)
            {
                float sideSway = Mathf.Sin(_skateTime) * skateSway;     // 0.25f: 진폭(좌우 흔들림 크기)
                
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
