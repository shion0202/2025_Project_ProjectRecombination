using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Monster;
using Monster.AI.Blackboard;
using Managers;

public class ShoulderRapid : PartBaseShoulder
{
    [SerializeField] protected GameObject missilePrefab;
    [SerializeField] protected GameObject targetingPrefab; // 타겟팅 표시용 프리팹
    [SerializeField] protected int maxTargetCount = 12;
    [SerializeField] private float particleStopDelay = 0.9f;  // Inspector에서 조절 가능
    private Coroutine _skillCoroutine = null;
    private List<GameObject> targetingInstances = new List<GameObject>();

    [SerializeField] protected float maxYawAngle = 90f; // 좌우 방향 최대 90도씩 = 180도 범위
    [SerializeField] protected float maxPitchAngle = 10f; // 상하 각도 범위 (조절 가능)

    public override void UseAbility()
    {
        LaunchTargetMissiles();
    }

    private void LaunchTargetMissiles()
    {
        // 스킬 시전 하는 동안 카메라, 플레이어 이동 불가
        // 캐릭터가 카메라 방향을 바라봄
        // 화면 범위 내의 적을 조준(타겟팅), 최대 수치까지 타겟팅 가능
        // 타겟팅된 적에게 미사일 발사, 최대 수치가 아닐 경우 남은 미사일은 타겟팅된 적들에게 균등 분배
        if (_skillCoroutine != null) return;
        _skillCoroutine = StartCoroutine(CoLaunchTargetMissiles());
    }

    protected void LookCameraDirection()
    {
        Camera cam = Camera.main;
        Vector3 lookDirection = cam.transform.forward;
        lookDirection.y = 0; // 수평 방향으로만 회전
        if (lookDirection != Vector3.zero)
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    List<TargetPoint> FindValidTargets(LayerMask obstacleMask, float maxRange)
    {
        Camera cam = Camera.main;
        List<TargetPoint> result = new List<TargetPoint>();
        TargetPoint[] allEnemies = FindObjectsOfType<TargetPoint>();

        foreach (var enemy in allEnemies)
        {
            Vector3 viewPos = cam.WorldToViewportPoint(enemy.transform.position);
            if (viewPos.z > 0 && viewPos.x > 0 && viewPos.x < 1 && viewPos.y > 0 && viewPos.y < 1)
            {
                if (viewPos.z <= cam.farClipPlane) // 카메라 시야거리 제한
                {
                    if (Vector3.Distance(_owner.transform.position, enemy.transform.position) <= maxRange)
                    {
                        if (IsVisibleFromCamera(enemy.transform, cam, obstacleMask))
                        {
                            result.Add(enemy);
                        }
                    }
                }
            }
        }
        return result;
    }

    bool IsVisibleFromCamera(Transform enemyTransform, Camera cam, LayerMask obstacleMask)
    {
        Vector3 direction = enemyTransform.position - cam.transform.position;
        float distance = direction.magnitude;
        direction.Normalize();

        if (Physics.Raycast(cam.transform.position, direction, out RaycastHit hit, distance, obstacleMask))
        {
            // 장애물이 적 앞에 있음
            if (!hit.transform.GetComponentInChildren<TargetPoint>())
                return false;
        }
        return true;
    }

    protected Vector3 GetRandomDirection(Vector3 forward)
    {
        // 좌우 yaw -maxYaw ~ +maxYawdeg, 상하 pitch -maxPitch ~ +maxPitchdeg
        float roll = Random.Range(0.0f, maxPitchAngle);
        float yaw = Random.Range(-maxYawAngle, maxYawAngle);
        float pitch = Random.Range(0.0f, maxPitchAngle);
        Quaternion rot = Quaternion.Euler(pitch, yaw, roll);
        return rot * forward;
    }

    private IEnumerator CoLaunchTargetMissiles()
    {
        GUIManager.Instance.SetBackSkillIcon(true);
        // 1. 스킬 시전 중 플레이어와 카메라 조작 불가
        _owner.FollowCamera.SetCameraRotatable(false);
        _owner.SetMovable(false);

        // 2. 플레이어가 카메라 방향 바라봄
        LookCameraDirection();

        // 3. 화면 내의 적을 감지(카메라 시야각/범위 외 적 제외)
        LayerMask mask = 0;
        mask |= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        List<TargetPoint> targets = FindValidTargets(mask, 100.0f);
        if (targets.Count > maxTargetCount)
            targets = targets.GetRange(0, maxTargetCount);

        // 4. 타겟마다 targetingPrefab 생성(시각적 타겟 표시)
        foreach (var enemy in targets)
        {
            TargetPoint targetPos = enemy.GetComponentInChildren<TargetPoint>();
            Vector3 targetPoint = enemy.transform.position;
            if (targetPos != null)
            {
                targetPoint = targetPos.transform.position;
            }

            GameObject targeting = Instantiate(targetingPrefab, targetPoint, Quaternion.identity, enemy.transform);
            targetingInstances.Add(targeting);

            ParticleSystem ps = targeting.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                StartCoroutine(StopParticleAfterDelay(ps));
            }

            // 도착 후 잠깐 멈춤
            yield return new WaitForSeconds(0.2f);
        }

        // 6. 각 타겟에게 유도 미사일 발사
        int targetCount = targets.Count;
        if (targetCount <= 0)
        {
            _owner.FollowCamera.SetCameraRotatable(true);
            _owner.SetMovable(true);
            _skillCoroutine = null;

            GUIManager.Instance.SetBackSkillIcon(false);
            GUIManager.Instance.SetBackSkillCooldown(false);
            yield break;
        }

        int missilesPerTarget = maxTargetCount / targetCount; // 기본 분배 수
        int remainder = maxTargetCount % targetCount;          // 나머지 미사일 수

        for (int i = 0; i < targetCount; i++)
        {
            int missilesToFire = missilesPerTarget + (i < remainder ? 1 : 0); // 나머지는 앞 타겟에 1개씩 분배

            TargetPoint enemy = targets[i];

            for (int j = 0; j < missilesToFire; j++)
            {
                Vector3 targetPoint = enemy.transform.position;
                Vector3 camShootDirection = (targetPoint - transform.position).normalized;
                Vector3 randomDir = GetRandomDirection(camShootDirection);

                GameObject missile = Instantiate(missilePrefab, _owner.transform.position, Quaternion.LookRotation(randomDir));
                var missileComp = missile.GetComponent<Missile>();
                if (missileComp != null)
                {
                    missileComp.Init(_owner.gameObject, enemy.transform, transform.position, targetPoint, randomDir, 100.0f);
                }
            }
        }

        // 타겟팅 프리팹 제거 (시각 효과 종료)
        foreach (var inst in targetingInstances)
        {
            Destroy(inst);
        }

        // 7. 플레이어와 카메라의 조작 재개
        _owner.FollowCamera.SetCameraRotatable(true);
        _owner.SetMovable(true);

        float time = 5.0f;
        GUIManager.Instance.SetBackSkillCooldown(true);
        GUIManager.Instance.SetBackSkillCooldown(time);
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            time -= 0.1f;
            GUIManager.Instance.SetBackSkillCooldown(time);
            if (time <= 0.0f)
            {
                break;
            }
        }

        GUIManager.Instance.SetBackSkillIcon(false);
        GUIManager.Instance.SetBackSkillCooldown(false);
        Debug.Log("쿨타임 종료");
        _skillCoroutine = null;
    }

    private IEnumerator StopParticleAfterDelay(ParticleSystem ps)
    {
        yield return new WaitForSeconds(particleStopDelay);

        if (ps != null)
        {
            ps.Pause();
        }
    }
}
