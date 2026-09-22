using UnityEngine;

/// <summary>
/// 사격 시 팔 IK(TwoBoneIK)의 손 타깃과 팔꿈치 힌트 위치를 계산한다.
///
/// 사격 애니메이션은 한 팔 기준이라 허리를 비틀어 팔을 앞으로 보낸다. 양팔 사격에서는 허리를 펴므로
/// 팔이 사선으로 벌어지는데, 손을 각 어깨 정면(조준 방향)으로 끌어와 두 팔이 나란히 앞을 향하게 한다.
///
/// 타깃은 매 프레임 실제 어깨 위치에서 조준점 방향으로 팔 길이만큼 떨어진 곳에 둔다.
/// 어깨를 고정해 두면 가슴 조준 IK로 상체를 숙이거나 젖힐 때 실제 어깨와 타깃 거리가 줄어 팔꿈치가 접힌다.
/// 방향은 어깨의 정면이 아니라 조준점을 향하므로, 허리가 비틀려도 팔은 조준점을 가리킨다.
/// 타깃과 힌트는 캐릭터 루트의 자식으로 두고 로컬 좌표로 옮겨, 이동 중에도 한 프레임씩 뒤처지지 않게 한다.
///
/// 자세 값은 파츠마다 무기 크기와 스킨 본 구성이 달라, 장착한 팔 파츠의 ArmIKProfile을 쓴다.
/// 파츠에 프로필이 없으면 아래 기본값을 쓴다.
/// </summary>
[DefaultExecutionOrder(10000)]
public class ArmShootIKTargets : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private RigAimController rigAimController;
    [SerializeField] private PlayerController playerController;

    [Header("IK 타깃 (캐릭터 루트의 자식)")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform leftElbowHint;
    [SerializeField] private Transform rightElbowHint;

    [Header("기본 자세 (파츠에 프로필이 없을 때)")]
    [Tooltip("팔 길이 대비 손 타깃 거리. 1보다 크면 타깃이 팔이 닿는 거리 밖에 놓여, " +
             "몸을 숙이거나 타깃이 늦게 따라와도 팔이 끝까지 곧게 펴진 채 조준 방향을 가리킨다.")]
    [SerializeField, Range(0.5f, 1.2f)] private float reachRatio = 0.95f;
    [Tooltip("팔꿈치 힌트 위치. 팔 중간 지점 기준 캐릭터 로컬 좌표이며, x는 바깥쪽 방향으로 좌우 대칭 적용된다.")]
    [SerializeField] private Vector3 elbowHintOffset = new Vector3(0.3f, -0.2f, -0.1f);
    [Tooltip("어깨 위치 보정(캐릭터 로컬). x는 바깥쪽 방향으로 좌우 대칭 적용된다.")]
    [SerializeField] private Vector3 shoulderOffset = Vector3.zero;
    [Tooltip("조준 방향에서 팔을 바깥쪽으로 벌리는 각도(도). 0이면 두 팔이 나란히 정면을 향한다.")]
    [SerializeField, Range(0.0f, 30.0f)] private float spreadAngle = 5.0f;
    [Tooltip("발사할 때 손이 어깨 쪽으로 밀리는 거리(m).")]
    [SerializeField, Range(0.0f, 0.3f)] private float recoilDistance = 0.06f;
    [Tooltip("발사할 때 팔이 위로 들리는 각도(도).")]
    [SerializeField, Range(0.0f, 30.0f)] private float recoilPitch = 5.0f;
    [Tooltip("반동에서 원래 자세로 돌아오는 빠르기.")]
    [SerializeField, Range(1.0f, 40.0f)] private float recoilRecoverSpeed = 14.0f;

    [Header("공통")]
    [Tooltip("타깃이 목표 위치를 따라가는 빠르기. 높을수록 즉각 반응하고, 낮을수록 부드럽지만 늦게 따라간다.\n" +
             "조준점(카메라)과 캐릭터 회전의 갱신 시점이 달라 생기는 팔 떨림을 줄인다.")]
    [SerializeField] private float followSharpness = 20.0f;

    [Header("디버그")]
    [Tooltip("[임시] 화면에 팔별 타깃 거리 비율, 팔꿈치 각도, IK 가중치를 표시하고 Scene 뷰에 선을 그린다.")]
    [SerializeField] private bool showDebug = false;

    private Transform _leftUpper, _leftLower, _leftHand;
    private Transform _rightUpper, _rightLower, _rightHand;
    private float _leftArmLength;
    private float _rightArmLength;
    private bool _hasBones;
    private bool _hasLeftTarget;
    private bool _hasRightTarget;

    // 어깨 기준으로 부드럽게 따라가는 타깃/힌트 오프셋(캐릭터 회전 기준, 스케일 무관). 월드 좌표로 변환해 넣는다.
    private Vector3 _leftHandLocal, _rightHandLocal;
    private Vector3 _leftHintLocal, _rightHintLocal;

    // 발사 반동 강도(1에서 시작해 0으로 줄어든다).
    private float _leftRecoil;
    private float _rightRecoil;

    /// <summary>
    /// 발사 반동을 준다. 손이 어깨 쪽으로 밀리고 팔이 위로 들렸다가 돌아온다.
    /// PlayerController.ApplyRecoil에서 발사한 팔을 판별해 호출한다.
    /// </summary>
    public void Kick(bool isLeft)
    {
        if (isLeft) _leftRecoil = 1.0f;
        else _rightRecoil = 1.0f;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rigAimController == null) rigAimController = GetComponent<RigAimController>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    // 타깃은 그 프레임의 모든 처리(애니메이션, 리그, 캐릭터 이동, 다른 절차적 스크립트)가 끝난 뒤의 최종 어깨 위치로 계산하고,
    // 다음 프레임의 IK가 이 타깃을 쓴다. Update 시점의 뼈 위치는 다른 스크립트가 초기 자세로 되돌려 둔 상태일 수 있어
    // 실제 어깨와 어긋나고, 그만큼 타깃이 가까워져 팔이 굽는다. (실행 순서를 가장 늦게 둔다)
    private void LateUpdate()
    {
        if (animator == null || rigAimController == null || !rigAimController.IsInit) return;

        if (!_hasBones)
        {
            _hasBones = FindBones();
            if (!_hasBones) return;
        }

        UpdateArm(true);
        UpdateArm(false);
    }

    private bool FindBones()
    {
        _leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        _rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (_leftUpper == null || _leftLower == null || _leftHand == null ||
            _rightUpper == null || _rightLower == null || _rightHand == null)
        {
            return false;
        }

        return true;
    }

    // 현재 팔 길이. 뼈 사이 거리는 자세와 무관하지만, 스폰 연출 등으로 캐릭터 스케일이 바뀌는 순간에 한 번만 재면
    // 실제보다 짧게 잡혀 타깃이 팔이 닿는 거리 안쪽에 놓이므로 매 프레임 다시 잰다.
    private float MeasureArmLength(bool isLeft)
    {
        Transform upper = isLeft ? _leftUpper : _rightUpper;
        Transform lower = isLeft ? _leftLower : _rightLower;
        Transform hand = isLeft ? _leftHand : _rightHand;
        return Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
    }

    // 현재 장착한 팔 파츠의 프로필. 없으면 null.
    private ArmIKProfile GetEquippedProfile(bool isLeft)
    {
        if (playerController == null || playerController.Inven == null) return null;

        var equipped = playerController.Inven.EquippedItems;
        EPartType partType = isLeft ? EPartType.ArmL : EPartType.ArmR;
        if (!equipped.TryGetValue(partType, out var parts) || parts.Count == 0) return null;

        return parts[0] is PartBaseArm arm ? arm.IKProfile : null;
    }

    private void UpdateArm(bool isLeft)
    {
        Transform handTarget = isLeft ? leftHandTarget : rightHandTarget;
        Transform elbowHint = isLeft ? leftElbowHint : rightElbowHint;
        if (handTarget == null) return;

        Transform aimTarget = rigAimController.GetArmAimTarget(isLeft);
        if (aimTarget == null) return;

        ArmIKProfile profile = GetEquippedProfile(isLeft);
        float profileReach = profile != null ? profile.reachRatio : reachRatio;
        float profileSpread = profile != null ? profile.spreadAngle : spreadAngle;
        Vector3 profileHint = profile != null ? profile.elbowHintOffset : elbowHintOffset;
        Vector3 profileShoulder = profile != null ? profile.shoulderOffset : shoulderOffset;

        // 좌우 대칭 보정. (왼쪽은 x를 뒤집는다)
        float side = isLeft ? -1.0f : 1.0f;

        // 모든 거리 계산은 월드 좌표로 한다. 캐릭터 로컬 좌표로 계산하면 스크립트가 붙은 오브젝트나 부모의
        // 축별 스케일·회전에 따라 거리가 줄거나 늘어, 타깃이 팔 길이보다 가깝게 놓일 수 있다.
        // 보정 값(어깨, 팔꿈치 힌트)과 벌림 각도는 캐릭터의 회전 방향만 따른다.
        Quaternion rootRotation = transform.rotation;

        // 이번 프레임의 최종 어깨 위치. 팔 IK는 위팔 뼈를 어깨 관절 기준으로 돌릴 뿐 어깨 위치는 바꾸지 않는다.
        Transform upper = isLeft ? _leftUpper : _rightUpper;
        Vector3 shoulder = upper.position + rootRotation * Mirror(profileShoulder, side);
        float armLength = MeasureArmLength(isLeft);
        if (isLeft) _leftArmLength = armLength;
        else _rightArmLength = armLength;
        float reach = armLength * profileReach;

        Vector3 direction = (aimTarget.position - shoulder).normalized;
        // 캐릭터 위쪽 축을 기준으로 바깥쪽으로 돌린다. (오른팔은 오른쪽, 왼팔은 왼쪽)
        direction = Quaternion.AngleAxis(profileSpread * side, rootRotation * Vector3.up) * direction;

        // 어깨 기준 오프셋. 부드럽게 따라가는 계산은 캐릭터 회전 기준으로 해서, 캐릭터가 돌아도 지연되지 않게 한다.
        Vector3 handOffset = Quaternion.Inverse(rootRotation) * (direction * reach);
        Vector3 hintOffset = Quaternion.Inverse(rootRotation) * (direction * (reach * 0.5f)) + Mirror(profileHint, side);

        // 처음에는 바로 목표 위치에 두고, 이후에는 프레임 속도와 무관하게 일정한 빠르기로 따라간다.
        bool hasTarget = isLeft ? _hasLeftTarget : _hasRightTarget;
        float t = hasTarget && followSharpness > 0.0f ? 1.0f - Mathf.Exp(-followSharpness * Time.deltaTime) : 1.0f;
        if (isLeft) _hasLeftTarget = true;
        else _hasRightTarget = true;

        // 방향이 바뀌는 도중 벡터 보간으로 길이가 짧아지면 팔이 잠깐 굽으므로, 보간 후 길이를 다시 맞춘다.
        handOffset = Vector3.Lerp(isLeft ? _leftHandLocal : _rightHandLocal, handOffset, t).normalized * reach;
        hintOffset = Vector3.Lerp(isLeft ? _leftHintLocal : _rightHintLocal, hintOffset, t);
        if (isLeft) { _leftHandLocal = handOffset; _leftHintLocal = hintOffset; }
        else { _rightHandLocal = handOffset; _rightHintLocal = hintOffset; }

        // 발사 반동. 부드럽게 따라가는 보정과 별개로 바로 적용해야 반동이 굼뜨지 않는다.
        float recoilRecover = profile != null ? profile.recoilRecoverSpeed : recoilRecoverSpeed;
        float recoil = isLeft ? _leftRecoil : _rightRecoil;
        recoil *= Mathf.Exp(-recoilRecover * Time.deltaTime);
        if (recoil < 0.001f) recoil = 0.0f;
        if (isLeft) _leftRecoil = recoil;
        else _rightRecoil = recoil;

        Vector3 finalHandOffset = handOffset;
        if (recoil > 0.0f)
        {
            float kickDistance = (profile != null ? profile.recoilDistance : recoilDistance) * recoil;
            float kickPitch = (profile != null ? profile.recoilPitch : recoilPitch) * recoil;

            // 오프셋은 캐릭터 회전 기준이라 위쪽은 Vector3.up이다. 팔 방향을 오른쪽 축 기준으로 위로 든다.
            Vector3 armDirection = handOffset.normalized;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, armDirection);
            if (pitchAxis.sqrMagnitude > 0.0001f)
            {
                armDirection = Quaternion.AngleAxis(-kickPitch, pitchAxis.normalized) * armDirection;
            }

            // Reach Ratio가 1보다 크면 타깃이 팔 길이 밖에 있어 조금 당겨서는 팔이 굽지 않으므로 실제 팔 길이 기준으로 당긴다.
            float kickedLength = Mathf.Min(handOffset.magnitude, armLength) - kickDistance;
            finalHandOffset = armDirection * Mathf.Max(0.0f, kickedLength);
        }

        handTarget.position = shoulder + rootRotation * finalHandOffset;

        if (elbowHint != null)
        {
            elbowHint.position = shoulder + rootRotation * hintOffset;
        }
    }

    private static Vector3 Mirror(Vector3 offset, float side)
    {
        return new Vector3(offset.x * side, offset.y, offset.z);
    }

    // [임시] 팔이 굽는 원인 판별용.
    // 타깃 거리 비율이 1보다 큰데 팔꿈치 각도가 180도에서 멀면, IK 이후 다른 무언가가 팔을 굽히고 있다는 뜻이다.
    // 타깃 거리 비율이 1보다 작으면 타깃 위치 계산 쪽 문제다.
    private string BuildDebugLine(bool isLeft)
    {
        Transform upper = isLeft ? _leftUpper : _rightUpper;
        Transform lower = isLeft ? _leftLower : _rightLower;
        Transform hand = isLeft ? _leftHand : _rightHand;
        Transform target = isLeft ? leftHandTarget : rightHandTarget;
        float armLength = isLeft ? _leftArmLength : _rightArmLength;
        if (upper == null || target == null || armLength <= 0.0f) return $"{(isLeft ? "L" : "R")}: -";

        float targetRatio = Vector3.Distance(upper.position, target.position) / armLength;
        float elbowAngle = Vector3.Angle(upper.position - lower.position, hand.position - lower.position);
        float handGap = Vector3.Distance(hand.position, target.position);
        float weight = rigAimController.GetNormalizedWeight(isLeft ? "ArmLAim" : "ArmRAim");

        // 스크립트가 이번 프레임에 정한 어깨 기준 오프셋과 실제 타깃 위치의 차이.
        // 0이 아니면 스크립트가 옮긴 뒤에 다른 무언가가 타깃을 되돌리고 있다는 뜻이다.
        Vector3 setOffset = transform.rotation * (isLeft ? _leftHandLocal : _rightHandLocal);
        float overwriteGap = Vector3.Distance(target.position - upper.position, setOffset);
        string targetName = target.name + (target.GetComponent<UnityEngine.Animations.Rigging.RigTransform>() != null ? "(RigTransform O)" : "(RigTransform X)");

        // IK 제약이 실제로 참조하는 타깃이 이 스크립트가 옮기는 타깃과 같은지 확인한다.
        string constraintName = isLeft ? "ArmLAim_IK" : "ArmRAim_IK";
        string constraintTarget = "제약 없음";
        foreach (var ik in GetComponentsInChildren<UnityEngine.Animations.Rigging.TwoBoneIKConstraint>())
        {
            if (ik.gameObject.name != constraintName) continue;
            Transform ikTarget = ik.data.target;
            constraintTarget = ikTarget == target ? "제약 타깃 일치" : $"제약 타깃 불일치({(ikTarget != null ? ikTarget.name : "없음")})";
            break;
        }
        targetName += $", {constraintTarget}";

        ArmIKProfile profile = GetEquippedProfile(isLeft);
        string profileInfo = profile != null
            ? $"{profile.name} (Reach {profile.reachRatio:F2}, Shoulder {profile.shoulderOffset})"
            : $"프로필 없음 → 기본값 (Reach {reachRatio:F2}, Shoulder {shoulderOffset})";

        float setRatio = setOffset.magnitude / armLength;
        Vector3 scale = transform.lossyScale;

        return $"{(isLeft ? "L" : "R")}: 타깃거리비율 {targetRatio:F2} (스크립트 설정 {setRatio:F2}, 되돌림 {overwriteGap:F2}m), " +
               $"팔꿈치각도 {elbowAngle:F0}°, 손-타깃 {handGap:F2}m, IK가중치 {weight:F2} | {targetName} | {profileInfo} | " +
               $"스크립트 오브젝트 {name} 스케일({scale.x:F2}, {scale.y:F2}, {scale.z:F2})";
    }

    private void OnGUI()
    {
        if (!showDebug || !_hasBones) return;

        // 한 줄이 길어 잘리므로 " | " 단위로 줄을 나눠 표시한다.
        float y = 50.0f;
        foreach (bool isLeft in new[] { true, false })
        {
            foreach (string line in BuildDebugLine(isLeft).Split(" | "))
            {
                GUI.Label(new Rect(20.0f, y, 1600.0f, 22.0f), line);
                y += 20.0f;
            }
            y += 8.0f;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || !_hasBones) return;

        Gizmos.color = Color.yellow;
        if (leftHandTarget != null) Gizmos.DrawLine(_leftUpper.position, leftHandTarget.position);
        if (rightHandTarget != null) Gizmos.DrawLine(_rightUpper.position, rightHandTarget.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_leftUpper.position, _leftLower.position);
        Gizmos.DrawLine(_leftLower.position, _leftHand.position);
        Gizmos.DrawLine(_rightUpper.position, _rightLower.position);
        Gizmos.DrawLine(_rightLower.position, _rightHand.position);
    }
}
