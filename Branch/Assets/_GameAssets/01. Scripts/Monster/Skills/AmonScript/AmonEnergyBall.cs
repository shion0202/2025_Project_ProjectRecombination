using _Test.Skills;
using Monster.AI.Blackboard;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmonEnergyBall : MonoBehaviour
{
    public SkillData skillData;
    private bool _damaged;

    private void Start()
    {
        _damaged = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (_damaged) return; // 이미 데미지를 입힌 경우 무시
        
        Debug.Log("플레이어와 충돌!");
        // 데미지 적용
        IDamagable damagable = other.gameObject.GetComponent<IDamagable>();
        damagable?.ApplyDamage(skillData.damage);
        _damaged = true; // 데미지를 입힌 상태로 설정
    }
}
