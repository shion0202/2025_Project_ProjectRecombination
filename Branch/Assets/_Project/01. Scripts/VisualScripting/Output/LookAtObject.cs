using _Project.Scripts.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtObject : ProcessBase
{
    [Header("Settings")]
    [SerializeField] private Transform lookAtObject;        // 회전시킬 오브젝트
    [SerializeField] private Transform pivot;               // 이동된 회전축(점)
    [SerializeField] private Transform target;              // 바라볼 대상
    [SerializeField] private float rotationSpeed = 5f;      // 회전 속도
    
    [Header("ETC")]
    // [SerializeField] private float angularSpeed = 90f;      // deg/sec
    [SerializeField] private Vector3 axis;
    [SerializeField] private Vector3 offset;                // 타겟 오프셋
    

    public override void Execute()
    {
        if (IsOn) return;
        if (target is null || lookAtObject is null) return;

        RunningCoroutine = StartCoroutine(C_LookAt());
    }

    private IEnumerator C_LookAt()
    {
        IsOn = true;
        
        while (true)
        {
            if (target is null || lookAtObject is null)
            {
                IsOn = false;
                yield break; // 코루틴 종료
            }

            Vector3 direction = (target.position + offset) - lookAtObject.position;
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            if (pivot != null)
            {
                // 피벗을 중심으로 회전
                lookAtObject.RotateAround(pivot.position, axis, rotationSpeed * Time.deltaTime);
                direction = (target.position + offset) - lookAtObject.position;
                lookRotation = Quaternion.LookRotation(direction);
                // 피벗을 기준으로 회전한 후, 타겟을 바라보도록 보정
                lookAtObject.rotation = Quaternion.Slerp(lookAtObject.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                // 피벗이 없으면 일반적인 LookAt 동작
                lookAtObject.rotation = Quaternion.Slerp(lookAtObject.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }

            yield return null; // 다음 프레임까지 대기
        }
    }
}
