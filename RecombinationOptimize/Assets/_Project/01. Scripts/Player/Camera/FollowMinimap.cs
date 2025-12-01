using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Target의 XZ축 좌표를 따라가며, 옵션에 따라 POV 회전에 맞춰 회전하는 미니맵 스크립트
public class FollowMinimap : MonoBehaviour
{
    [SerializeField] private bool isRotate = false;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0.0f, 20.0f, 0.0f);
    [SerializeField] private float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        Vector3 euler = transform.eulerAngles;
        if (isRotate)
        {
            // 미니맵 카메라는 y축(수평) 회전만 메인카메라에 동기화
            euler.y = Camera.main.transform.eulerAngles.y;
            transform.eulerAngles = euler;
        }

        // 위치는 기존대로 offset 유지
        Vector3 desiredPosition = target.position + Quaternion.Euler(0f, euler.y, 0f) * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}
