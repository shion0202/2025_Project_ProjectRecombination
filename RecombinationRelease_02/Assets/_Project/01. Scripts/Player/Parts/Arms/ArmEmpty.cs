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

    private void OnEnable()
    {
        Managers.GUIManager.Instance.RightArmRadial.SetActive(false);
        Managers.GUIManager.Instance.SetAmmoColor(partType, Color.black);
        Managers.GUIManager.Instance.SetAmmoColor(partType, true);
    }

    private void OnDisable()
    {
        if (Managers.GUIManager.IsAliveInstance())
        {
            Managers.GUIManager.Instance.RightArmRadial.SetActive(true);
        }
    }

    public override void UseAbility()
    {
        // 경★아무것도안함★축
    }
}
