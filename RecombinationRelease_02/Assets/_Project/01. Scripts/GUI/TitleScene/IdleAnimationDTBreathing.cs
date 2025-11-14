using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace _Project.Scripts.GUI.TitleScene
{
    public class IdleAnimationDTBreathing : DTBase
    {
        public float moveAmount = 0.1f;
        public float scaleAmountY = 1.05f;
        public float duration = 1.5f;
        
        private void Update()
        {
            if (!isStarted) return;

            if (isRunning) return;
            
            isRunning = true;
            
            // 1. Y축 이동 (Bobbing)
            transform.DOMoveY(transform.position.y + moveAmount, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // 무한 반복 + 갔다가 돌아오기

            // 2. Y축 스케일 (Breathing)
            transform.DOScaleY(transform.localScale.y * scaleAmountY, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // 무한 반복 + 갔다가 돌아오기
        }
    }
}