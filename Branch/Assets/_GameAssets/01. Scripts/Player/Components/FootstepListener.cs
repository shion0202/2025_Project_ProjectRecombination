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

    // 애니메이션 이벤트에서 호출할 함수
    public void PlayFootstep()
    {
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
