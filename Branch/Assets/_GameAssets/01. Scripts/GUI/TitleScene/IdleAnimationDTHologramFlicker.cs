using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace _Project.Scripts.GUI.TitleScene
{
    public class IdleAnimationDTHologramFlicker : DTBase
    {
        // public bool isStarted;
        // public bool isRunning;
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }
        
        private void Update()
        {
            if (!isStarted) return;
            if (isRunning) return;
            
            isRunning = true;

            StartFlickerLoop();
        }

        private void StartFlickerLoop()
        {
            // 시퀀스 생성
            Sequence flickerSequence = DOTween.Sequence();

            // 0.05초 만에 거의 투명하게 (알파 0.1)
            flickerSequence.Append(_image.DOFade(0.1f, 0.05f));
        
            // 0.1초 대기
            flickerSequence.AppendInterval(0.1f);
        
            // 0.05초 만에 다시 불투명하게 (알파 1.0)
            flickerSequence.Append(_image.DOFade(1.0f, 0.05f));
        
            // 불규칙한 다음 깜빡임을 위해 랜덤 시간 대기 (0.5초 ~ 2.0초 사이)
            flickerSequence.AppendInterval(Random.Range(0.5f, 2.0f));

            // 시퀀스 완료 시, 자기 자신(StartFlickerLoop)을 다시 호출하여 무한 반복
            flickerSequence.OnComplete(StartFlickerLoop);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_image);
        }
    }
}