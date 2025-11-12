using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

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

    protected override void Awake()
    {
        base.Awake();
        _legsAnimType = EAnimationType.Caterpillar;
        _isAnimating = false;
    }

    protected void OnEnable()
    {
        if (_owner != null)
        {
            _currentMoveDirection = _owner.transform.forward;
        }
    }

    protected void OnDrawGizmos()
    {
        if (_owner == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_owner.transform.position, skillRange);
    }

    public override void UseAbility()
    {
        Impact();
    }

    public override void FinishActionForced()
    {
        base.FinishActionForced();
        _owner.SetMovable(true);
    }

    public override Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        if (moveInput == Vector2.zero)
        {
            if (caterpillarMaterial != null)
            {
                caterpillarMaterial.SetVector("_AnimSpeed", Vector2.zero);
            }

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
        if (_skillCoroutine != null) return;
        _skillCoroutine = StartCoroutine(CoImpartRoutine());
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

    protected IEnumerator CoImpartRoutine()
    {
        GUIManager.Instance.SetLegsSkillIcon(true);
        Destroy(Instantiate(impactEffectPrefab, _owner.transform.position, Quaternion.Euler(_owner.transform.rotation.eulerAngles + new Vector3(-90.0f, 0.0f, 0.0f))), 5.0f);

        // 적 탐지 및 데미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, skillRange);
        foreach (Collider hit in hitColliders)
        {
            IDamagable monster = hit.transform.GetComponent<IDamagable>();
            if (monster != null)
            {
                monster.ApplyDamage(skillDamage);
                // TODO: 적 기절 (2초)
            }
            else
            {
                monster = hit.transform.GetComponentInParent<IDamagable>();
                if (monster != null)
                {
                    monster.ApplyDamage(skillDamage);
                    // TODO: 적 기절 (2초)
                }
            }
        }

        // 파츠 HP 모두 회복
        _owner.HealHp(100.0f, EHealType.Percentage, EHealRange.Part);
        Debug.Log("캐터필러 스킬 효과: 파츠 Hp 모두 회복");

        // 이동 불가 적용
        _owner.SetMovable(false);
        Debug.Log("캐터필러 스킬 효과: 이동 불가");
        yield return new WaitForSeconds(skillDuration);
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
