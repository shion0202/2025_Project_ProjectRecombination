using Managers;
using Monster.AI.BehaviorTree.Nodes;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class LegsBasic : PartBaseLegs
{
    [SerializeField] protected GameObject dashEffectPrefab;
    private bool _isCooldown = false;

    protected override void Awake()
    {
        base.Awake();

        _legsAnimType = EAnimationType.Base;
    }

    protected void OnEnable()
    {
        _currentSkillCount = 0;
        _isCooldown = false;

        if (_owner)
        {
            _owner.FinishDash();
        }

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
        _currentSkillCount = 0;
        _isCooldown = false;
        _owner.FinishDash();

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
        if (_currentSkillCount >= maxSkillCount) return;
        Dash();
    }

    public override Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform)
    {
        if (moveInput == Vector2.zero) return Vector3.zero;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        return moveDirection.normalized * (_owner.Stats.TotalStats[EStatType.WalkSpeed].value + _owner.Stats.TotalStats[EStatType.AddMoveSpeed].value);
    }

    protected void Dash()
    {
        if (_isCooldown) return;

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        LookCameraDirection();
        _owner.Dash(skillRange / skillTime);
        _skillCoroutine = StartCoroutine(CoHandleDash());
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    protected IEnumerator CoHandleDash()
    {
        Vector3 direction = -_owner.DashDirection.normalized;
        Quaternion effectRotation = Quaternion.LookRotation(direction, Vector3.up);
        GameObject go = Utils.Instantiate(dashEffectPrefab, _owner.transform.position + (Vector3.up * 1.0f) + (direction * -3.0f), effectRotation);
        Utils.Destroy(go, 2.0f);
        ++_currentSkillCount;
        if (_currentSkillCount >= maxSkillCount)
        {
            GUIManager.Instance.SetLegsSkillIcon(true);
        }

        yield return new WaitForSeconds(skillTime);

        _isCooldown = true;
        _owner.FinishDash();
        GUIManager.Instance.SetLegsSkillIcon(true);

        float time = (skillCooldown * (_currentSkillCount)) - _owner.Stats.TotalStats[EStatType.CooldownReduction].value;
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
        _currentSkillCount = 0;
        _isCooldown = false;
        _skillCoroutine = null;
    }
}
