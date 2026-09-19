using UnityEngine;

/// <summary>
/// 조준 제약(MultiAimConstraint)과 같은 오브젝트에 붙여, RigAimController가 올리는 최대 가중치를 제한한다.
///
/// 팔 조준은 위팔과 전완 두 제약으로 나눠 쓴다. 위팔을 1까지 올리면 팔이 일직선으로 뻗고,
/// 낮추면 애니메이션 포즈가 남아 팔꿈치가 벌어진 자세가 된다. 무기 방향은 전완 제약이 맞춘다.
///
/// 사격 애니메이션은 한 팔 기준이라 양팔 사격 시 팔이 옆으로 벌어진다.
/// 그래서 한 팔 사격과 양팔 사격의 상한을 따로 둔다. (양팔일 때 더 높게 두어 IK가 팔을 정면으로 모은다)
/// </summary>
public class AimWeightScale : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    [Tooltip("한 팔만 사격할 때 이 제약이 도달할 최대 가중치")]
    public float maxWeight = 1.0f;

    [Range(0.0f, 1.0f)]
    [Tooltip("양팔을 함께 사격할 때 이 제약이 도달할 최대 가중치")]
    public float dualMaxWeight = 1.0f;
}
