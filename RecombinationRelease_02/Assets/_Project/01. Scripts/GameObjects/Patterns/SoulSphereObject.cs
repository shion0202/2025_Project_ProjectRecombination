using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulSphereObject : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float lifeTime = 10.0f;
    private DamagableObject _damageableObject;
    private float _currentTime = 0.0f;
    private float _damage;

    // 추후 Pooling할 경우 이벤트 등록 과정 수정 필요
    private void Start()
    {
        _damageableObject = gameObject.GetComponent<DamagableObject>();
        if (_damageableObject)
        {
            _damageableObject.OnObjectDied -= OnDieByPlayer;
            _damageableObject.OnObjectDied += OnDieByPlayer;
        }
    }

    private void Update()
    {
        _currentTime += Time.deltaTime;
        if (_currentTime >= lifeTime)
        {
            ExplosionObject();
        }
    }

    void OnDestroy()
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
        Utils.Destroy(Utils.Instantiate(explosionPrefab, transform.position, Quaternion.identity), 1.0f);
        Utils.Destroy(gameObject);
    }

    private void OnDieByPlayer()
    {
        Utils.Destroy(gameObject);
    }
}
