using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 이벤트 실행 여부를 판단하여 등록된 이벤트가 호출될 때 실행되는 Input
    // Register Output을 통해 등록 필요 (Input → Output(Register) → Input(OnEvent) → Output 구조)
    public class OnEvent : ProcessBase
    {
        public override void Execute()
        {
            IsOn = true;
        }

        public override string ToString()
        {
            string objectName = gameObject.name;
            string log = $"[{objectName} ({GetType().Name})] IsOn: {IsOn}";
            return log;
        }
    }
}
