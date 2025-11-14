using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ToggleUI : ProcessBase
    {
        [SerializeField] private bool isActivate;

        public override void Execute()
        {
            Managers.GUIManager.Instance.HUD.SetActive(isActivate);
        }
    }
}