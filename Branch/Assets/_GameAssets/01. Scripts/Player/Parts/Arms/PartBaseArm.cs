using Cinemachine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseArm : PartBase
{
    [Header("사격")]
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected LayerMask ignoreMask = 0;
    [SerializeField] protected int maxAmmo;
    [SerializeField] protected float reloadTime;
    protected Transform bulletSpawnPoint;
    protected CinemachineImpulseSource impulseSource;
    protected float _currentShootTime = 0.0f;
    protected bool _isShooting = false;
    protected int _currentAmmo = 0;
    protected float _currentReloadTime = 0.0f;
    protected bool _isOverheat = false;
    protected bool _isUseOverheat = false;
    protected bool _isUseAmmo = false;

    [Header("이펙트")]
    [SerializeField] protected GameObject muzzleFlashEffectPrefab;
    [SerializeField] protected GameObject hitEffectPrefab;
    [SerializeField] protected GameObject projectileEffectPrefab;
    protected Color originalColor = Color.white;
    protected Coroutine fadeCoroutine = null;

    // 반동 관련 값을 스탯으로 관리할지?
    [Header("파라미터")]
    [SerializeField] protected float shootingRange = 100.0f;
    [SerializeField] protected float recoilX = 4.0f;
    [SerializeField] protected float recoilY = 2.0f;
    [SerializeField, Tooltip("조준점이 플레이어로부터 이 거리보다 가까우면 조준선 위 이 거리 지점을 향해 발사한다. 총구가 조준선 옆에 있어 가까운 조준점일수록 탄이 옆으로 크게 꺾이는 문제 완화용")]
    protected float minAimDistance = 5.0f;

    [Header("사격 자세")]
    [Tooltip("이 파츠를 장착했을 때의 사격 IK 자세 값. 비우면 ArmShootIKTargets의 기본값을 쓴다.")]
    [SerializeField] protected ArmIKProfile ikProfile;
    [Tooltip("발사 지점 보정값. 발사 지점(Bullet Spawner)은 모든 팔 파츠가 공유하므로, " +
             "이 파츠의 총구 위치에 맞게 장착 시 원래 위치에서 이만큼 옮긴다. (Bullet Spawner 부모 기준 로컬 좌표)")]
    [SerializeField] protected Vector3 spawnPointOffset = Vector3.zero;
    private BulletSpawnPoint _bulletSpawner;

    public bool IsOverheat => _isOverheat;
    public ArmIKProfile IKProfile => ikProfile;

    // [임시] 사격 불가 버그 추적용. PlayerController.BuildShootDebugReport()에서 사용한다.
    public string BuildDebugState()
    {
        return $"isShooting={_isShooting}, ammo={_currentAmmo}/{maxAmmo}, overheat={_isOverheat}, shootTime={_currentShootTime:F2}, active={gameObject.activeInHierarchy}";
    }

    protected override void Awake()
    {
        base.Awake();

        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (ignoreMask == 0)
        {
            ignoreMask = ~0;
            ignoreMask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Water"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("UI"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Face"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Hair"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Outline"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Player"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("PlayerMesh"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Bullet"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Minimap"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("MonsterDead"));
        }

        _currentAmmo = maxAmmo;
        _currentReloadTime = reloadTime;
    }

    protected virtual void Update()
    {
        if (partType == EPartType.ArmL)
        {
            GUIManager.Instance.GameUIController.SetAmmoLeftSlider(_currentAmmo, maxAmmo);
        }
        else
        {
            GUIManager.Instance.GameUIController.SetAmmoRightSlider(_currentAmmo, maxAmmo);
        }

        _currentShootTime -= Time.deltaTime;

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
                GUIManager.Instance.GameUIController.SetAmmoColor(partType, false);
            }

            return;
        }
        if ((_owner.CurrentPlayerState & EPlayerState.Rotating) != 0) return;

        if (_currentAmmo <= 0) return;
        if (_currentShootTime <= 0.0f)
        {
            Shoot();
            _currentShootTime = (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value);
        }
    }

    public override void UseAbility()
    {
        _isShooting = true;
    }

    public override void UseCancleAbility()
    {
        _isShooting = false;
    }

    public override void FinishActionForced()
    {
        _isShooting = false;
        _currentShootTime = 0.0f;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    public override void SetOwner(PlayerController owner)
    {
        base.SetOwner(owner);

        BulletSpawnPoint bulletSpawner = null;
        if (partType == EPartType.ArmL) bulletSpawner = _owner.GetComponentInChildren<BulletSpawnPointLeft>();
        else bulletSpawner = _owner.GetComponentInChildren<BulletSpawnPointRight>();
        if (bulletSpawner != null)
        {
            bulletSpawnPoint = bulletSpawner.transform;
            _bulletSpawner = bulletSpawner;
        }
    }

    public override void OnEquipped()
    {
        base.OnEquipped();

        if (_bulletSpawner != null)
        {
            _bulletSpawner.ApplyOffset(spawnPointOffset);
        }
    }

#if UNITY_EDITOR
    // 플레이 중 인스펙터에서 보정값을 바꾸면 장착 중인 파츠에 한해 바로 반영한다. (조절용)
    private void OnValidate()
    {
        if (!Application.isPlaying || !gameObject.activeInHierarchy || _bulletSpawner == null) return;

        _bulletSpawner.ApplyOffset(spawnPointOffset);
    }
#endif

    protected virtual void Shoot()
    {
        _owner.FollowCamera.ApplyAimAssist();

        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = GetShootDirection(targetPoint);

        GameObject bullet = Utils.Instantiate(bulletPrefab, bulletSpawnPoint.position + camShootDirection.normalized * 1.5f, Quaternion.LookRotation(camShootDirection.normalized));
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Parent = bulletSpawnPoint;
            bulletComponent.Init(_owner.gameObject, null, bulletSpawnPoint.position + camShootDirection.normalized * 1.5f, Vector3.zero, camShootDirection.normalized, (int)_owner.Stats.CombinedPartStats[partType][EStatType.Damage].value);
        }

        _owner.ApplyRecoil(impulseSource, recoilX, recoilY);

        _currentAmmo = Mathf.Clamp(_currentAmmo - 1, 0, maxAmmo);
        if (_currentAmmo <= 0)
        {
            CancleShootState(partType == EPartType.ArmL ? true : false);
            _isOverheat = true;
            GUIManager.Instance.GameUIController.SetAmmoColor(partType, true);
        }
    }

    // 카메라 기준 사격 방향을 결정하는 함수
    protected Vector3 GetTargetPoint(out RaycastHit hit)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 startPoint = _owner.FollowCamera.transform.position + _owner.FollowCamera.transform.forward * ((Vector3.Distance(_owner.transform.position, _owner.FollowCamera.transform.position)) + 1.0f);
        Vector3 targetPoint = Vector3.zero;

        if (Physics.Raycast(startPoint, ray.direction, out hit, shootingRange, ignoreMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * shootingRange;
        }

        return targetPoint;
    }

    protected Vector3 GetTargetPoint(out RaycastHit[] hits)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 startPoint = _owner.FollowCamera.transform.position + _owner.FollowCamera.transform.forward * ((Vector3.Distance(_owner.transform.position, _owner.FollowCamera.transform.position)) + 1.0f);
        Vector3 targetPoint = Vector3.zero;

        hits = Physics.RaycastAll(startPoint, ray.direction, shootingRange, ignoreMask);
        if (hits.Length > 0)
        {
            targetPoint = hits[0].point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * shootingRange;
        }

        return targetPoint;
    }

    // 총구에서 조준점을 향하는 발사 방향. 조준점이 minAimDistance보다 가까우면 조준선 위 minAimDistance 지점을 향한다.
    // 조준점 자체(피격 위치, 미사일 목표 등)가 필요한 곳에는 쓰지 않고, 직선 투사체의 방향에만 사용한다.
    protected Vector3 GetShootDirection(Vector3 targetPoint)
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        float minDepth = Vector3.Dot(_owner.transform.position - ray.origin, ray.direction) + minAimDistance;
        if (Vector3.Dot(targetPoint - ray.origin, ray.direction) < minDepth)
        {
            targetPoint = ray.origin + ray.direction * minDepth;
        }

        return (targetPoint - bulletSpawnPoint.position).normalized;
    }

    protected void CancleShootState(bool isLeft)
    {
        _owner.CancleAttack(isLeft);
    }

    // 현재 사용 X
    protected IEnumerator CoFadeOutLaser()
    {
        //MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        //laserLineRenderer.GetPropertyBlock(propertyBlock);
        //Color c = originalColor;

        //while (c.a > 0.0f)
        //{
        //    propertyBlock.SetColor("_Color", Color.red);
        //    laserLineRenderer.SetPropertyBlock(propertyBlock);

        //    c.a -= Time.deltaTime;
        //    if (c.a <= 0.0f) c.a = 0.0f;

        //    yield return null;
        //}

        //laserLineRenderer.enabled = false;
        //fadeCoroutine = null;

        yield break;
    }
}
