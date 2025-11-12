using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct StatPair
{
    public string StatName;
    public float StatValue;

    public StatPair(string name, float value)
    {
        StatName = name;
        StatValue = value;
    }
}

[System.Serializable]
public class StatListView
{
    public List<StatData> stats = new List<StatData>();

    public void FromDictionary(StatDictionary dict)
    {
        stats.Clear();
        foreach (var s in dict.GetAllStats())
            stats.Add(s.Clone());
    }

    public StatDictionary ToDictionary()
    {
        var dict = new StatDictionary();
        foreach (var s in stats)
            dict.SetStat(s.Clone());
        return dict;
    }
}

[System.Serializable]
public class PartStatView
{
    public EPartType partType;
    public StatListView stats = new StatListView();
}

public class CharacterStat : MonoBehaviour
{
    #region Variables
    [Header("플레이어의 종합 능력치")]
    [SerializeField] private StatListView totalStatsView = new StatListView();

    [Header("파츠별 최종 능력치")]
    [SerializeField] private List<PartStatView> combinedStatsView = new List<PartStatView>();

    [Header("플레이어의 기본 능력치")]
    [SerializeField] private StatListView baseStatsView = new StatListView();

    [Header("플레이어가 착용 중인 파츠 능력치")]
    [SerializeField] private List<PartStatView> partStatsView = new List<PartStatView>();

    [Header("플레이어에게 적용된 효과 목록")]
    [SerializeField] private List<StatModifier> modifiersView = new List<StatModifier>();

    // private float _currentTotalHealth;
    private float _currentBodyHealth;
    private float _currentPartHealth;
    
    private StatDictionary _baseStats = new();                                  // 캐릭터 기본 스탯
    private Dictionary<EPartType, StatDictionary> _partStats = new();           // 파츠 부위별 스탯
    private List<StatModifier> _modifiers = new();                              // 버프, 스킬 등 기타 스탯
    private Dictionary<EPartType, StatDictionary> _combinedPartStats = new();   // 기본 + 기타 스탯 보정을 포함하는 개별 파츠 스탯 (공격 속도 등 개별적으로 적용되는 파츠에서 사용)
    private StatDictionary _totalStats;                                         // 기본 + 파츠 + 기타 스탯
    #endregion

    #region Properties
    public StatDictionary BaseStats
    {
        get { return _baseStats; }
        set { _baseStats = value; }
    }
    
    public Dictionary<EPartType, StatDictionary> PartStats
    {
        get { return _partStats; }
    }

    public Dictionary<EPartType, StatDictionary> CombinedPartStats
    {
        get { return _combinedPartStats; }
        set { _combinedPartStats = value; }
    }

    public StatDictionary TotalStats
    {
        get { return _totalStats; }
        set { _totalStats = value; }
    }
    
    // 현재 체력값의 Getter/Setter
    public float CurrentHealth => _currentBodyHealth + _currentPartHealth;
    
    public float CurrentPartHealth
    {
        get => _currentPartHealth;
        set => _currentPartHealth = value;
    }
    public float CurrentBodyHealth
    {
        get => _currentBodyHealth;
        set => _currentBodyHealth = value;
    }

    public float MaxHealth => MaxBodyHealth + MaxPartHealth;            // 최대 체력 (몸통 + 파츠)
    public float MaxBodyHealth => _totalStats[EStatType.MaxHp].value;
    public float MaxPartHealth
    {
        get
        {
            if (_totalStats.Contains(EStatType.MaxHp))
            {
                return _totalStats[EStatType.AddHp].value;
            }

            return 0.0f;
        }
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        // Part Stat은 파츠 부위 종류만큼 미리 생성
        // To-do: 베이스 파츠는 기본 장착 파츠이므로 이를 기준으로 초기화하도록 수정 가능
        foreach (EPartType type in Enum.GetValues(typeof(EPartType)))
        {
            if (_partStats.ContainsKey(type)) continue;

            _partStats[type] = new StatDictionary();
            _combinedPartStats[type] = new StatDictionary();
        }

        // Base Stat 초기화
        GoogleSheetLoader baseParam = Resources.Load<GoogleSheetLoader>("Params/ParamDatas");
        if (baseParam != null)
        {
            var row = baseParam.GetRow("CharacterStats", 1001);
            if (row != null)
            {
                InitializeFromRow(row);
            }
            else
            {
                Debug.LogWarning("Bast Stat Index(4001)에 해당하는 데이터가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("Base Stat 데이터가 없습니다.");
        }

        if (_baseStats == null) return;

        //_currentBodyHealth = _baseStats[EStatType.MaxHp].value;
        //_currentPartHealth = CalculatePartHealth();

        _currentBodyHealth = MaxBodyHealth;     // 캐릭터의 바디 체력 값 초기화
        _currentPartHealth = MaxPartHealth;     // 기본 파츠 체력 총합 초기화

        SyncToInspector();
    }
    #endregion

    #region Public Methods

    public void InitializeFromRow(RowData row)
    {
        _baseStats = row.Stats.Clone();
        CalculateTotalStats();
    }

    // 파츠 교체 시 실행되는 함수
    // 파츠 교체와 함께 Total Stat을 갱신
    public void SetPartStats(PartBase part)
    {
        _partStats[part.PartType] = part.Stats ?? new StatDictionary();
        CalculateTotalStats();

        _currentPartHealth = MaxPartHealth;
        // Debug.Log($"파츠 교체: Body Hp({_currentBodyHealth}), Part Hp({_currentPartHealth})");
    }

    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        CalculateTotalStats();
    }

    public void AddModifier(List<StatModifier> mods)
    {
        foreach (var mod in mods)
        {
            _modifiers.Add(mod);
        }
        CalculateTotalStats();
    }

    public void RemoveModifier(object source)
    {
        _modifiers.RemoveAll(m => m.source == source);
        CalculateTotalStats();
    }

    public void CalculateStatsForced()
    {
        CalculateTotalStats();
    }

    // 동기화 메서드들
    public void SyncToInspector()
    {
        baseStatsView.FromDictionary(_baseStats);
        totalStatsView.FromDictionary(_totalStats);

        partStatsView.Clear();
        foreach (var kvp in _partStats)
        {
            var ps = new PartStatView { partType = kvp.Key };
            ps.stats.FromDictionary(kvp.Value);
            partStatsView.Add(ps);
        }

        combinedStatsView.Clear();
        foreach (var kvp in _combinedPartStats)
        {
            var ps = new PartStatView { partType = kvp.Key };
            ps.stats.FromDictionary(kvp.Value);
            combinedStatsView.Add(ps);
        }

        modifiersView = new List<StatModifier>(_modifiers);
    }

    public void SyncFromInspector()
    {
        _baseStats = baseStatsView.ToDictionary();

        _partStats.Clear();
        foreach (var pv in partStatsView)
            _partStats[pv.partType] = pv.stats.ToDictionary();

        _modifiers = new List<StatModifier>(modifiersView);

        CalculateTotalStats();
    }

    public override string ToString()
    {
        string baseStatsStr = _baseStats != null ? _baseStats.ToString() : "Null";
        string totalStatsStr = _totalStats != null ? _totalStats.ToString() : "Null";

        string partStatsSummary = string.Join(", ", _partStats.Select(kvp =>
        {
            string stats = kvp.Value != null ? kvp.Value.ToString() : "Null";
            return $"{kvp.Key}: {stats}";
        }));

        string modifiersSummary = string.Join(", ", _modifiers.Select(m => $"{m.statType}({m.modifierType}): {m.value:F2}"));

        return $"CharacterStat:" +
               $"\n  CurrentHealth: {CurrentHealth:F2} / MaxHealth: {MaxHealth:F2}" +
               $"\n  BaseStats: {baseStatsStr}" +
               $"\n  TotalStats: {totalStatsStr}" +
               $"\n  PartStats: [{partStatsSummary}]" +
               $"\n  Modifiers: [{modifiersSummary}]";
    }
    #endregion

    #region Private Methods
    // Total Stat(최종적으로 사용하는 스탯)을 갱신하는 함수
    // Combined Part Stats도 함께 갱신
    // To-do: Total Stats용 파라미터와 CPS용 파라미터 분리 고려
    private void CalculateTotalStats()
    {
        // Stat Type 중 Base Stat에 해당하는 스탯을 Total Stats에 추가
        _totalStats = _baseStats.Clone();
        foreach (EPartType type in Enum.GetValues(typeof(EPartType)))
        {
            _combinedPartStats[type] = _baseStats.Clone();
        }

        // Stat Type 중 Part Stat에 해당하는 스탯을 Total Stats에 추가
        // 이미 Base Stat을 통해 추가된 상태라면 값만 갱신
        foreach (EPartType type in Enum.GetValues(typeof(EPartType)))
        {
            StatDictionary partStats;
            _partStats.TryGetValue(type, out partStats);
            if (partStats == null)
            {
                partStats = new StatDictionary();
            }

            foreach (var stat in partStats.GetAllStats())
            {
                var target = _totalStats[stat.statType];
                if (target != null)
                {
                    if (stat.statType == EStatType.DamageReductionRate)
                    {
                        target.MultiplyValue(stat.value);
                        continue;
                    }

                    target.AddValue(stat.value);
                }
                else
                {
                    _totalStats.SetStat(stat);
                }

                var partTarget = _combinedPartStats[type][stat.statType];
                if (partTarget != null)
                {
                    if (stat.statType == EStatType.DamageReductionRate)
                    {
                        target.MultiplyValue(stat.value);
                        continue;
                    }

                    partTarget.AddValue(stat.value);
                }
                else
                {
                    _combinedPartStats[type].SetStat(stat);
                }
            }
        }

        // Modifiers 적용 (Flat → PercentAdd → PercentMul 순)
        foreach (var stat in _totalStats.GetAllStats())
        {
            float finalValue = stat.value;

            foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.Flat))
            {
                finalValue += mod.value;
            }
            foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentAdd))
            {
                finalValue += stat.value * mod.value;
            }
            foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentMul))
            {
                if (stat.statType == EStatType.DamageReductionRate)
                {
                    finalValue *= (1 - mod.value);
                    continue;
                }

                finalValue *= (1 + mod.value);
            }

            stat.SetValue(finalValue);
        }
        foreach (var stats in _combinedPartStats.Values)
        {
            foreach (var stat in stats.GetAllStats())
            {
                float finalValue = stat.value;

                foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.Flat))
                {
                    finalValue += mod.value;
                }
                foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentAdd))
                {
                    finalValue += stat.value * mod.value;
                }
                foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentMul))
                {
                    finalValue *= (1 + mod.value);
                }

                stat.SetValue(finalValue);
            }
        }

        SyncToInspector();
    }
    
    // 캐릭터가 착용 중인 파츠별 HP 값을 총합하여 반환
    private float CalculatePartHealth()
    {
        var result = 0f;
        
        foreach (StatDictionary partStats in _partStats.Values)
        {
            if (partStats.IsEmpty()) continue;

            foreach (EStatType type in System.Enum.GetValues(typeof(EStatType)))
            {
                if (partStats[type] == null)  continue;
                
                if (partStats[type].statType == EStatType.MaxHp)
                    result += partStats[type].value;
            }
        }
        
        return result;
    }

    private float CalculateBodyHealth()
    {
        return 0f;
    }
    
    #endregion
}
