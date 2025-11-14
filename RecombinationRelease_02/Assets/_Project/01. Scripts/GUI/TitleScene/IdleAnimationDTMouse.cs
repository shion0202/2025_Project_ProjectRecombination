using UnityEngine;
using UnityEngine.UI; // UI.Image를 사용하기 위해 필요
using DG.Tweening;   // DOTween을 사용하기 위해 필요

namespace _Project.Scripts.GUI.TitleScene
{
    public class IdleAnimationDTMouse : DTBase
    {
        // --- 1. 배경 애니메이션 (DOTween 루프) ---
        [Header("A: 배경 숨쉬기 (Looping Idle)")]
        [Tooltip("캐릭터가 미세하게 커졌다 작아지는 정도 (예: 1.03 = 3% 커짐)")]
        public float idlePulseScale = 1.03f;
        [Tooltip("숨쉬기 1회에 걸리는 시간 (주기)")]
        public float idlePulseDuration = 4.0f;

        // --- 2. 마우스 반응 (Reactive Tilt) ---
        [Header("B: 마우스 2.5D 기울임 (Reactive Tilt)")]
        [Tooltip("마우스 움직임에 따라 이미지가 기울어지는 최대 각도")]
        public float maxTiltAngle = 7f;
        [Tooltip("마우스 움직임에 따라 이미지가 parallax 효과로 움직이는 최대 거리 (픽셀)")]
        public float maxParallaxMove = 15f;
        [Tooltip("기울임/움직임이 마우스를 따라오는 부드러움 (값이 클수록 빠름)")]
        public float tiltSmoothSpeed = 5f;

        // --- 내부 변수 ---
        private RectTransform _rectTransform;
        private Vector2 _originalAnchorPos; // UI의 초기 앵커 위치
        private Vector3 _originalScale;     // UI의 초기 스케일
        
        // 마우스 반응을 위한 목표값
        private Quaternion _targetRotation;
        private Vector2 _targetAnchorPos;

        void Update()
        {
            if (!isStarted) return;

            if (!isRunning)
            {
                isRunning = true;
                Init();
            }
            
            // 3. 마우스 위치 계산 (화면 중앙 기준 -1.0 ~ +1.0 범위로 정규화)
            float mouseX = (Input.mousePosition.x - (Screen.width / 2f)) / (Screen.width / 2f);
            float mouseY = (Input.mousePosition.y - (Screen.height / 2f)) / (Screen.height / 2f);
            
            // 화면 밖으로 나가도 -1, 1을 넘지 않도록 고정
            mouseX = Mathf.Clamp(mouseX, -1f, 1f);
            mouseY = Mathf.Clamp(mouseY, -1f, 1f);

            // --- 4. 마우스 위치에 따른 '기울임(Rotation)' 계산 ---
            _targetRotation = Quaternion.Euler(
                mouseY * maxTiltAngle,  // 마우스가 위/아래로 가면 X축 회전
                -mouseX * maxTiltAngle, // 마우스가 좌/우로 가면 Y축 회전 (방향 반대)
                0                       // Z축은 고정
            );

            // --- 5. 마우스 위치에 따른 '이동(Parallax)' 계산 ---
            _targetAnchorPos = _originalAnchorPos + new Vector2(
                mouseX * maxParallaxMove, 
                mouseY * maxParallaxMove
            );

            // --- 6. 실제 값 적용 (Lerp를 사용해 부드럽게) ---
            
            // 목표 회전값으로 부드럽게 이동
            _rectTransform.localRotation = Quaternion.Lerp(
                _rectTransform.localRotation, 
                _targetRotation, 
                Time.deltaTime * tiltSmoothSpeed
            );

            // 목표 위치로 부드럽게 이동
            _rectTransform.anchoredPosition = Vector2.Lerp(
                _rectTransform.anchoredPosition, 
                _targetAnchorPos, 
                Time.deltaTime * tiltSmoothSpeed
            );
        }

        private void Init()
        {
            // 1. 컴포넌트 및 초기값 저장
            _rectTransform = GetComponent<RectTransform>();
            _originalAnchorPos = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;

            // 2. DOTween으로 '배경 숨쉬기(맥박)' 애니메이션 시작
            _rectTransform.DOScale(_originalScale * idlePulseScale, idlePulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // 무한히 컸다가 작아지기 반복
        }

        // 씬 종료 또는 오브젝트 파괴 시 DOTween 루프 정리
        void OnDestroy()
        {
            _rectTransform.DOKill();
        }
    }
}