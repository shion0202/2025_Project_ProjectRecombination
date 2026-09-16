using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepListener : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;

    [Header("조건 확인")]
    [SerializeField] private GameObject targetLegs;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private Transform groundCheckOrigin; // 발 위치나 캐릭터 중심

    [Header("중복 문제 방지")]
    [SerializeField] private float minStepInterval = 0.1f; // 이 시간보다 짧게 다시 호출되면 무시
    private float _lastStepTime = -999f;

    // 이동 애니메이션이 있는 레이어 (Base Layer)
    private const int MoveLayerIndex = 0;

    private Animator _animator;
    private readonly List<AnimatorClipInfo> _clipInfos = new();

    private void Awake()
    {
        // 애니메이션 이벤트는 Animator와 같은 오브젝트의 컴포넌트로 전달되므로 같은 오브젝트에서 찾는다.
        _animator = GetComponent<Animator>();
    }

    // 애니메이션 이벤트에서 호출할 함수
    public void PlayFootstep(AnimationEvent animationEvent)
    {
        // 블렌드 트리는 섞이는 클립마다 이벤트를 보낸다. (대각선 이동, 사격 중 이동 전환 등)
        // 클립마다 이벤트 시점이 조금씩 달라 중복 방지 간격을 벗어나면 한 걸음에 두 번 울리므로,
        // 가장 크게 섞인 클립이 보낸 이벤트만 처리한다.
        if (!IsDominantClip(animationEvent.animatorClipInfo.clip)) return;

        // 중복 호출 방지
        float now = Time.time;
        if (now - _lastStepTime < minStepInterval)
            return; // 너무 빨리 또 호출됐으면 무시

        _lastStepTime = now;

        // 클립 없으면 또는 targetLegs가 비활성화 상태일 경우 반환
        if (footstepClips == null || footstepClips.Length == 0 || !targetLegs.activeSelf) return;

        // 공중에 떠 있을 때는 재생 안 함 (점프 중 등)
        //if (!IsGrounded()) return;

        // 랜덤 발소리 재생
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }

    private bool IsDominantClip(AnimationClip eventClip)
    {
        // 판단할 정보가 없으면 기존처럼 재생한다.
        if (_animator == null || eventClip == null) return true;

        AnimationClip dominantClip = null;
        float maxWeight = -1.0f;

        _animator.GetCurrentAnimatorClipInfo(MoveLayerIndex, _clipInfos);
        FindDominantClip(ref dominantClip, ref maxWeight);

        // 상태 전이 중에는 다음 상태의 클립도 이벤트를 보내므로 함께 비교한다.
        if (_animator.IsInTransition(MoveLayerIndex))
        {
            _animator.GetNextAnimatorClipInfo(MoveLayerIndex, _clipInfos);
            FindDominantClip(ref dominantClip, ref maxWeight);
        }

        return dominantClip == null || dominantClip == eventClip;
    }

    private void FindDominantClip(ref AnimationClip dominantClip, ref float maxWeight)
    {
        foreach (AnimatorClipInfo info in _clipInfos)
        {
            // 비중이 같으면 먼저 찾은 클립을 유지해 두 클립이 모두 통과하지 않게 한다.
            if (info.weight > maxWeight)
            {
                maxWeight = info.weight;
                dominantClip = info.clip;
            }
        }
    }

    private bool IsGrounded()
    {
        Transform origin = groundCheckOrigin != null ? groundCheckOrigin : transform;
        Ray ray = new Ray(origin.position + Vector3.up * 0.1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundMask))
        {
            // 필요하다면 여기서 땅 종류에 따라 다른 소리 재생도 가능
            return true;
        }

        return false;
    }
}
