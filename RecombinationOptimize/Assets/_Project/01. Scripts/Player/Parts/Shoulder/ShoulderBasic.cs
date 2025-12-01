using Managers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ShoulderBasic : PartBaseShoulder
{
    private void OnEnable()
    {
        //Managers.GUIManager.Instance.ShoulderIcon.SetActive(true);
        Managers.GUIManager.Instance.SetBackSkillIcon(true);
    }

    private void OnDisable()
    {
        if (Managers.GUIManager.IsAliveInstance())
        {
            //Managers.GUIManager.Instance.ShoulderIcon.SetActive(false);
            Managers.GUIManager.Instance.SetBackSkillIcon(false);
        }
    }

    public override IEnumerator CoStartCooldown()
    {
        GUIManager.Instance.SetBackSkillIcon(true);
        GUIManager.Instance.SetBackSkillCooldown(true);
        GUIManager.Instance.SetBackSkillCooldown(_currentCooldown);

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            _currentCooldown -= 0.1f;
            GUIManager.Instance.SetBackSkillCooldown(_currentCooldown);
            if (_currentCooldown <= 0.0f)
            {
                _currentCooldown = 0.0f;
                break;
            }
        }

        GUIManager.Instance.SetBackSkillCooldown(false);
        _cooldownRoutine = null;
    }
}
