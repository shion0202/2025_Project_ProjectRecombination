using Monster;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;

public class HoverBarrier : MonoBehaviour
{
    [SerializeField] private float damage = 10.0f;      // 보호막이 줄 데미지
    [SerializeField] private float pushForce = 100.0f;   // 보호막이 몬스터를 밀쳐내는 힘

    //// 보호막 스크립트: 몬스터와 충돌 또는 트리거할 경우 데미지를 주면서 밀쳐내기
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Enemy"))
    //    {
    //        Debug.Log($"충돌! 현재 배리어 공격력: {damage}");
    //        // 몬스터가 보호막에 닿았을 때, 데미지를 주고 밀쳐내기
    //        TakeDamage(other.transform);
    //        Vector3 pushDirection = (other.transform.position - transform.position).normalized;
    //        pushDirection.y = 0.0f;
    //        Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
    //        if (rb != null)
    //        {
    //            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
    //            Debug.Log("Add Force!");
    //        }      
    //    }
    //}

    //public void TakeDamage(Transform target, float coefficient = 1.0f)
    //{
    //    IDamagable enemy = target.GetComponent<IDamagable>();
    //    if (enemy != null)
    //    {
    //        enemy.ApplyDamage(damage * coefficient);
    //    }
    //    else
    //    {
    //        enemy = target.transform.GetComponentInParent<IDamagable>();
    //        if (enemy != null)
    //        {
    //            enemy.ApplyDamage(damage * coefficient);
    //        }
    //    }
    //}
}
