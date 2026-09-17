using Managers;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 튜토리얼 강조 연출을 끄는 Output. 목표 달성 조건(상자 열기, 파츠 교체 등)에 연결한다.
    public class ClearHighlight : ProcessBase
    {
        public override void Execute()
        {
            if (IsOn) return;

            GUIManager.Instance.GameUIController.TutorialHighlight.Hide();

            IsOn = true;
        }
    }
}
