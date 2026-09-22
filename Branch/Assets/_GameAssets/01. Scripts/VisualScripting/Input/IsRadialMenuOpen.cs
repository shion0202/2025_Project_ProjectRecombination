using Managers;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 파츠 교체(래디얼) 메뉴가 열려 있는 동안 IsOn이 되는 Input.
    ///
    /// 파츠 교체는 Tab을 누른 채로 메뉴에서 고르는 2단계 조작이라, 단계마다 강조할 곳이 다르다.
    /// 이 노드와 Conditional의 Not 옵션을 조합하면 "누르기 전 안내"와 "메뉴 안 안내"를 나눌 수 있다.
    /// 메뉴를 닫으면 다시 꺼지므로, Tab을 놓았다 다시 눌러도 안내가 따라온다.
    /// </summary>
    public class IsRadialMenuOpen : ProcessBase
    {
        // 그래프의 시작점처럼 매 프레임 상태를 갱신한다. 다른 노드가 Execute()를 불러주지 않는다.
        private void Update()
        {
            if (!GUIManager.IsAliveInstance()) return;

            GameUIController uiController = GUIManager.Instance.GameUIController;
            if (uiController == null || uiController.RadialUI == null) return;

            IsOn = uiController.RadialUI.activeSelf;
        }

        public override void Execute()
        {
        }

        public override string ToString()
        {
            return $"[{gameObject.name} ({GetType().Name})] IsOn: {IsOn}";
        }
    }
}
