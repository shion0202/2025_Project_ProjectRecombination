using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmBasic : PartBaseArm
{
    protected override void Awake()
    {
        base.Awake();

        _isZooming = false;
    }

    protected void OnEnable()
    {
        GUIManager.Instance.SetAmmoColor(partType, Color.white);
        Managers.GUIManager.Instance.SetAmmoColor(partType, false);

        if (partType == EPartType.ArmL)
        {
            GUIManager.Instance.SetAmmoLeftSlider(0, 0);
        }
        else
        {
            GUIManager.Instance.SetAmmoRightSlider(0, 0);
        }
    }

    protected override void Update()
    {
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

    protected override void Shoot()
    {
        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = (targetPoint - bulletSpawnPoint.position);

        GameObject bullet = Utils.Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(camShootDirection.normalized));
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
        }
    }
}
