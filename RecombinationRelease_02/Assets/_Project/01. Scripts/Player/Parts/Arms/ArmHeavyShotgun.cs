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
        GUIManager.Instance.SetAmmoColor(partType, Color.red);
        Managers.GUIManager.Instance.SetAmmoColor(partType, false);

        if (_owner && !_owner.Stats.CombinedPartStats[partType].IsEmpty() && _owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots] != null)
        {
            _currentShootTime = (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value);
        }
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

    public override void FinishActionForced()
    {
        base.FinishActionForced();
        
        if (_soundRoutine != null)
        {
            StopCoroutine(_soundRoutine);
            _soundRoutine = null;
        }
    }

    protected override void Shoot()
    {
        Vector3 startPoint = _owner.FollowCamera.transform.position + _owner.FollowCamera.transform.forward * (Vector3.Distance(_owner.transform.position, _owner.FollowCamera.transform.position));
        Vector3 origin = bulletSpawnPoint.position;
        Vector3 forward = _owner.FollowCamera.transform.forward;

        if (muzzleFlashPrefab)
        {
            muzzleFlashEffect = Utils.Instantiate(muzzleFlashPrefab, origin, Quaternion.LookRotation(-_owner.transform.forward));
            Utils.Destroy(muzzleFlashEffect, 0.5f); // Lifetime of muzzle effect.
        }

        for (int i = 0; i < pelletCount; i++)
        {
            // 1. '좁은 탄착군' (bulletPosition 기준) ray
            Vector3 narrowDir = GetRandomConeDirection(forward, denseSpreadAngle);

            if (Physics.Raycast(startPoint, narrowDir, out RaycastHit hit, denseRange, ignoreMask))
            {
                // 밀집 히트 처리
                ProcessPelletHit(hit, 0.5f);
                continue;
            }
            else
            {
                Vector3 denseEndPos = origin + narrowDir * denseRange;

                // 2. '넓은 탄착군' (denseEndPos 기준) ray: narrowDir을 중심축 삼아 고르게 퍼짐!
                Vector3 spreadDir = GetRandomConeDirection(narrowDir, spreadAngle);

                if (Physics.Raycast(denseEndPos, spreadDir, out RaycastHit spreadHit, maxRange - denseRange, ignoreMask))
                {
                    // 확산 히트 처리 etc.
                    ProcessPelletHit(spreadHit);
                }
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
            GUIManager.Instance.SetAmmoColor(partType, true);
        }
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
    }

    private IEnumerator CoPlayReloadClip()
    {
        yield return new WaitForSeconds(0.5f);

        _audioSource.Stop();
        _audioSource.clip = shootClips[1];
        _audioSource.Play();
    }
}
