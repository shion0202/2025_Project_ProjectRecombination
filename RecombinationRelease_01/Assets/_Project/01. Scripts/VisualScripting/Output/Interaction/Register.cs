using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.VisualScripting
{
    // 상호작용 키를 통해 호출할 이벤트를 등록하는 Output
    // OnEvent Input을 nextInput 변수에 연결 (Input → Output(Register) → Input(OnEvent) → Output 구조)
    public class Register : ProcessBase
    {
        [SerializeField] private ProcessBase nextInput;

        public override void Execute()
        {
            IsOn = true;

            EventManager.Instance.AddListener(EEventType.Interaction, OnRegister);
        }

        private void OnRegister(EEventType eventType, Component sender, object param = null)
        {
            nextInput.Execute();
        }

        public override string ToString()
        {
            string objectName = gameObject.name;
            string nextInputName = (nextInput != null) ? nextInput.GetType().Name : "None";

            string log = $"[{objectName} ({GetType().Name})] IsOn: {IsOn}, Next: {nextInputName}";
            return log;
        }
    }
}
