# 팔 사격 포즈 문제 해결 방안 (한 팔 / 양팔 공용)

## 1. 문제 상황
- 록맨식 팔 파츠 사격 게임. 한 팔 사격과 양팔 사격이 모두 필요함.
- 가지고 있는 건 **한 팔 사격 애니메이션**뿐인데, 이 클립은 사격하면서 **허리(척추)를 비틀어** 팔을 앞으로 보냄.
- 이 클립을 붙이고 IK를 걸었지만, 양팔을 뻗으면 팔이 앞이 아니라 **옆으로 벌어짐**. IK 값을 조정해도 해결되지 않음.

## 2. 근본 원인
- IK(Two Bone IK / Humanoid IK)는 **상완 → 전완 → 손** 두 마디만 푼다.
- **척추(Spine/Chest/UpperChest)와 쇄골(Shoulder)은 건드리지 않는다.**
- 클립의 허리 회전이 남아 있으면 가슴이 돌아가서 한쪽 어깨는 앞, 다른 쪽 어깨는 뒤에 위치한다.
- IK는 정상적으로 풀려도 **출발점인 어깨가 틀어져 있어서** 팔이 벌어져 보인다.
- 따라서 IK 파라미터 조정으로는 해결할 수 없다. **허리를 먼저 펴고 → 그 다음에 IK를 푸는 순서**가 필요하다.

## 3. 하지 말아야 할 것 (지금까지 실패한 방향)
- IK weight, hint, 타깃 위치만 계속 조정하기 → 원인이 IK 밖에 있으므로 효과 없음.
- 사격 클립을 척추가 포함된 마스크(또는 Base Layer 전신)로 재생하기.
- IK 타깃을 **가슴/어깨 본의 자식**으로 두거나 **어깨 본의 forward** 기준으로 계산하기 → 타깃이 비틀림을 따라감.
- **LateUpdate에서 허리를 펴기** → IK는 이미 틀어진 어깨 기준으로 풀린 뒤라서, 허리를 펴는 순간 어깨가 이동하며 손이 어긋남.
- `OnAnimatorIK` 안에서 `GetBoneTransform().position`으로 어깨 위치 읽기 → 이전 프레임 포즈가 나올 수 있음.

## 4. 먼저 점검할 것
1. 사격 클립 레이어의 Avatar Mask에서 **Body(척추)가 꺼져 있는가?**
2. 사격 클립이 Base Layer에서 전신으로 재생되고 있지 않은가?
3. IK 타깃의 부모가 **캐릭터 루트**인가? (가슴/어깨 본이면 안 됨)
4. (Animation Rigging) 허리/쇄골 보정 제약이 IK보다 **먼저** 평가되는가?

## 5. 해결 구성

### ① 레이어 분리
| 레이어 | 블렌딩 | 마스크 | 내용 |
|---|---|---|---|
| Base Layer | - | 전신 | 이동/대기. **허리는 여기서만 결정** |
| Arm Layer | Override | **사격하는 팔 + 손만** (Body, 쇄골 끔) | 한 팔 사격 클립. **IK Pass 켬** |

- 이렇게 하면 허리 비틀림은 빠지고, 팔은 사선 방향으로 남는다. 이걸 IK로 정면에 맞춘다.
- 양팔 사격도 같은 구조에서 IK weight만 양쪽 다 1로 올리면 된다.

### ② IK 타깃은 캐릭터 루트 기준 고정 오프셋으로 계산 (Humanoid 내장 IK 기준)
```csharp
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ArmShootIK : MonoBehaviour
{
    // 바인드 포즈에서 캐릭터 루트 기준으로 측정한 어깨 위치
    [SerializeField] Vector3 rightShoulderLocal = new(0.18f, 1.40f, 0f);
    [SerializeField] Vector3 leftShoulderLocal  = new(-0.18f, 1.40f, 0f);
    [SerializeField] float reach = 0.55f;   // 어깨 → 손 거리 (팔 파츠별로 조정)

    // 한 팔: rightWeight = 1, leftWeight = 0 / 양팔: 둘 다 1
    public float rightWeight, leftWeight;

    Animator animator;
    void Awake() => animator = GetComponent<Animator>();

    void OnAnimatorIK(int layerIndex)
    {
        Solve(AvatarIKGoal.RightHand, rightShoulderLocal, rightWeight);
        Solve(AvatarIKGoal.LeftHand,  leftShoulderLocal,  leftWeight);
    }

    void Solve(AvatarIKGoal goal, Vector3 shoulderLocal, float w)
    {
        Vector3 shoulder = transform.TransformPoint(shoulderLocal);
        Vector3 target = shoulder + transform.forward * reach;
        animator.SetIKPositionWeight(goal, w);
        animator.SetIKPosition(goal, target);
    }
}
```
- 손 회전은 처음엔 weight 0으로 두고 **위치부터** 맞춘다. 위치가 맞은 뒤에 회전 오프셋을 찾아 추가한다.
- 팔 파츠마다 길이나 총구 위치가 다르면 `reach`와 오프셋만 교체하면 된다.

### ③ Animation Rigging을 쓰는 경우
- Rig 계층 순서: **척추/쇄골 보정 제약(Override Transform 또는 Multi-Rotation)을 위에, Two Bone IK를 아래에** 둔다. 같은 Rig 안에서는 위에서 아래 순서로 평가된다.
- IK 타깃 오브젝트는 **캐릭터 루트의 자식**으로 둔다.
- 한 팔 사격에서 허리 비틀림을 일부 살리고 싶으면, 척추 회전 제약을 따로 두고 양팔 모드에서는 그 weight만 0으로 한다.

### ④ 마무리 보정 (자연스러움)
- 허리를 펴면 양팔 뻗은 모습이 뻣뻣해 보일 수 있다.
- 쇄골을 앞으로 5~10도 내밀어 주면(protraction) 허리 회전이 하던 역할을 일부 대신해서 자연스러워진다.
- Humanoid: `OnAnimatorIK` 안에서 `animator.SetBoneLocalRotation(HumanBodyBones.RightShoulder / LeftShoulder, ...)`
- Animation Rigging: 쇄골용 Multi-Rotation 제약을 IK보다 위에 둔다.

## 6. 검증 순서
1. IK weight를 0으로 두고 사격 → **허리가 비틀리지 않는지** 먼저 확인 (팔만 사선으로 나오면 정상)
2. Scene 뷰에서 IK 타깃 위치를 Gizmo로 그려서 **양 어깨 정면에 평행하게** 찍히는지 확인
3. IK weight 1 → 손이 타깃에 붙는지 확인
4. 양팔 모드(둘 다 1)에서 팔이 평행하게 앞을 향하는지 확인
5. 쇄골 보정을 추가하고 외형 조정

## 7. 여전히 안 되면 확인할 정보
- 사용 중인 IK 방식: Humanoid 내장 IK / Animation Rigging
- Animator 레이어 구성과 각 레이어의 Avatar Mask 설정
- IK 타깃 오브젝트의 부모와 위치 계산 방식
- 허리 보정을 어디서(LateUpdate / OnAnimatorIK / Rig 제약) 하고 있는지
