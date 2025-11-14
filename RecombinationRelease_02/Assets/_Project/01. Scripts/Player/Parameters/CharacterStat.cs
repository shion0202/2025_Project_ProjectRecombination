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

    private float _currentHealth;
    
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

    // 현재 체력 관련 Getter/Setter
    public float CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }

    public float MaxHealth => MaxBodyHealth + MaxPartHealth;                // 최대 체력 (몸통 + 파츠)
    public float MaxBodyHealth => _totalStats[EStatType.MaxHp].value;
    public float MaxPartHealth
    {
        get
        {
            if (_totalStats.Contains(EStatType.AddHp))
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

        _currentHealth = MaxHealth;

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
    }

    // To-do: 버프/디버프 완성 어려울 경우 Modifier로 임시 버프 사용
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

            // To-do: mod 종류에 따라 다른 buff Icon을 생성/활성화
            // 생성된 버프 아이콘 소스가 무엇인지 파악 필요
            Managers.GUIManager.Instance.BuffIcon.SetActive(true);
        }
        CalculateTotalStats();
    }

    public void RemoveModifier(object source)
    {
        _modifiers.RemoveAll(m => m.source == source);
        CalculateTotalStats();

        // 삭제된 소스에 맞춰 버프 아이콘 삭제/비활성화
        Managers.GUIManager.Instance.BuffIcon.SetActive(false);
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
            if (!_partStats.TryGetValue(type, out var partStats) || partStats == null) continue;

            foreach (var stat in partStats.GetAllStats())
            {
                AddOrUpdateStat(_totalStats, stat);
                AddOrUpdateStat(_combinedPartStats[type], stat);
            }
        }

        // Modifier를 statType별로 그룹핑해서 캐싱
        var modsByType = _modifiers.GroupBy(m => m.statType)
                                   .ToDictionary(g => g.Key, g => g.ToList());

        // Modifier 연산 적용
        ApplyModifiersToDict(_totalStats, modsByType);
        foreach (var stats in _combinedPartStats.Values)
        {
            ApplyModifiersToDict(stats, modsByType);
        }

        SyncToInspector();
    }

    private void AddOrUpdateStat(StatDictionary dict, StatData stat)
    {
        // 이미 statType이 존재하면 값 누적, 없으면 새로 추가
        StatData targetStat = dict[stat.statType];
        if (targetStat != null)
        {
            targetStat.AddValue(stat.value);
        }
        else
        {
            dict.SetStat(stat);
        }
    }

    private void ApplyModifiersToDict(StatDictionary dict, Dictionary<EStatType, List<StatModifier>> modsByType)
    {
        foreach (var stat in dict.GetAllStats())
        {
            float baseValue = stat.value;

            if (modsByType.TryGetValue(stat.statType, out var modsForType))
            {
                // Flat → PercentAdd → PercentMul 순서로 계산
                float flat = modsForType.Where(m => m.modifierType == EStackType.Flat).Sum(m => m.value);
                float percentAdd = modsForType.Where(m => m.modifierType == EStackType.PercentAdd).Sum(m => m.value);
                float percentMul = modsForType.Where(m => m.modifierType == EStackType.PercentMul).Sum(m => m.value);

                baseValue += flat;
                baseValue += stat.value * percentAdd;
                baseValue *= (1 + percentMul);
            }

            stat.SetValue(baseValue);
        }

        // Modifiers 적용 (Flat → PercentAdd → PercentMul 순)
        //foreach (var stat in _totalStats.GetAllStats())
        //{
        //    float finalValue = stat.value;

        //    foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.Flat))
        //    {
        //        finalValue += mod.value;
        //    }
        //    foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentAdd))
        //    {
        //        finalValue += stat.value * mod.value;
        //    }
        //    foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentMul))
        //    {
        //        finalValue *= (1 + mod.value);
        //    }

        //    stat.SetValue(finalValue);
        //}
        //foreach (var stats in _combinedPartStats.Values)
        //{
        //    foreach (var stat in stats.GetAllStats())
        //    {
        //        float finalValue = stat.value;

        //        foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.Flat))
        //        {
        //            finalValue += mod.value;
        //        }
        //        foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentAdd))
        //        {
        //            finalValue += stat.value * mod.value;
        //        }
        //        foreach (var mod in _modifiers.Where(m => m.statType == stat.statType && m.modifierType == EStackType.PercentMul))
        //        {
        //            finalValue *= (1 + mod.value);
        //        }

        //        stat.SetValue(finalValue);
        //    }
        //}
    }
    #endregion
}
