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

    // 한 발의 펠릿 적중 정보. 타격음을 한 번만 내기 위해 모든 펠릿을 판정한 뒤 이펙트를 생성한다.
    private readonly List<(RaycastHit hit, float coefficient)> _pelletHits = new();

    // _currentShootTime은 발사 후 0에서 발사 간격까지 증가한다. (다른 팔은 감소)
    // 시작 시점에 발사 간격 스탯을 알 수 없으므로 어떤 간격보다도 큰 값으로 두어 바로 발사 가능한 상태로 시작한다.
    private const float ReadyShootTime = 1000000.0f;

    protected override void Awake()
    {
        base.Awake();
        _audioSource = GetComponent<AudioSource>();
        _currentShootTime = ReadyShootTime;
    }

    protected void OnEnable()
    {
        GUIManager.Instance.GameUIController.SetAmmoColor(partType, Color.red);
        GUIManager.Instance.GameUIController.SetAmmoColor(partType, false);

        // 장착 시 발사 간격으로 초기화하면 안 된다.
        // 파츠 교체 중에는 스탯이 아직 이전 파츠 기준이라 짧은 간격이 들어가 쿨타임이 처음부터 다시 돌았다.
        // 다른 팔 파츠처럼 해제 시점의 진행 상태에서 이어서 회복한다.

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
        // 기본 처리가 _currentShootTime을 0으로 만드는데, 증가형인 이 파츠에서는 "방금 발사함"이 된다.
        // 파츠 교체로 쿨타임이 초기화되지 않도록 진행 상태를 유지한다.
        float shootTime = _currentShootTime;
        base.FinishActionForced();
        _currentShootTime = shootTime;

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
        // 아래 편차 벡터 계산이 정규화되지 않은 길이를 전제로 튜닝되어 있어, 방향만 보정하고 길이는 유지한다.
        Vector3 camShootDirection = GetShootDirection(targetPoint) * (targetPoint - bulletSpawnPoint.position).magnitude;

        if (muzzleFlashPrefab)
        {
            muzzleFlashEffect = Utils.Instantiate(muzzleFlashPrefab, origin, Quaternion.LookRotation(-_owner.transform.forward));
            Utils.Destroy(muzzleFlashEffect, 0.5f);
        }

        _pelletHits.Clear();

        for (int i = 0; i < pelletCount; i++)
        {
            // 1단계 발사 방향 (좁은 스프레드)
            Vector3 narrowDir = GetRandomConeDirection(camShootDirection, denseSpreadAngle * Random.Range(0.6f, 1.0f));

            // 편차 벡터 계산: narrowDir에서 중앙 forward 제외 (normalized)
            Vector3 deviation = (narrowDir * 1.41f - camShootDirection).normalized;

            if (Physics.Raycast(origin, narrowDir, out RaycastHit denseHit, denseRange, ignoreMask))
            {
                // 밀집 히트 처리
                _pelletHits.Add((denseHit, 1.0f));
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
                    _pelletHits.Add((spreadHit, 1.5f));
                }

                DebugDrawPelletRays(origin, narrowDir, denseEndPos, spreadDir);
            }
        }

        // 같은 타격음이 펠릿 수만큼 동시에 겹치면 음량이 크게 합쳐지고 파형 간섭으로 먹먹하게 울린다.
        // 이펙트는 펠릿마다 생성하되, 소리는 플레이어에게 가장 가까운 적중 지점에서 한 번만 낸다.
        int soundHitIndex = -1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < _pelletHits.Count; i++)
        {
            if (_pelletHits[i].hit.distance < nearestDistance)
            {
                nearestDistance = _pelletHits[i].hit.distance;
                soundHitIndex = i;
            }
        }

        for (int i = 0; i < _pelletHits.Count; i++)
        {
            ProcessPelletHit(_pelletHits[i].hit, _pelletHits[i].coefficient, i == soundHitIndex);
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
    private void ProcessPelletHit(RaycastHit hit, float coefficient, bool playSound)
    {
        TakeDamage(hit.transform, coefficient);

        GameObject hitEffect = Utils.Instantiate(hitEffectPrefab, hit.point, Quaternion.identity);
        if (!playSound)
        {
            // 타격 이펙트의 AudioSource는 Play On Awake라 활성화되는 순간 재생을 시작한다.
            // 같은 프레임에 멈추면 소리가 출력되지 않는다. (풀에서 다시 꺼내면 다시 재생된다)
            foreach (AudioSource source in hitEffect.GetComponentsInChildren<AudioSource>())
            {
                source.Stop();
            }
        }
        Utils.Destroy(hitEffect, 0.5f);
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
