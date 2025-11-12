using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public abstract class ProcessBase : MonoBehaviour
    {
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
    }
}
