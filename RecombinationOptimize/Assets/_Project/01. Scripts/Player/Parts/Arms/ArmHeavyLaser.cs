using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Monster;

public class ArmHeavyLaser : PartBaseArm
{
    [Header("화력형 레이저 설정")]
    [SerializeField] protected int maxPenetration = 1;

    protected override void Awake()
    {
        base.Awake();
        //originalColor = laserLineRenderer.material.color;
    }

    protected override void Shoot()
    {
        RaycastHit[] hits;
        Vector3 targetPoint = GetTargetPoint(out hits);

        // 카메라 -> 레이저 발사 방향
        Vector3 startPos = bulletSpawnPoint.position;
        Vector3 direction = (targetPoint - startPos).normalized;

        // 히트 정렬 (가까운 순으로)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // 라인 시작점
        //laserLineRenderer.SetPosition(0, startPos);

        int hitCount = 0;
        Vector3 endPosition = startPos + direction * shootingRange;

        foreach (RaycastHit hit in hits)
        {
            // 타격 처리
            TakeDamage(hit.transform);

            // 데미지 이외의 공격(충돌) 판정
            Destroy(Instantiate(bulletPrefab, hit.point, Quaternion.identity), 0.1f);

            ++hitCount;
            endPosition = hit.point; // 라인렌더러 종료 지점 갱신

            // 관통 제한 체크
            if (hitCount > maxPenetration) break;
        }

        // 라인 최종 길이 반영
        //laserLineRenderer.SetPosition(1, endPosition);
        //laserLineRenderer.enabled = true;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        fadeCoroutine = StartCoroutine(CoFadeOutLaser());

        // 반동 적용
        _owner.ApplyRecoil(impulseSource, recoilX, recoilY);
    }
}
