using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class DebugLog : ProcessBase
    {
        [SerializeField] private string logMessage = "디버그 로그 메시지";
        public override void Execute()
        {
            if (IsOn) return;
            Debug.Log($"{logMessage}");
            IsOn = true;
        }
    }
}