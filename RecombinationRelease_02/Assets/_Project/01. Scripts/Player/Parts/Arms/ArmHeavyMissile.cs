using Managers;
using Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmHeavyMissile : PartBaseArm
{
    [Header("발사 범위")]
    [SerializeField] protected float maxYawAngle = 90f; // 좌우 방향 최대 90도씩 = 180도 범위
    [SerializeField] protected float maxPitchAngle = 10f; // 상하 각도 범위 (조절 가능)
    [SerializeField] protected float targetRadius = 5f;  // 타겟 주변 랜덤 목표 위치 반경
    protected Transform _currentTarget = null;
    protected SkinnedMeshRenderer smr;
    protected Coroutine _morphBlendRoutine = null;

    protected void OnEnable()
    {
        GUIManager.Instance.SetAmmoColor(partType, Color.red);
        Managers.GUIManager.Instance.SetAmmoColor(partType, false);
        _currentShootTime = (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value);
    }

    protected override void Update()
    {
        if (partType == EPartType.ArmL)
        {
            GUIManager.Instance.SetAmmoLeftSlider(_currentShootTime, (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value));
        }
        else
        {
            GUIManager.Instance.SetAmmoRightSlider(_currentShootTime, (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value));
        }

        _currentShootTime += Time.deltaTime;

        if (!_isShooting)
        {
            if (_currentAmmo >= maxAmmo) return;

            _currentReloadTime -= Time.deltaTime;
            if (_currentReloadTime > 0.0f) return;

            _currentAmmo = Mathf.Clamp(_currentAmmo + 1, 0, maxAmmo);
            _currentReloadTime = reloadTime;
            if (_currentAmmo >= maxAmmo)
            {
                _isOverheat = false;
            }

            return;
        }
        if ((_owner.CurrentPlayerState & EPlayerState.Rotating) != 0) return;

        if (_currentAmmo <= 0) return;
        if (_currentShootTime >= (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value))
        {
            Shoot();
            _currentShootTime = 0.0f;
        }
    }

    protected override void Shoot()
    {
        if (_morphBlendRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
        }
        _morphBlendRoutine = StartCoroutine(CoStartMorphBlend());

        _currentTarget = FindTargetInView();

        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);

        // 타겟 주변 반경 내 랜덤 위치 생성
        Vector3 randomOffset = Random.insideUnitSphere * targetRadius;
        randomOffset.y = 0;  // 수평 오차만 적용하려면 y축 제거

        Vector3 randomizedTargetPoint = targetPoint + randomOffset;

        Vector3 camShootDirection = (randomizedTargetPoint - bulletSpawnPoint.position).normalized;

        Vector3 randomDir = GetRandomDirection(camShootDirection);

        GameObject bullet = Utils.Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(randomDir));
        Bullet bulletComp = bullet.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.Init(_owner.gameObject, _currentTarget, bulletSpawnPoint.position, randomizedTargetPoint, randomDir,
                (int)_owner.Stats.CombinedPartStats[partType][EStatType.Damage].value);
        }

        _owner.ApplyRecoil(impulseSource, recoilX, recoilY);

        _currentAmmo = Mathf.Clamp(_currentAmmo - 1, 0, maxAmmo);
        if (_currentAmmo <= 0)
        {
            CancleShootState(partType == EPartType.ArmL ? true : false);
            _isOverheat = true;
            GUIManager.Instance.SetAmmoColor(partType, true);
        }
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

    protected Transform FindTargetInView()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Camera cam = Camera.main;
        Transform bestTarget = null;
        float bestViewportDistance = float.MaxValue;

        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        foreach (var enemy in enemies)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(enemy.transform.position);

            // 적이 카메라 앞에 있고 화면 내에 있어야 함
            bool inFront = viewportPos.z > 0;
            bool inView = viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1;

            if (inFront && inView)
            {
                // 카메라와 적 간 거리 (사거리) 체크
                float distanceToCam = Vector3.Distance(cam.transform.position, enemy.transform.position);
                if (distanceToCam <= shootingRange)
                {
                    // 뷰포트 중앙과 적 위치 간 거리 계산
                    Vector2 enemyViewportPos2D = new Vector2(viewportPos.x, viewportPos.y);
                    float viewportDistance = Vector2.Distance(screenCenter, enemyViewportPos2D);

                    // 중앙과 가장 가까운 적 선택
                    if (viewportDistance < bestViewportDistance)
                    {
                        bestViewportDistance = viewportDistance;
                        bestTarget = enemy.transform;
                    }
                }
            }
        }

        return bestTarget;
    }

    protected IEnumerator CoStartMorphBlend(float time = 0.1f)
    {
        SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();
        float min = 0.0f;
        float max = 100.0f;

        float elapsed = min;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float weight = Mathf.Lerp(min, max, elapsed / time);
            smr.SetBlendShapeWeight(0, weight);
            yield return null;
        }

        smr.SetBlendShapeWeight(0, max);

        _morphBlendRoutine = StartCoroutine(CoEndMorphBlend(time));
    }

    protected IEnumerator CoEndMorphBlend(float time)
    {
        SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();
        float min = 100.0f;
        float max = 0.0f;

        float elapsed = min;
        while (elapsed < time)
        {
            elapsed -= Time.deltaTime;
            float weight = Mathf.Lerp(min, max, elapsed / time);
            smr.SetBlendShapeWeight(0, weight);
            yield return null;
        }

        smr.SetBlendShapeWeight(0, max);
        _morphBlendRoutine = null;
    }
}
