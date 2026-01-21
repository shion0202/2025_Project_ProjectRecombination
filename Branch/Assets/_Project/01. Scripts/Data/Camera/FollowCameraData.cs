using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Follow Camera Data", order = 10)]
public class FollowCameraData : ScriptableObject
{
    [Header("시야각")]
    [Range(1.0f, 179.0f)] public float FOV;

    [Header("카메라 위치 및 화면 구성")]
    [Range(-0.5f, 1.5f), Tooltip("카메라 기준 캐릭터의 X축 위치")] public float screenX;
    [Range(-0.5f, 1.5f), Tooltip("카메라 기준 캐릭터의 Y축 위치")] public float screenY;
    [Range(0.01f, 10.0f), Tooltip("캐릭터와 카메라 간의 거리")] public float cameraDistance;
    [Tooltip("카메라가 추적할 목표의 위치 오프셋")] public Vector3 trackedOffset;

    [Header("카메라 이동 예측")]
    [Range(0.0f, 1.0f), Tooltip("카메라 이동 예측 시간\n목표의 현재 움직임을 기반으로 다음 위치를 예측하여 카메라를 미리 이동시킵니다.")] public float lookaheadTime;
    [Range(0.0f, 30.0f), Tooltip("카메라 이동 예측을 할 때 부드러운 정도")] public float lookaheadSmoothing;
    [Tooltip("카메라 이동 예측을 사용할 때 Y축 움직임(수직 이동) 무시 여부")] public bool ignoreLookaheadY;

    [Header("카메라 반응 속도")]
    [Range(0.0f, 20.0f), Tooltip("X축 카메라 반응 속도")] public float dampingX;
    [Range(0.0f, 20.0f), Tooltip("Y축 카메라 반응 속도")] public float dampingY;
    [Range(0.0f, 20.0f), Tooltip("Z축 카메라 반응 속도")] public float dampingZ;
    [Tooltip("목표가 움직일 때에만 반응 속도를 적용할지 여부\n활성화할 시 다른 요인(스크립트 등)으로 카메라가 회전할 경우 반응 속도 옵션이 동작하지 않습니다.")] public bool targetMovementOnly;

    [Header("카메라 이동 범위")]
    [Range(0.0f, 2.0f), Tooltip("화면 중앙을 기준으로 설정되는 반응 없는 구역 너비\n데드 존 내에서는 카메라가 움직이지 않습니다.")] public float deadZoneWidth;
    [Range(0.0f, 2.0f), Tooltip("화면 중앙을 기준으로 설정되는 반응 없는 구역 너비\n데드 존 내에서는 카메라가 움직이지 않습니다.")] public float deadZoneHeight;
    [Tooltip("화면 중앙을 기준으로 설정되는 반응 없는 구역 너비\n데드 존 내에서는 카메라가 움직이지 않습니다.")] public float deadZoneDepth;
    [Range(0.0f, 2.0f)] public float softZoneWidth;
    [Range(0.0f, 2.0f)] public float softZoneHeight;
    [Range(-0.5f, 0.5f)] public float softZoneOffsetX;
    [Range(-0.5f, 0.5f)] public float softZoneOffsetY;

    [Header("마우스 감도")]
    [Range(0.01f, 10.0f), Tooltip("X축 감도")] public float sensitivityX;
    [Range(0.01f, 10.0f), Tooltip("Y축 감도")] public float sensitivityY;

    [Header("카메라 회전 범위")]
    [Range(-180.0f, 180.0f), Tooltip("X축(오른쪽) 카메라 회전 범위")] public float maxAimRangeX;
    [Range(-180.0f, 180.0f), Tooltip("X축(왼쪽) 카메라 회전 범위")] public float minAimRangeX;
    [Range(-180.0f, 180.0f), Tooltip("Y축(아래쪽) 카메라 회전 범위")] public float maxAimRangeY;
    [Range(-180.0f, 180.0f), Tooltip("Y축(위쪽) 카메라 회전 범위")] public float minAimRangeY;

    [Header("카메라 가속/감속 시간")]
    [Range(0.0f, 10.0f), Tooltip("카메라 움직임이 최고 속도에 도달하기 위해 필요한 시간\n0을 입력하면 마우스 움직임이 시작될 때 즉시 최고 속도가 됩니다.")] public float accelTimeX;
    [Range(0.0f, 10.0f), Tooltip("카메라 속도가 0이 될 때까지 필요한 시간\n0을 입력하면 마우스 움직임이 끝났을 때 즉시 멈춥니다.")] public float decelTimeX;
    [Range(0.0f, 10.0f), Tooltip("카메라 움직임이 최고 속도에 도달하기 위해 필요한 시간\n0을 입력하면 마우스 움직임이 시작될 때 즉시 최고 속도가 됩니다.")] public float accelTimeY;
    [Range(0.0f, 10.0f), Tooltip("카메라 속도가 0이 될 때까지 필요한 시간\n0을 입력하면 마우스 움직임이 끝났을 때 즉시 멈춥니다.")] public float decelTimeY;

    [Header("카메라 상태 전환 속도")]
    [Range(0.01f, 50.0f), Tooltip("다른 카메라 상태에서 현재 카메라 상태로 전환될 때의 카메라가 전환되는 속도")] public float convertSpeed;
}
