using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetNoticeMessage : ProcessBase
    {
        [SerializeField] private string noticeMessage;

        public override void Execute()
        {
            if (IsOn) return;

            Managers.GUIManager.Instance.ActivateMessage(noticeMessage);

            IsOn = true;
        }
    }
}