using Managers;
using Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmHeavyShotgun : PartBaseArm
{
    [Header("샷건 설정")]
    [SerializeField] protected GameObject muzzleFlashPrefab;
    [SerializeField] private int pelletCount = 12;              // 펠릿 총 개수
    [SerializeField] private float denseSpreadAngle = 5f;       // 밀집 구간 각도
    [SerializeField] private float denseRange = 10f;            // 밀집 구간 최대 거리
    [SerializeField] private float spreadAngle = 25f;           // 확산 최대 각도
    [SerializeField] private float maxRange = 20f;              // 전체 사거리
    [SerializeField] private List<AudioClip> shootClips = new();
    protected GameObject muzzleFlashEffect;
    protected AudioSource _audioSource;
    protected Coroutine _soundRoutine = null;

    protected override void Awake()
    {
        base.Awake();
        _audioSource = GetComponent<AudioSource>();
    }

    protected void OnEnable()
    {
        GUIManager.Instance.GameUIController.SetAmmoColor(partType, Color.red);
        GUIManager.Instance.GameUIController.SetAmmoColor(partType, false);

        if (_owner && !_owner.Stats.CombinedPartStats[partType].IsEmpty() && _owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots] != null)
        {
            _currentShootTime = (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value);
        }

        _damagedTargets.Clear();
    }

    protected override void Update()
    {
        if (partType == EPartType.ArmL)
        {
            GUIManager.Instance.GameUIController.SetAmmoLeftSlider(_currentShootTime, (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value));
        }
        else
        {
            GUIManager.Instance.GameUIController.SetAmmoRightSlider(_currentShootTime, (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value));
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

    public override void FinishActionForced()
    {
        base.FinishActionForced();
        
        if (_soundRoutine != null)
        {
            StopCoroutine(_soundRoutine);
            _soundRoutine = null;
        }

        _damagedTargets.Clear();
    }

    protected override void Shoot()
    {
        _owner.FollowCamera.ApplyAimAssist();

        // 실제 발사 방향
        Vector3 origin = bulletSpawnPoint.position;
        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = (targetPoint - bulletSpawnPoint.position);

        if (muzzleFlashPrefab)
        {
            muzzleFlashEffect = Utils.Instantiate(muzzleFlashPrefab, origin, Quaternion.LookRotation(-_owner.transform.forward));
            Utils.Destroy(muzzleFlashEffect, 0.5f);
        }

        for (int i = 0; i < pelletCount; i++)
        {
            // 1단계 발사 방향 (좁은 스프레드)
            Vector3 narrowDir = GetRandomConeDirection(camShootDirection, denseSpreadAngle * Random.Range(0.6f, 1.0f));

            // 편차 벡터 계산: narrowDir에서 중앙 forward 제외 (normalized)
            Vector3 deviation = (narrowDir * 1.41f - camShootDirection).normalized;

            if (Physics.Raycast(origin, narrowDir, out RaycastHit denseHit, denseRange, ignoreMask))
            {
                // 밀집 히트 처리
                ProcessPelletHit(denseHit);
                DebugDrawPelletRays(origin, narrowDir, Vector3.zero, Vector3.zero);
                continue;
            }
            else
            {
                Vector3 denseEndPos = origin + narrowDir * denseRange;

                // 2단계: 편차 벡터를 축으로 2단계 확산 각도 내에서 회전시켜 더 넓게 퍼짐
                Vector3 spreadDir = RotateAroundAxis(narrowDir, deviation, (spreadAngle - denseSpreadAngle) * Random.Range(0.6f, 1.0f));

                if (Physics.Raycast(denseEndPos, spreadDir, out RaycastHit spreadHit, maxRange - denseRange, ignoreMask))
                {
                    ProcessPelletHit(spreadHit, 1.5f);
                }

                DebugDrawPelletRays(origin, narrowDir, denseEndPos, spreadDir);
            }
        }

        _audioSource.Stop();
        _audioSource.clip = shootClips[0];
        _audioSource.Play();

        if (_soundRoutine != null)
        {
            StopCoroutine(_soundRoutine);
            _soundRoutine = null;
        }
        _soundRoutine = StartCoroutine(CoPlayReloadClip());

        _owner.ApplyRecoil(impulseSource, recoilX, recoilY);

        _currentAmmo = Mathf.Clamp(_currentAmmo - 1, 0, maxAmmo);
        if (_currentAmmo <= 0)
        {
            CancleShootState(partType == EPartType.ArmL ? true : false);
            _isOverheat = true;
            GUIManager.Instance.GameUIController.SetAmmoColor(partType, true);
        }
    }

    // forward 벡터를 axis 축으로 angle 도만큼 회전하는 함수
    private Vector3 RotateAroundAxis(Vector3 forward, Vector3 axis, float angle)
    {
        // angle은 도 단위 (0~90)
        // angle을 0~1 범위로 정규화 (여기서 90도는 최대 회전)
        float t = Mathf.Clamp01(angle / 90f);

        // axis 방향 벡터 생성 (forward 벡터 끝에서 axis 방향으로 회전하므로 axis는 deviation normalized)
        Vector3 targetDir = (forward + axis).normalized;

        // forward에서 targetDir로 t 만큼 slerp (부드러운 방향 변화)
        Vector3 resultDir = Vector3.Slerp(forward, targetDir, t);

        return resultDir.normalized;
    }

    private Vector3 GetRandomConeDirection(Vector3 axis, float angle)
    {
        float angleRad = Mathf.Deg2Rad * angle;
        float z = Mathf.Cos(Random.Range(0f, angleRad));
        float theta = Random.Range(0f, 2 * Mathf.PI);
        float x = Mathf.Sin(angleRad) * Mathf.Cos(theta);
        float y = Mathf.Sin(angleRad) * Mathf.Sin(theta);

        // cone up축이 (0,0,1)일 때의 벡터
        Vector3 localDirection = new Vector3(x, y, z).normalized;

        // axis가 (0,0,1)일 때의 rotation에서 axis로 회전
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, axis);
        return (rot * localDirection).normalized;
    }

    // 히트 처리 함수 (적중 시 데미지, 이펙트 등)
    private void ProcessPelletHit(RaycastHit hit, float coefficient = 1.0f)
    {
        TakeDamage(hit.transform, coefficient);
        Utils.Destroy(Utils.Instantiate(hitEffectPrefab, hit.point, Quaternion.identity), 0.5f);
        Utils.Destroy(Utils.Instantiate(bulletPrefab, hit.point, Quaternion.identity), 0.1f);

        _damagedTargets.Clear();
    }

    private IEnumerator CoPlayReloadClip()
    {
        yield return new WaitForSeconds(0.5f);

        _audioSource.Stop();
        _audioSource.clip = shootClips[1];
        _audioSource.Play();
    }

    private void DebugDrawPelletRays(Vector3 startPoint, Vector3 narrowDir, Vector3 denseEndPos, Vector3 spreadDir)
    {
        // 1단계 narrow ray (빨간색)
        Debug.DrawRay(startPoint, narrowDir * denseRange, Color.red, 0.5f);

        // 2단계 spread ray (노란색)
        Debug.DrawRay(denseEndPos, spreadDir * (maxRange - denseRange), Color.yellow, 0.5f);
    }
}
