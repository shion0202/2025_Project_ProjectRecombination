using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetIndicatorTarget : ProcessBase
    {
        [SerializeField] private Transform target;

        public override void Execute()
        {
            if (IsOn) return;

            Managers.GUIManager.Instance.SetIndicatorTarget(target);

            IsOn = true;
        }
    }
}