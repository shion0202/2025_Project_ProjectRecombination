using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseShoulder : PartBase
{
    [SerializeField] protected LayerMask ignoreMask = 0;

    protected virtual void Awake()
    {
        if (ignoreMask == 0)
        {
            ignoreMask |= 1;
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Outline"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Player"));
        }
    }

    public override void FinishActionForced()
    {
        GUIManager.Instance.ResetSkillCooldown();
    }

    public override void UseAbility()
    {
        
    }

    public override void UseCancleAbility()
    {
        
    }
}
