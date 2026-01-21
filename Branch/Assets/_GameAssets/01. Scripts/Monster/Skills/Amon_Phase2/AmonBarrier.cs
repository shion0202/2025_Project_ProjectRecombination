using _Test.Skills;
using Monster.AI.Blackboard;
using Monster.AI.FSM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class AmonBarrier : MonoBehaviour, IDamagable
{
    /// - 캐스팅 시작 시 보호막 활성화 되며 보호막이 활성화 되는 동안 받는 모든 대미지 50% 감소
    /// - 보호막이 활성화 된 상태에서 플레이어가 공격하면 보호막이 일정량 대미지를 흡수함
    /// - 보호막이 흡수할 수 있는 대미지의 양은 몬스터의 최대 체력의 10%이며 보호막이 흡수할 수 있는 대미지의 양이 0이 되면 보호막이 비활성화 되고 캐스팅이 취소됨
    /// - 보호막이 활성화 된 상태에서 플레이어가 공격할 때마다 보호막이 흡수할 수 있는 대미지의 양이 감소
    
    private float _maxHealth;
    private int _currentBarrierHealth;
    private bool _isActive;
    
    private event Action _onBarrierDestroy;
    
    public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1, float defenceIgnoreRate = 0)
    {
        float currentDamage = inDamage * 0.5f;    // 보호막이 활성화 된 상태에서 받는 모든 대미지 50% 감소
        _currentBarrierHealth -= Mathf.CeilToInt(currentDamage);
    }

    public void Update()
    {
        if (!_isActive) return;
        
        if (_currentBarrierHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(float maxHealth, Action onDestroy = null)
    {
        _maxHealth = maxHealth * 0.1f; // 보호막이 흡수할 수 있는 대미지의 양은 몬스터의 최대 체력의 10%
        _onBarrierDestroy = onDestroy;
        _currentBarrierHealth = Mathf.CeilToInt(maxHealth); // 보호막이 흡수할 수 있는 대미지의 양은 몬스터의 최대 체력의 10%
        _isActive = true;
    }
    
    private void OnDestroy()
    {
        _onBarrierDestroy?.Invoke();
    }
}
