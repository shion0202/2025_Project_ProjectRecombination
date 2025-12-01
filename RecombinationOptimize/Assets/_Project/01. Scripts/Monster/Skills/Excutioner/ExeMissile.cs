using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

public class ExeMissile : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    private Vector3 _targetPosition;
    private float _lifeTime;
    private bool _isExplosion;

    private Vector3 _startPosition;
    private float _elapsed = 0.0f;

    private void Update()
    {
        transform.position = Vector3.Lerp(_startPosition, _targetPosition, _elapsed / _lifeTime);
        _elapsed += Time.deltaTime;

        if (_elapsed >= _lifeTime)
        {
            Explosion();
        }
    }

    public void Init(Vector3 inTargetPosition, bool isExplosion, float lifeTime = 1.0f)
    {
        _targetPosition = inTargetPosition;
        _lifeTime = lifeTime;
        _isExplosion = isExplosion;

        _startPosition = transform.position;
        transform.LookAt(inTargetPosition);
    }

    private void Explosion()
    {
        if (_isExplosion)
        {
            Utils.Destroy(Utils.Instantiate(explosionPrefab, transform.position, Quaternion.identity), 2.0f);
        }

        Utils.Destroy(gameObject);
    }
}
