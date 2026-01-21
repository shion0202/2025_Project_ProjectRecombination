using UnityEngine;
using DG.Tweening;
using Cinemachine;

namespace _Project.Scripts.VisualScripting
{
    public class ShakeObject : ProcessBase
    {
        [SerializeField] private GameObject objectToShake;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float duration = 1f; // 흔드는 시간
        [Range(0.01f, 0.1f)][SerializeField] private float magnitude = 0.1f; // 흔드는 강도
        [SerializeField] private bool repeat;
        
        public override void Execute()
        {
            // 이미 흔들기가 진행 중이면 무시
            if (IsOn) return;
            if (objectToShake is null) return;
            // RunningCoroutine = StartCoroutine(C_Shake());
            
            Debug.Log($"{objectToShake.transform.position.ToString()}");
            IsOn = true;

            // Cinemachine Impulse Source가 있을 경우 기존 Shake 로직 대신 Impulse 실행
            FollowCameraController followCamera = objectToShake.GetComponent<FollowCameraController>();
            impulseSource = GetComponent<CinemachineImpulseSource>();
            if (followCamera != null && impulseSource != null)
            {
                followCamera.ApplyShake(impulseSource, magnitude * 50.0f);
                return;
            }

            if (!repeat)
                objectToShake.transform.DOShakePosition(duration, magnitude).OnComplete(() =>
                {
                    IsOn = false;
                });
            else
                objectToShake.transform.DOShakePosition(duration, magnitude).SetLoops(-1);
        }
    }
}