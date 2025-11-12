using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 개별 스탯 데이터들을 Dictionary 형태로 관리하는 클래스
public class StatDictionary
{
    private Dictionary<EStatType, StatData> _statDict = new();

    // Indexer 문법: 외부에서 StatDictionary[EStatType] 형태로 접근 가능
    public StatData this[EStatType type]
    {
        get => _statDict.TryGetValue(type, out var stat) ? stat : null;
        set => _statDict[type] = value;
    }

    public void SetStat(StatData newStat) => _statDict[newStat.statType] = newStat.Clone();

    public void SetStat(EStatType type, float value = 0f, string stringValue = "", float min = float.MinValue, float max = float.MaxValue)
    {
        if (_statDict.TryGetValue(type, out var stat))
        {
            stat.SetValue(value);
        }
        else
        {
            _statDict[type] = new StatData(type, value, stringValue, min, max);
        }
    }

    public StatDictionary Clone()
    {
        var clone = new StatDictionary();
        foreach (var kvp in _statDict)
        {
            clone._statDict[kvp.Key] = kvp.Value.Clone();
        }
        return clone;
    }

    public void Clear()
    {
        _statDict.Clear();
    }

    public IEnumerable<StatData> GetAllStats() => _statDict.Values;

    public void RemoveStat(EStatType type) => _statDict.Remove(type);

    public bool Contains(EStatType type) => _statDict.ContainsKey(type);

    public bool IsEmpty() => _statDict.Count == 0;

    public override string ToString()
    {
        if (_statDict.Count == 0)
        {
            return "[StatDictionary] Dictionary is Empty.";
        }
            
        string statsStr = "";
        foreach (var stat in _statDict.Values)
        {
            statsStr += $"{stat.statType}: {(!string.IsNullOrEmpty(stat.stringValue) ? $"\"{stat.stringValue}\"" : stat.value.ToString("F2"))}, ";
        }
        statsStr = statsStr.TrimEnd(',', ' ');

        return $"[{GetType().Name}] {{ {statsStr} }}";
    }
}
