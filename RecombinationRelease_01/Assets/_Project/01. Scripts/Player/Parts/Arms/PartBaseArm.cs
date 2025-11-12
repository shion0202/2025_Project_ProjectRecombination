using Cinemachine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartBaseArm : PartBase
{
    [Header("사격")]
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected Transform bulletSpawnPoint;
    [SerializeField] protected CinemachineImpulseSource impulseSource;
    [SerializeField] protected LayerMask ignoreMask = 0;
    [SerializeField] protected int maxAmmo;
    [SerializeField] protected float reloadTime;
    protected float _currentShootTime = 0.0f;
    protected bool _isShooting = false;
    protected int _currentAmmo = 0;
    protected float _currentReloadTime = 0.0f;
    protected bool _isOverheat = false;

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

    protected virtual void Awake()
    {
        if (ignoreMask == 0)
        {
            ignoreMask |= 1;
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Outline"));
            ignoreMask &= ~(1 << LayerMask.NameToLayer("Player"));
        }

        _currentAmmo = maxAmmo;
        _currentReloadTime = reloadTime;
    }

    protected virtual void Update()
    {
        if (partType == EPartType.ArmL)
        {
            GUIManager.Instance.SetAmmoLeftSlider(_currentAmmo, maxAmmo);
        }
        else
        {
            GUIManager.Instance.SetAmmoRightSlider(_currentAmmo, maxAmmo);
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
                GUIManager.Instance.SetAmmoColor(partType, false);
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

    protected virtual void Shoot()
    {
        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = (targetPoint - bulletSpawnPoint.position);

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(camShootDirection.normalized));
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Init(_owner.gameObject, null, bulletSpawnPoint.position, Vector3.zero, camShootDirection.normalized, (int)_owner.Stats.CombinedPartStats[partType][EStatType.Damage].value);
            bulletComponent.Parent = bulletSpawnPoint;
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

    // 카메라 기준 사격 방향을 결정하는 함수
    protected Vector3 GetTargetPoint(out RaycastHit hit)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = Vector3.zero;

        if (Physics.Raycast(ray, out hit, shootingRange, ignoreMask))
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
        Vector3 targetPoint = Vector3.zero;

        hits = Physics.RaycastAll(ray.origin, ray.direction, shootingRange, ignoreMask);
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
