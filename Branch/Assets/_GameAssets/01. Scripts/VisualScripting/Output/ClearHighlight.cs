using Managers;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 튜토리얼 강조 연출을 끄는 Output. 목표 달성 조건(상자 열기, 파츠 교체 등)에 연결한다.
    public class ClearHighlight : ProcessBase
    {
        [Tooltip("이 단계에서 쓰인 강조 노드. 실행 시 함께 완료 처리해, 이후에 불려도 다시 켜지지 않게 한다.\n" +
                 "튜토리얼을 미리 달성해 단계를 건너뛰었을 때 뒤늦게 강조가 켜지는 것을 막는다.")]
        [SerializeField] private List<HighlightTarget> finishTargets = new();

        public override void Execute()
        {
            if (IsOn) return;

            foreach (HighlightTarget target in finishTargets)
            {
                if (target != null) target.Finish();
            }

            GUIManager.Instance.GameUIController.TutorialHighlight.Hide();

            IsOn = true;
        }
    }
}
