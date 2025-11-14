using Managers;
using Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmLaserCharge : PartBaseArm
{
    [Header("차지 레이저 설정")]
    [SerializeField] private GameObject laserPrefab;  // LineRenderer 포함된 프리팹
    [SerializeField] protected GameObject chargeEffectPrefab;
    [SerializeField] protected float maxChargeTime = 2.0f;
    [SerializeField] protected Vector3 chargeOffset = Vector3.zero;
    [SerializeField] protected List<AudioClip> chargeClips = new();
    protected float _currentChargeTime = 0.0f;
    private GameObject currentLaserObject;
    private LineRenderer currentLaser;
    private GameObject chargeEffect;
    protected Vector3 defaultImpulseValue;
    protected SkinnedMeshRenderer smr;
    protected bool isMaxCharge = false;
    protected AudioSource _audioSource;

    protected Coroutine _recoilRoutine = null;
    protected Coroutine _morphBlendRoutine = null;
    protected Coroutine _morphBlendSecondRoutine = null;

    protected override void Awake()
    {
        base.Awake();

        defaultImpulseValue = impulseSource.m_DefaultVelocity;
        _audioSource = GetComponent<AudioSource>();
    }

    protected void OnEnable()
    {
        GUIManager.Instance.SetAmmoColor(partType, Color.blue);
        Managers.GUIManager.Instance.SetAmmoColor(partType, false);
    }

    protected override void Update()
    {
        if (partType == EPartType.ArmL)
        {
            GUIManager.Instance.SetAmmoLeftSlider(_currentShootTime, maxChargeTime);
        }
        else
        {
            GUIManager.Instance.SetAmmoRightSlider(_currentShootTime, maxChargeTime);
        }

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

        _currentShootTime += Time.deltaTime;
        if (_currentShootTime >= maxChargeTime)
        {
            if (!isMaxCharge)
            {
                _morphBlendSecondRoutine = StartCoroutine(CoMorphBlend(1, true, 0.3f));
                isMaxCharge = true;
            }

            if (chargeEffect)
            {
                Utils.Destroy(chargeEffect);
                chargeEffect = null;
            }
        }
    }

    public override void UseAbility()
    {
        if (_currentAmmo <= 0) return;

        base.UseAbility();
        if (chargeEffectPrefab)
        {
            chargeEffect = Utils.Instantiate(chargeEffectPrefab, bulletSpawnPoint.position + chargeOffset, Quaternion.identity, bulletSpawnPoint);
        }

        if (_morphBlendRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
        }
        _morphBlendRoutine = StartCoroutine(CoMorphBlend(0, true));
    }

    public override void UseCancleAbility()
    {
        if (!_isShooting || _currentAmmo <= 0) return;

        base.UseCancleAbility();
        if (chargeEffect)
        {
            Utils.Destroy(chargeEffect);
            chargeEffect = null;
        }

        Shoot();

        _currentShootTime = 0.0f;
        isMaxCharge = false;
    }

    public override void FinishActionForced()
    {
        base.FinishActionForced();

        if (chargeEffect)
        {
            Utils.Destroy(chargeEffect);
            chargeEffect = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;

            Utils.Destroy(currentLaserObject.gameObject);
            currentLaser = null;
            currentLaserObject = null;
        }

        if (_recoilRoutine != null)
        {
            StopCoroutine(_recoilRoutine);
            _recoilRoutine = null;

            impulseSource.m_DefaultVelocity = defaultImpulseValue;
        }

        if (_morphBlendRoutine != null || _morphBlendSecondRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
            StopCoroutine(_morphBlendSecondRoutine);
            _morphBlendRoutine = null;
            _morphBlendSecondRoutine = null;

            SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();
            smr.SetBlendShapeWeight(0, 0.0f);
            smr.SetBlendShapeWeight(1, 0.0f);
        }
    }

    protected override void Shoot()
    {
        if (currentLaserObject == null)
        {
            currentLaserObject = Utils.Instantiate(laserPrefab);
            currentLaser = currentLaserObject.GetComponent<LineRenderer>();
        }
        if (currentLaser == null) return;
        currentLaser.enabled = true;

        if (_currentShootTime > maxChargeTime)
        {
            _currentShootTime = maxChargeTime;
            _audioSource.clip = chargeClips[1];
        }
        else
        {
            _audioSource.clip = chargeClips[0];
        }
        _currentChargeTime = (_currentShootTime / maxChargeTime);

        // 벽 등 레이저가 최종적으로 충돌할 Obstacle Point 연산
        RaycastHit hit;
        Vector3 obstaclePoint = GetTargetPoint(out hit); 

        // 몬스터 등 레이저가 통과해야할 Target Point 연산
        RaycastHit[] hits;
        Vector3 targetPoint = GetTargetPointsByMask(out hits);

        // 레이저 활성화 후 위치 및 방향 설정
        currentLaserObject.gameObject.SetActive(true);
        currentLaser.startWidth = 0.3f + 0.3f * _currentChargeTime;
        currentLaser.endWidth = 0.3f + 0.3f * _currentChargeTime;
        currentLaser.transform.position = bulletSpawnPoint.position;
        Vector3 camShootDirection = (obstaclePoint - bulletSpawnPoint.position).normalized;
        currentLaser.transform.rotation = Quaternion.LookRotation(camShootDirection);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        fadeCoroutine = StartCoroutine(CoDestroyLaser());

        if (hits.Length > 0)
        {
            foreach (RaycastHit hitObject in hits)
            {
                TakeDamage(hitObject.transform, 1.0f + _currentChargeTime);
                Utils.Destroy(Utils.Instantiate(bulletPrefab, hitObject.point, Quaternion.identity), 0.1f);
            }
        }

        _audioSource.Play();
        impulseSource.m_DefaultVelocity = (defaultImpulseValue * 0.5f) + defaultImpulseValue * _currentChargeTime * 0.5f;
        _owner.ApplyRecoil(impulseSource, recoilX * _currentChargeTime, recoilY * _currentChargeTime);
        if (_recoilRoutine != null)
        {
            StopCoroutine(_recoilRoutine);
        }
        _recoilRoutine = StartCoroutine(CoDelayRecoil());

        _currentAmmo = Mathf.Clamp(_currentAmmo - 1, 0, maxAmmo);
        if (_currentAmmo <= 0)
        {
            _isOverheat = true;
        }

        if (_morphBlendRoutine != null)
        {
            StopCoroutine(_morphBlendRoutine);
        }
        _morphBlendRoutine = StartCoroutine(CoMorphBlend(0, false));

        if (_morphBlendSecondRoutine != null)
        {
            StopCoroutine(_morphBlendSecondRoutine);
        }
        _morphBlendSecondRoutine = StartCoroutine(CoMorphBlend(1, false));
    }

    protected IEnumerator CoDestroyLaser()
    {
        yield return new WaitForSeconds(0.1f);

        Utils.Destroy(currentLaserObject.gameObject);
        currentLaser = null;
        currentLaserObject = null;
    }

    protected IEnumerator CoDelayRecoil()
    {
        yield return new WaitForSeconds(0.5f);

        impulseSource.m_DefaultVelocity = defaultImpulseValue;
        _recoilRoutine = null;
    }

    protected IEnumerator CoMorphBlend(int morphIndex, bool isToFire, float time = 0.5f)
    {
        SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();
        float min = isToFire ? 0.0f : 100.0f;
        float max = isToFire ? 100.0f : 0.0f;

        float elapsed = min;
        while (elapsed < time)
        {
            if (isToFire) elapsed += Time.deltaTime;
            else elapsed -= Time.deltaTime;

            float weight = Mathf.Lerp(min, max, elapsed / time);  // weight는 0~100 범위
            smr.SetBlendShapeWeight(morphIndex, weight);
            yield return null;
        }

        // 보정: 정확히 1(max)로 설정
        smr.SetBlendShapeWeight(morphIndex, max);
        _morphBlendRoutine = null;
    }

    protected Vector3 GetTargetPointsByMask(out RaycastHit[] hits)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 startPoint = _owner.FollowCamera.transform.position + _owner.FollowCamera.transform.forward * (Vector3.Distance(_owner.transform.position, _owner.FollowCamera.transform.position));
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
}
