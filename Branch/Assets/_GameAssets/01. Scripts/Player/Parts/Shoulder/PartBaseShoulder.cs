using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseShoulder : PartBase
{
    [SerializeField] protected LayerMask ignoreMask = 0;
    [SerializeField] protected float skillCooldown = 0.0f;

    protected override void Awake()
    {
        base.Awake();
        
        if (ignoreMask == 0)
        {
            ignoreMask = ~0;
            ignoreMask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Water"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("UI"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Face"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Hair"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Outline"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Player"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("PlayerMesh"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Bullet"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Minimap"));
        }
    }

    public override void FinishActionForced()
    {
        //GUIManager.Instance.ResetSkillCooldown();
    }

    public override void UseAbility()
    {
        
    }

    public override void UseCancleAbility()
    {
        
    }

    public override void PreserveCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;
        if (_cooldownRoutine != null)
        {
            StopCoroutine(_cooldownRoutine);
            _cooldownRoutine = null;
        }

        // 쿨타임이 얼마나 지났는지 백분율(%)로 저장 (1 -> 0)
        _owner.CooldownDictionary[currentPartType] = _currentCooldown / skillCooldown;
    }

    public override void SetCurrentCooldown(EPartType currentPartType)
    {
        if (!_owner) return;

        _currentCooldown = skillCooldown * _owner.CooldownDictionary[currentPartType];

        if (_currentCooldown > 0.0f)
        {
            _cooldownRoutine = StartCoroutine(CoStartCooldown());
        }
    }

    public virtual IEnumerator CoStartCooldown()
    {
        GUIManager.Instance.GameUIController.SetBackSkillIcon(true);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(true);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(_currentCooldown);

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            _currentCooldown -= 0.1f;
            GUIManager.Instance.GameUIController.SetBackSkillCooldown(_currentCooldown);
            if (_currentCooldown <= 0.0f)
            {
                _currentCooldown = 0.0f;
                break;
            }
        }

        GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
        GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        _cooldownRoutine = null;
    }
}
