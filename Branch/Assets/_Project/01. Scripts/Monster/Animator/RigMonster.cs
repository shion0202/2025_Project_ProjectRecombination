using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigMonster : MonoBehaviour
{
    [SerializeField] private Rig hitRecoilRig; // 2번에서 만든 Rig 레이어 (예: HitRecoilRig)
    [SerializeField] private Transform spineIKTarget; // 1번에서 만든 척추 IK 타겟
    [SerializeField] private Transform originalSpineTarget; // 척추의 기본 위치 (IK 타겟의 부모)

    [SerializeField] private float recoilForce = 0.5f;
    [SerializeField] private float recoilDuration = 0.3f; // 움찔하는 총 시간

    private Vector3 _recoilTargetPosition;
    private float _recoilTimer;

    void Start()
    {
        // Rig의 가중치를 0으로 시작 (IK 비활성화)
        hitRecoilRig.weight = 0f;
        _recoilTargetPosition = spineIKTarget.localPosition; // 로컬 위치 기준
    }

    void Update()
    {
        if (_recoilTimer > 0)
        {
            _recoilTimer -= Time.deltaTime;

            // Rig 가중치를 1로 올렸다가 (recoilDuration의 절반)
            // 다시 0으로 내립니다. (recoilDuration의 나머지 절반)
            float halfDuration = recoilDuration / 2f;
            if (_recoilTimer > halfDuration)
            {
                // IK 켜기 (0 -> 1)
                hitRecoilRig.weight = Mathf.Lerp(1f, 0f, (_recoilTimer - halfDuration) / halfDuration);
            }
            else
            {
                // IK 끄기 (1 -> 0)
                hitRecoilRig.weight = Mathf.Lerp(0f, 1f, _recoilTimer / halfDuration);
            }

            // IK 타겟을 원래 위치로 부드럽게 복귀
            spineIKTarget.localPosition = Vector3.Lerp(_recoilTargetPosition, Vector3.zero, 1f - (_recoilTimer / recoilDuration));
        }
        else
        {
            hitRecoilRig.weight = 0f; // 확실하게 끔
        }
    }

    // 외부(예: FSM이나 데미지 시스템)에서 이 함수를 호출
    public void TakeHit(Vector3 hitDirection)
    {
        // 피격 방향의 반대 + 힘 만큼 IK 타겟을 밀어냄
        // (좌표계에 따라 hitDirection을 변환해야 할 수 있음)
        Vector3 recoilDir = originalSpineTarget.InverseTransformDirection(-hitDirection.normalized);
        _recoilTargetPosition = recoilDir * recoilForce;

        spineIKTarget.localPosition = _recoilTargetPosition;
        _recoilTimer = recoilDuration;
    }
}
