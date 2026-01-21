using System;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public abstract class ProcessBase : MonoBehaviour
    {
        // [Tooltip("에디터에서 우클릭으로 고유 ID를 생성하세요.")]
        // public string uniqueID;
        
        [SerializeField] private bool isOn;
        private Coroutine _runningCoroutine;

        public bool IsOn
        {
            get => isOn;
            protected set => isOn = value;
        }

        public Coroutine RunningCoroutine
        {
            get => _runningCoroutine;
            set => _runningCoroutine = value;
        }
    
        // 컴포넌트가 동작할 기능 함수
        public abstract void Execute();
    
        // 입력된 프로세스의 동작 여부를 Not 연산을 적용해 반환
        protected virtual bool CheckInputProcessStatus(ProcessData processData)
        {
            return (processData.isNot) ? !processData.process.IsOn : processData.process.IsOn;
        }
        
        private void OnEnable()
        { 
            if (isOn)
                isOn = false;
            if (_runningCoroutine != null)
                StopCoroutine(_runningCoroutine);
        }
        
        // // 에디터 편의 기능: 컨텍스트 메뉴로 ID 자동 생성
        // [ContextMenu("Generate Unique ID")]
        // private void GenerateID()
        // {
        //     uniqueID = Guid.NewGuid().ToString();
        // }
    }
}
