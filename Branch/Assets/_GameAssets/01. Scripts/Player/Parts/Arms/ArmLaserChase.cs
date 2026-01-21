using Monster.AI;
using Cinemachine;
using Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmLaserChase : PartBaseArm
{
    [Header("곡선 레이저 설정")]
    [SerializeField] protected float upOffset = 2.0f;
    [SerializeField] protected float maxSideOffset = 5.0f;      // 곡선 세기의 최대값
    [SerializeField] protected float crosshairRadius = 0.1f;    // 0.1 = 화면의 10% 반경(조절 가능)
    [SerializeField] protected string enemyTag = "Enemy";
    protected Transform currentTarget = null;

    protected override void Update()
    {
        _currentShootTime -= Time.deltaTime;
        if ((_owner.CurrentPlayerState & EPlayerState.Rotating) != 0) return;
        if (!_isShooting) return;
        if (_currentAmmo <= 0) return;

        Shoot();
    }

    public override void UseAbility()
    {
        base.UseAbility();
        //laserLineRenderer.gameObject.SetActive(true);
    }

    public override void UseCancleAbility()
    {
        base.UseCancleAbility();
        //laserLineRenderer.gameObject.SetActive(false);
    }

    // Update에서 실행
    protected override void Shoot()
    {
        // 1. 타겟 해제 조건 확인(마우스 입력 해제/타겟 아웃/죽음 등)
        if (currentTarget != null)
        {
            bool outOfScreen = IsTargetOutOfScreen(currentTarget);
            bool destroyed = !currentTarget.gameObject.activeInHierarchy; // 또는 HP, disable 등 구체화
            bool inputUp = Input.GetMouseButtonUp(0); // 마우스 왼쪽 버튼 해제 시
            bool isDie = false;

            // TODO: AIController를 통해 몬스터 상태 확인
            AIController monster = currentTarget.GetComponent<AIController>();
            if (monster != null)
            {
                isDie = monster.Blackboard.State.HasState("Death");
            }
            else
            {
                monster = currentTarget.GetComponentInParent<AIController>();
                if (monster != null)
                {
                    isDie = monster.Blackboard.State.HasState("Death");
                }
            }

            if (outOfScreen || destroyed || inputUp || isDie)
                currentTarget = null;
        }

        // 2. 타겟이 없다면 크로스헤어 내에서 가장 가까운 적 탐색
        if (currentTarget == null)
        {
            currentTarget = FindClosestEnemyWithinCrosshair();
        }

        // 3. 곡선 빔 그리기 (타겟이 유효할 때)
        if (currentTarget != null)
        {
            DrawCurvedLaser(currentTarget.position);

            if (_currentShootTime <= 0.0f)
            {
                TakeDamage(currentTarget);
                _currentShootTime = (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value);
            }
        }
        else
        {
            // 빔 비활성화 등(옵션)
            //laserLineRenderer.positionCount = 0;
        }
    }

    Transform FindClosestEnemyWithinCrosshair()
    {
        var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform closest = null;
        float minScreenDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(enemy.transform.position);
            if (viewportPos.z > 0)
            {
                Vector2 center = new Vector2(0.5f, 0.5f);
                Vector2 enemyXY = new Vector2(viewportPos.x, viewportPos.y);
                float dist = Vector2.Distance(center, enemyXY);

                if (dist < crosshairRadius && dist < minScreenDist)
                {
                    // Raycast로 시야 가려짐 검증
                    Vector3 dir = enemy.transform.position - Camera.main.transform.position;
                    if (Physics.Raycast(Camera.main.transform.position, dir.normalized, out RaycastHit hit, 100f))
                    {
                        // 적 본체 또는 자식 오브젝트가 맞았을 때만 인정
                        if (hit.collider != null &&
                            (hit.collider.gameObject == enemy.gameObject ||
                             hit.collider.transform.IsChildOf(enemy.transform)))
                        {
                            closest = enemy.transform;
                            minScreenDist = dist;
                        }
                    }
                }
            }
        }
        return closest;
    }

    // 타겟이 화면 범위 내에 있는지 확인 (예시)
    bool IsTargetOutOfScreen(Transform target)
    {
        Vector3 vp = Camera.main.WorldToViewportPoint(target.position);
        // 화면 앞에 있고, 0~1 사이에 모두 포함돼 있으면 true/false 반환
        return (vp.z < 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1);
    }

    // 곡선(베지어) 빔 연출
    void DrawCurvedLaser(Vector3 targetPos)
    {
        Vector3 p0 = bulletSpawnPoint.position;
        Vector3 p2 = targetPos + Vector3.up * 0.5f;

        Vector3 cameraRight = Camera.main.transform.right;
        Vector3 dirToTarget = (p2 - Camera.main.transform.position).normalized;
        float sideAmt = Vector3.Dot(dirToTarget, cameraRight);
        float appliedSideOffset = maxSideOffset * -sideAmt; // (왼쪽-오른쪽 자연스러운 방향)

        Vector3 midPoint = (p0 + p2) * 0.5f + Vector3.up * upOffset + cameraRight * appliedSideOffset;

        int segmentCount = 20;
        //laserLineRenderer.positionCount = segmentCount + 1;
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 bezierPoint =
                Mathf.Pow(1 - t, 2) * p0
                + 2 * (1 - t) * t * midPoint
                + Mathf.Pow(t, 2) * p2;
            //laserLineRenderer.SetPosition(i, bezierPoint);
        }
    }
}
