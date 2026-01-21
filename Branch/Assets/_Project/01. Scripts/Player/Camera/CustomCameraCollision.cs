using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCameraCollision : MonoBehaviour
{
    public Transform playerTransform; // 플레이어 위치
    public Camera cam; // 주 카메라
    public float minDistance = 0.3f; // 카메라 최소 거리
    public float maxDistance = 5f;   // 카메라 최대 거리
    public float smoothSpeed = 10f;  // 카메라 이동 부드러움

    // 충돌 감지 레이어 마스크 (Wall, Breakable 등 포함)
    public LayerMask collisionLayers;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        Vector3 desiredCameraPos = playerTransform.position - playerTransform.forward * maxDistance + Vector3.up * 1.5f; // 카메라 기본 위치 (플레이어 뒤 + 높이)

        // 플레이어 위치에서 카메라 방향으로 레이캐스트 (장애물 감지)
        RaycastHit hit;
        Vector3 rayDirection = desiredCameraPos - playerTransform.position;
        float rayDistance = rayDirection.magnitude;
        rayDirection.Normalize();

        if (Physics.SphereCast(playerTransform.position + Vector3.up * 1.5f, 0.2f, rayDirection, out hit, rayDistance, collisionLayers))
        {
            // 충돌지점까지 거리 계산
            float hitDistance = hit.distance;

            // 최소거리보다 가까워지지 않게 제한
            float clampedDistance = Mathf.Clamp(hitDistance - 0.1f, minDistance, maxDistance);

            Vector3 newCameraPos = playerTransform.position + rayDirection * clampedDistance + Vector3.up * 1.5f;
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, newCameraPos, ref currentVelocity, 1f / smoothSpeed);
        }
        else
        {
            // 장애물이 없어 기본 위치로
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredCameraPos, ref currentVelocity, 1f / smoothSpeed);
        }

        cam.transform.LookAt(playerTransform.position + Vector3.up * 1.5f); // 카메라가 플레이어 정면을 바라보도록
    }
}
