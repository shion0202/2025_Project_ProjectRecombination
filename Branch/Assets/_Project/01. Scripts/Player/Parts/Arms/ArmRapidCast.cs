using Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmRapidCast : PartBaseArm
{
    [Header("속사 설정")]
    [SerializeField] protected GameObject castEffectPrefab;
    [SerializeField] protected float maxChargeTime = 2.0f;
    [SerializeField] protected float maxCastTime = 1.0f;
    protected float _currentChargeTime = 0.0f;
    private float _currentCastTime = 0.0f;

    protected override void Update()
    {
        _currentShootTime -= Time.deltaTime;

        if ((_owner.CurrentPlayerState & EPlayerState.Rotating) != 0) return;
        if (!_isShooting)
        {
            _currentCastTime -= Time.deltaTime;
            return;
        }

        if (_currentCastTime <= maxCastTime)
        {
            _currentCastTime += Time.deltaTime;
            return;
        }

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

        RaycastHit hit;
        Vector3 targetPoint = GetTargetPoint(out hit);
        //laserLineRenderer.SetPosition(0, bulletSpawnPoint.position);

        if (hit.collider != null)
        {
            //laserLineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            Vector3 camShootDirection = (targetPoint - bulletSpawnPoint.position).normalized;
            //laserLineRenderer.SetPosition(1, bulletSpawnPoint.position + camShootDirection * 100.0f);
        }

        //laserLineRenderer.enabled = true;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        fadeCoroutine = StartCoroutine(CoFadeOutLaser());

        if (hit.collider != null)
        {
            // 몬스터 피격 판정
            TakeDamage(hit.transform);

            Destroy(Instantiate(hitEffectPrefab, targetPoint, Quaternion.identity), 0.5f);
            Destroy(Instantiate(bulletPrefab, targetPoint, Quaternion.identity), 0.5f);
        }

        _owner.ApplyRecoil(impulseSource, recoilX, recoilY);
    }
}
