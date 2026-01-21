using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [System.Serializable]
    public struct RotateAxis
    {
        public bool x;
        public bool y;
        public bool z;
    }
    
    public class RotateObject : ProcessBase
    {
        [Tooltip("회전 대상")][SerializeField] private GameObject objectToRotate;
        [Tooltip("회전 각")][Range(-90f, 90f)][SerializeField] private float rotateAngle;
        [Tooltip("회전 축")][SerializeField] private RotateAxis axis;
        [Tooltip("회전 시간")][SerializeField] private float timeToRotate = 1f;
        [Tooltip("1회 회전 후 멈출지 여부")][SerializeField] private bool rotateOnce = true;
        
        public override void Execute()
        {
            if (IsOn) return;
            if (CheckNull()) return;

            StartCoroutine(C_Rotate());
        }

        private IEnumerator C_Rotate()
        {
            IsOn = true;
            
            Transform tf = objectToRotate.transform;

            // 1) 시작/목표 회전 계산
            Quaternion start = tf.localRotation;

            // 여러 축이 동시에 켜질 수 있음
            bool anyAxis = axis.x || axis.y || axis.z;
            Vector3 eulerDelta = anyAxis
                ? new Vector3(
                    axis.x ? rotateAngle : 0f,
                    axis.y ? rotateAngle : 0f,
                    axis.z ? rotateAngle : 0f
                )
                : new Vector3(0f, rotateAngle, 0f); // 축 미선택 시 Y축 기본

            Quaternion target = start * Quaternion.Euler(eulerDelta);

            float elapsedTime = 0f;

            while (elapsedTime < timeToRotate)
            {
                elapsedTime += Time.deltaTime;
                // float t = elapsedTime / timeToRotate;
                // 오브젝트를 주어진 시간 동안 회전시킨다.
                float t = Mathf.Clamp01(elapsedTime / timeToRotate);
                // t = ease.Evaluate(t); // 가감속

                tf.localRotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }
            // 3) 마지막에 목표값으로 스냅(부동소수 오차 제거)
            tf.localRotation = target;
            
            IsOn = rotateOnce;
        }
        
        private bool CheckNull() => (objectToRotate is null || timeToRotate <= 0f);
    }
}