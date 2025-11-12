using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class DamagableObject : MonoBehaviour, IDamagable
{
    [Header("Information")]
    [SerializeField, Tooltip("인스펙터 확인 및 디버그용이며, 하단의 Stats 값을 변경하는 것이 좋습니다.")] private float _currentHp = 0.0f;
    private StatDictionary _stats = new();

    [Header("Object Stats")]
    [SerializeField] private List<StatPair> statList = new();

    public event Action OnObjectDied;

    public StatDictionary Stats
    {
        get => _stats;
        set => _stats = value;
    }

    public List<StatPair> StatList
    {
        get => statList;
        set => statList = value;
    }

    public bool IsDead => _currentHp <= 0;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        if (_currentHp <= 0.0f)
        {
            _currentHp = GetMaxHp();
        }
    }

    public void ApplyDamage(float inDamage)
    {
        if (inDamage <= 0) return;
        if (IsDead) return;

        float damage = (inDamage - (_stats[EStatType.Defence].value + _stats[EStatType.AddDefence].value)) * (1 - _stats[EStatType.DamageReductionRate].value);
        if (damage <= 0.0f)
        {
            damage = 1.0f;  // 최소 데미지
        }

        _currentHp -= damage;
        if (_currentHp <= 0)
        {
            _currentHp = 0;
            OnDie();
        }
    }

    private void Init()
    {
        _stats.Clear();

        foreach (var statPair in statList)
        {
            if (System.Enum.TryParse<EStatType>(statPair.StatName, out var statType))
            {
                _stats[statType] = new StatData(statType, statPair.StatValue);
            }
            else
            {
                Debug.LogWarning($"Invalid StatName found in statList: {statPair.StatName}");
            }
        }
    }

    private float GetMaxHp()
    {
        if (_stats.Contains(EStatType.MaxHp))
        {
            return _stats[EStatType.MaxHp].value;
        }

        return 0.0f;
    }

    private void OnDie()
    {
        // 오브젝트가 사망했음을 외부에 알림
        OnObjectDied?.Invoke();
        Debug.Log(gameObject.name + " is Dead.");
    }

    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"[{gameObject.name} ({GetType().Name})] Current Hp: {_currentHp:F2}");
        sb.AppendLine("Stats:");
        foreach (var stat in _stats.GetAllStats())
        {
            sb.AppendLine($"  {stat.statType}: {stat.value:F2}");
        }

        return sb.ToString();
    }
}
