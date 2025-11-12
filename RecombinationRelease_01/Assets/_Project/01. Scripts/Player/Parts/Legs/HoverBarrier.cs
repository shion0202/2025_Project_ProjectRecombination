using Monster;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;

public class HoverBarrier : MonoBehaviour
{
    private float _damage = 10.0f;      // 보호막이 줄 데미지
    private float _pushForce = 50.0f;   // 보호막이 몬스터를 밀쳐내는 힘

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }

    public float PushForce
    {
        get => _pushForce;
        set => _pushForce = value;
    }

    // 보호막 스크립트: 몬스터와 충돌 또는 트리거할 경우 데미지를 주면서 밀쳐내기
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"충돌! 현재 배리어 공격력: {_damage}");
            // 몬스터가 보호막에 닿았을 때, 데미지를 주고 밀쳐내기
            TakeDamage(other.transform);
            Vector3 pushDirection = (other.transform.position - transform.position).normalized;
            Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(pushDirection * _pushForce, ForceMode.Impulse);     // 예시로 100의 힘으로 밀쳐냄
            }      
        }
    }

    public void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        MonsterBase monster = target.GetComponent<MonsterBase>();
        if (monster != null)
        {
            monster.TakeDamage((int)(_damage * coefficient));
        }
        else
        {
            monster = target.transform.GetComponentInParent<MonsterBase>();
            if (monster != null)
            {
                monster.TakeDamage((int)(_damage * coefficient));
            }
        }
    }
}
