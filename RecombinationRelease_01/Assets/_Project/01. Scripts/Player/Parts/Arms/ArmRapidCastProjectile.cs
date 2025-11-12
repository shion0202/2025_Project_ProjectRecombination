using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmRapidCastProjectile : PartBaseArm
{
    [Header("속사 설정")]
    [SerializeField] protected GameObject castEffectPrefab;
    [SerializeField] protected float maxChargeTime = 2.0f;
    [SerializeField] protected float maxCastTime = 1.0f;
    protected float _currentChargeTime = 0.0f;
    private float _currentCastTime = 0.0f;

    protected override void Update()
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
            _currentCastTime -= Time.deltaTime;
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

        if (_currentCastTime <= maxCastTime)
        {
            _currentCastTime += Time.deltaTime;
            return;
        }

        if (_currentAmmo <= 0) return;
        _currentChargeTime += Time.deltaTime;
        if (_currentShootTime <= 0.0f)
        {
            _currentShootTime = (_owner.Stats.CombinedPartStats[partType][EStatType.IntervalBetweenShots].value) / (1.0f + _currentChargeTime / maxChargeTime);
            Shoot();
        }
    }

    public override void UseAbility()
    {
        base.UseAbility();

        if (_currentCastTime < 0.0f)
        {
            _currentCastTime = 0.0f;
        }
    }

    public override void UseCancleAbility()
    {
        base.UseCancleAbility();
        _currentChargeTime = 0.0f;
    }

    protected override void Shoot()
    {
        if (_currentChargeTime > maxChargeTime)
        {
            _currentChargeTime = maxChargeTime;
        }

        Vector3 targetPoint = GetTargetPoint(out RaycastHit hit);
        Vector3 camShootDirection = (targetPoint - bulletSpawnPoint.position);
        camShootDirection.Normalize();

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Init(_owner.gameObject, null,bulletSpawnPoint.position, Vector3.zero, camShootDirection, (int)_owner.Stats.CombinedPartStats[partType][EStatType.Damage].value);
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
}
