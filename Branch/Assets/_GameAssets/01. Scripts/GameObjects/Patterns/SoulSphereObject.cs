using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulSphereObject : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("피격 범위를 보여주는 원형 장판. Launch_Range.prefab 같은 평평한 디스크 메쉬를 넣는다.")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [Tooltip("rangeIndicatorPrefab의 localScale.x가 1일 때 실제로 보이는 반경(월드 단위). 메쉬 크기에 맞춰 인스펙터에서 보정한다.")]
    [SerializeField] private float rangeIndicatorBaseRadius = 0.5f;
    [SerializeField] private float lifeTime = 10.0f;
    [Tooltip("폭발 시 플레이어에게 데미지를 주는 반경. 이 범위 밖이면 피해를 받지 않는다.")]
    [SerializeField] private float explosionRadius = 5.0f;
    [Tooltip("범위 장판이 0에서 explosionRadius까지 커지는 데 걸리는 시간. 장판에 닿는 순간 데미지가 들어간다.")]
    [SerializeField] private float explosionGrowDuration = 0.5f;
    private DamagableObject _damageableObject;
    private float _currentTime = 0.0f;
    private float _damage;
    private bool _hasExploded;

    // 추후 Pooling할 경우 이벤트 등록 과정 수정 필요
    private void Start()
    {
        _damageableObject = gameObject.GetComponent<DamagableObject>();
        
        if (!_damageableObject) return;
        
        _damageableObject.OnObjectDied -= OnDieByPlayer;
        _damageableObject.OnObjectDied += OnDieByPlayer;
    }

    private void Update()
    {
        if (_hasExploded) return;

        _currentTime += Time.deltaTime;
        if (_currentTime >= lifeTime)
        {
            ExplosionObject();
        }
    }

    private void OnDestroy()
    {
        if (_damageableObject)
        {
            _damageableObject.OnObjectDied -= OnDieByPlayer;  // 이벤트 구독 해제
        }
    }

    public void Init(float inDamage, float inLifeTime = -1.0f)
    {
        _damage = inDamage;
        if (inLifeTime > 0.0f)
        {
            lifeTime = inLifeTime;
        }
    }

    private void ExplosionObject()
    {
        _hasExploded = true;

        Utils.Destroy(Utils.Instantiate(explosionPrefab, transform.position, Quaternion.identity), 1.5f);

        var indicator = Utils.Instantiate(rangeIndicatorPrefab, transform.position, Quaternion.identity);
        StartCoroutine(ExpandRangeIndicatorAndDamage(indicator));
    }

    // 폭발 반경까지 커지는 원형 장판을 보여주고, 장판에 닿는 순간에만 데미지를 준다.
    // (거리와 무관하게 즉시 피격되던 버그 수정 + 피격 범위를 눈으로 보여주기 위함)
    private IEnumerator ExpandRangeIndicatorAndDamage(GameObject indicator)
    {
        var playerObject = Managers.MonsterManager.Instance.Player;
        var hasDamaged = false;
        var elapsed = 0.0f;

        while (elapsed < explosionGrowDuration)
        {
            var currentRadius = Mathf.Lerp(0.0f, explosionRadius, elapsed / explosionGrowDuration);
            if (indicator)
            {
                var scale = currentRadius / rangeIndicatorBaseRadius;
                var indicatorScale = indicator.transform.localScale;
                indicator.transform.localScale = new Vector3(scale, indicatorScale.y, scale);
            }

            if (!hasDamaged && playerObject)
            {
                var sqrDistance = (playerObject.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance <= currentRadius * currentRadius
                    && playerObject.TryGetComponent(out PlayerController player))
                {
                    player.ApplyDamage(_damage, 1 << playerObject.layer);
                    hasDamaged = true;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (indicator)
        {
            Utils.Destroy(indicator);
        }
        Utils.Destroy(gameObject);
    }

    private void OnDieByPlayer()
    {
        Utils.Destroy(gameObject);
    }
}
