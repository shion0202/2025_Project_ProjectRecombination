using Cinemachine;
using UnityEngine;
using DG.Tweening;
using Managers;

namespace _Project.Scripts.VisualScripting
{
    public class ExcutionerDumy: ProcessBase
    {
        [SerializeField] private GameObject objectToShake;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float shakeDuration = 0.5f;
        [Range(0.01f, 0.1f)][SerializeField] private float shakeMagnitude = 0.1f;
        [SerializeField] private bool repeat;
    
        public override void Execute()
        {
            // Debug.Log(IsOn ? "ExcutionerDumy is On" : "ExcutionerDumy is Off"); // 디버그용
        }

        public void OneStepShake()
        {
           // if (!IsOn) return;

           objectToShake ??= GameManager.Instance.FollowCamera.gameObject;
           
           FollowCameraController followCamera = objectToShake.GetComponent<FollowCameraController>();
           impulseSource = GetComponent<CinemachineImpulseSource>();
        
           if (followCamera != null && impulseSource != null)
           {
               followCamera.ApplyShake(impulseSource, shakeMagnitude * 50.0f);
               return;
           }

           if (!repeat)
               objectToShake.transform.DOShakePosition(shakeDuration, shakeMagnitude).OnComplete(() =>
               {
                   IsOn = false;
               });
           else
               objectToShake.transform.DOShakePosition(shakeDuration, shakeMagnitude).SetLoops(-1);
        }
    }
}