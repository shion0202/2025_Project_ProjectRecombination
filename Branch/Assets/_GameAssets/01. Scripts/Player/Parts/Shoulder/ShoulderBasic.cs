using Managers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ShoulderBasic : PartBaseShoulder
{
    private void OnEnable()
    {
        //Managers.GUIManager.Instance.GameUIController.ShoulderIcon.SetActive(true);
        Managers.GUIManager.Instance.GameUIController.SetBackSkillIcon(true);
    }

    private void OnDisable()
    {
        if (Managers.GUIManager.IsAliveInstance())
        {
            //Managers.GUIManager.Instance.GameUIController.ShoulderIcon.SetActive(false);
            Managers.GUIManager.Instance.GameUIController.SetBackSkillIcon(false);
        }
    }

    public override IEnumerator CoStartCooldown()
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

        GUIManager.Instance.GameUIController.SetBackSkillCooldown(false);
        _cooldownRoutine = null;
    }
}
