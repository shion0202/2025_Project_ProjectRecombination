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

    public bool IsOverheat => _isOverheat;

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
        }
    }

    protected virtual void Shoot()
    {
        _owner.FollowCamera.ApplyAimAssist();

        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = (targetPoint - bulletSpawnPoint.position);

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
