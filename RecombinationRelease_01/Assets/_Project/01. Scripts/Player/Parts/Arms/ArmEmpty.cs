using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmEmpty : PartBaseArm
{
    protected override void Awake()
    {
        base.Awake();
        _isAnimating = false;
    }

    public override void UseAbility()
    {
        // 경★아무것도안함★축
    }
}
