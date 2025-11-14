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
}
