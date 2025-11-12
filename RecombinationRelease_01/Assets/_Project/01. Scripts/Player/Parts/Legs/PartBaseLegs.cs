using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseLegs : PartBase, ILegsMovement
{
    [Header("다리 파츠 설정")]
    [SerializeField] protected float skillTime = 0.5f;
    [SerializeField] protected float skillDuration = 1.0f;
    [SerializeField] protected float skillCooldown = 3.0f;
    [SerializeField] protected int maxSkillCount = 1;
    [SerializeField] protected float skillRange = 1.0f;
    [SerializeField] protected float skillDamage = 0.0f;
    protected int _currentSkillCount = 0;
    protected Coroutine _skillCoroutine = null;
    protected EAnimationType _legsAnimType = EAnimationType.Base;

    public EAnimationType LegsAnimType => _legsAnimType;

    protected virtual void Awake()
    {

    }

    public override void UseAbility()
    {
        // 스킬 입력 시작 시 실행할 로직
    }

    public override void UseCancleAbility()
    {
        // 스킬 입력 종료 시 실행할 로직
        // 버튼을 누르는 동안 차지 후, 버튼을 뗄 때 대시하는 기능 등
    }

    // 장비 교체 등 특수한 상황에서 대시를 강제로 종료해야 할 때 사용
    public override void FinishActionForced()
    {
        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        _currentSkillCount = 0;
        GUIManager.Instance.ResetSkillCooldown();
    }

    public virtual Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        return Vector3.zero;
    }
}
