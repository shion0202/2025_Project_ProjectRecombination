using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseShoulder : PartBase
{
    [SerializeField] protected LayerMask ignoreMask = 0;

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
}
