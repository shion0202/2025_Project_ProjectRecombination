using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ToggleInteractionHUD : ProcessBase
    {
        [SerializeField] private bool isActivate;
        [SerializeField] private string interactionName;

        public override void Execute()
        {
            Managers.GUIManager.Instance.InteractionUI.SetActive(isActivate);
            if (isActivate)
            {
                Managers.GUIManager.Instance.InteractionText.text = interactionName;
            }
        }
    }
}