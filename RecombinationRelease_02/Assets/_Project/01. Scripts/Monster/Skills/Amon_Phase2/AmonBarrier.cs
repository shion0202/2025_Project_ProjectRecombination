using _Test.Skills;
using Monster.AI.Blackboard;
using Monster.AI.FSM;
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

    [SerializeField] private int barrierHealth = 40;           // 40번 타격 시 보호막 파괴
    [SerializeField] private float absorbablePercent = 0.1f;
    private AmonSoulAbsorb skillData;
    private FSM _fsm;
    private Dictionary<string, object> _stats;
    private float _currentAbsorbablePercent;
    private float _maxHealth;
    private int _currentBarrierHealth;

    // ApplyDamage 확인용 로직 (실제 사용 X)
    // To-do: Skill Data에서 FSM을 전달할 방법이 마땅치 않으므로, FSM에서 특정 상황일 때 데미지 로직을 수정하는 방향으로 구현
    public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1, float defenceIgnoreRate = 0)
    {
        float currentDamage = Utils.GetDamage(inDamage, defenceIgnoreRate, unitOfTime, _stats);
        currentDamage *= 0.5f;    // 보호막이 활성화 된 상태에서 받는 모든 대미지 50% 감소

        float absorbableDamage = _maxHealth * _currentAbsorbablePercent;
        currentDamage = Mathf.Clamp(currentDamage - absorbableDamage, 0.0f, currentDamage);

        _fsm.ApplyDamage(currentDamage, targetMask, unitOfTime, defenceIgnoreRate);

        // 피격 시마다 보호막이 흡수할 수 있는 배율이 점점 감소하여, 0이 되면 보호막 파괴 
        _currentAbsorbablePercent = Mathf.Clamp(_currentAbsorbablePercent - (absorbablePercent / barrierHealth), 0.0f, absorbablePercent);
        if (_currentAbsorbablePercent <= 0.0f)
        {
            // 보호막 파괴 처리
            skillData.IsSheldRemovedByPlayer = true;
        }
    }

    public void Init(FSM inFSM)
    {
        _fsm = inFSM;
        _stats = inFSM.blackboard.Map;
        _maxHealth = (float)_stats["MaxHp"];
        _currentAbsorbablePercent = absorbablePercent;     // 보호막이 흡수할 수 있는 대미지의 양은 몬스터의 최대 체력의 10%
        _currentBarrierHealth = barrierHealth;
    }
}
