using _Project.Scripts.VisualScripting;
using Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class LegsHover : PartBaseLegs
{
    [Header("호버링 설정")]
    [SerializeField] protected GameObject barrierPrefab;        // 보호막 프리팹
    [SerializeField] protected Transform barrierSpawnPoint;     // 보호막 생성 위치
    [SerializeField] protected float acceleration = 10f;        // 클수록 즉각적, 작을수록 둔함
    [SerializeField] protected float deceleration = 14f;        // 클수록 급브레이크, 작을수록 천천히 멈춤
    [SerializeField] protected float hoverHeight = 1.5f;
    [SerializeField] protected float hoverRange = 0.2f;
    [SerializeField] protected float hoverSpeed = 2.0f;
    [SerializeField] protected float hoverDownSpeed = 4.0f;
    [SerializeField] protected LayerMask groundLayer;
    protected GameObject _currentBarrier = null;                // 현재 활성화된 보호막
    protected Vector3 _currentMoveDirection = Vector3.zero;
    protected float groundY = 0.0f;
    protected bool isInit = false;

    [SerializeField] protected float largeGroundYChangeThreshold = 0.3f;    // 큰 높이 변화 임계값
    protected float previousGroundY = 0f;

    protected override void Awake()
    {
        base.Awake();

        _partModifiers.Add(new 
            (EStatType.WalkSpeed, EStackType.PercentMul, 0.7f, this));
        _partModifiers.Add(new StatModifier(EStatType.DamageReductionRate, EStackType.PercentMul, 0.7f, this));

        _legsAnimType = EAnimationType.Hover;
    }

    protected void OnEnable()
    {
        _currentMoveDirection = Vector3.zero;
        previousGroundY = groundY;
        isInit = false;

        if (_currentBarrier != null)
        {
            Utils.Destroy(_currentBarrier);
            _currentBarrier = null;
        }
        _owner.Stats.RemoveModifier(this);

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        GUIManager.Instance.SetLegsSkillIcon(false);
        GUIManager.Instance.SetLegsSkillCooldown(0.0f);
        GUIManager.Instance.SetLegsSkillCooldown(false);
    }

    protected void OnDisable()
    {
        _currentMoveDirection = Vector3.zero;
        isInit = false;
        _currentSkillCount = 0;

        if (_currentBarrier != null)
        {
            Utils.Destroy(_currentBarrier);
            _currentBarrier = null;
        }
        _owner.Stats.RemoveModifier(this);

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

    public override void UseAbility()
    {
        CreateBarrier();
    }

    public override Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        // 카메라 기준으로 입력 방향 구하기
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        Vector3 inputDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        float moveSpeed = (_owner.Stats.TotalStats[EStatType.WalkSpeed].value + _owner.Stats.TotalStats[EStatType.AddMoveSpeed].value);
        Vector3 targetVelocity = inputDir * moveSpeed;

        float accel = (inputDir.sqrMagnitude > 0.01f) ? acceleration : deceleration; // 멈출 땐 감속값

        // 감가속(관성) 처리
        _currentMoveDirection = Vector3.MoveTowards(_currentMoveDirection, targetVelocity, accel * Time.deltaTime);

        return _currentMoveDirection;
    }

    public Vector3 CalculateHoverDeltaY()
    {
        RaycastHit hit;
        Vector3 rayOrigin = _owner.transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 50f, groundLayer))
        {
            groundY = hit.point.y;
        }
        else
        {
            if (!isInit)
            {
                groundY = _owner.transform.position.y;
                previousGroundY = groundY;
                isInit = true;
            }
        }

        float groundYChange = Mathf.Abs(groundY - previousGroundY);
        previousGroundY = groundY;

        // groundY 변화량이 임계치를 초과하면 호버링 오프셋 0으로 처리
        float hoverOffset = 0f;
        bool isFalling = groundYChange >= largeGroundYChangeThreshold;

        if (!isFalling)
        {
            hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverRange;
        }
        else
        {
            // 큰 변화 시에는 고정된 호버 높이 유지 (oscillation 끔)
            hoverOffset = 0f;
        }

        float speed = isFalling ? hoverDownSpeed : hoverSpeed;
        float targetY = groundY + hoverHeight + hoverOffset;
        Vector3 moveDelta = Vector3.zero;
        moveDelta.y = (targetY - (_owner.transform.localPosition.y)) * speed;

        return moveDelta;
    }

    protected void CreateBarrier()
    {
        if (_skillCoroutine != null) return;
        _skillCoroutine = StartCoroutine(CoCreateBarrier());
    }

    IEnumerator CoCreateBarrier()
    {
        GUIManager.Instance.SetLegsSkillIcon(true);
        // 스킬 사용 시 보호막 생성
        _currentBarrier = Utils.Instantiate(barrierPrefab, barrierSpawnPoint.position - new Vector3(0.0f, 0.2f, 0.0f), Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f)), transform);
        Debug.Log("호버링 보호막 생성 및 버프");

        // 이동 속도 증가 및 받는 피해 감소 = Modifier 적용
        _owner.Stats.AddModifier(_partModifiers);

        // 일정 시간 동안 지속 후 쿨타임
        yield return new WaitForSeconds(skillDuration);
        Debug.Log("호버링 보호막 종료");

        // 보호막 파괴
        if (_currentBarrier != null)
        {
            Destroy(_currentBarrier);
            _currentBarrier = null;
        }
        _owner.Stats.RemoveModifier(this);

        float time = skillCooldown - _owner.Stats.TotalStats[EStatType.CooldownReduction].value;
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

        Debug.Log("호버링 쿨타임 초기화");
        GUIManager.Instance.SetLegsSkillIcon(false);
        GUIManager.Instance.SetLegsSkillCooldown(false);
        _skillCoroutine = null;
    }
}
