using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class AmonMeleeCollision : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask;
    private float _damage;
    //private Vector3 collisionScale;
    //private Vector3 collisionOffset;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            IDamagable target = other.transform.GetComponent<IDamagable>();
            target.ApplyDamage(_damage, targetMask);
        }
    }

    public void Init(float inDamage, Vector3 inScale, Vector3 inOffset, LayerMask inTargetMask = default)
    {
        transform.localScale = inScale;
        transform.localPosition = inOffset;

        _damage = inDamage;

        if (inTargetMask != default)
        {
            targetMask = inTargetMask;
        }
    }
}
