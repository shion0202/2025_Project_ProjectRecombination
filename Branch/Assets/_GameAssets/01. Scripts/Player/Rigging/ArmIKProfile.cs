using UnityEngine;

/// <summary>
/// 팔 파츠별 사격 IK 자세 값. 파츠마다 무기 크기와 스킨 본 구성이 달라 같은 값으로는 자세가 맞지 않는다.
/// PartBaseArm이 참조하고, ArmShootIKTargets가 현재 장착된 팔 파츠의 값을 읽어 쓴다.
/// 에셋이라 플레이 중에 바꾼 값이 종료 후에도 남는다.
/// </summary>
[CreateAssetMenu(fileName = "ArmIKProfile", menuName = "Player/Arm IK Profile")]
public class ArmIKProfile : ScriptableObject
{
    [Tooltip("팔 길이 대비 손 타깃 거리. 1보다 크면 타깃이 팔이 닿는 거리 밖에 놓여, " +
             "몸을 숙이거나 타깃이 늦게 따라와도 팔이 끝까지 곧게 펴진 채 조준 방향을 가리킨다.")]
    [Range(0.5f, 1.2f)] public float reachRatio = 0.95f;

    [Tooltip("조준 방향에서 팔을 바깥쪽으로 벌리는 각도(도). 0이면 두 팔이 나란히 정면을 향한다.")]
    [Range(0.0f, 30.0f)] public float spreadAngle = 5.0f;

    [Tooltip("팔꿈치 힌트 위치. 팔 중간 지점 기준 캐릭터 로컬 좌표이며, x는 바깥쪽 방향으로 좌우 대칭 적용된다.")]
    public Vector3 elbowHintOffset = new Vector3(0.3f, -0.2f, -0.1f);

    [Tooltip("어깨 위치 보정(캐릭터 로컬). x는 바깥쪽 방향으로 좌우 대칭 적용된다.")]
    public Vector3 shoulderOffset = Vector3.zero;
}
