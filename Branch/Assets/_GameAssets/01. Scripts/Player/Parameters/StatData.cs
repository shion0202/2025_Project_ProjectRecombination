using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 개별 스탯 데이터 클래스
[System.Serializable]
public class StatData
{
    public EStatType statType;
    public string stringValue;
    public float value;
    public float minValue;
    public float maxValue;

    public StatData(EStatType type, float value, string stringValue, float min = float.MinValue, float max = float.MaxValue)
    {
        statType = type;
        minValue = min;
        maxValue = max;
        SetValue(value);
        SetValue(stringValue);
    }

    public StatData(EStatType type, float value = 0f,float min = float.MinValue, float max = float.MaxValue)
    {
        statType = type;
        minValue = min;
        maxValue = max;
        SetValue(value);
        SetValue(string.Empty);
    }

    public StatData(EStatType type, string stringValue, float min = float.MinValue, float max = float.MaxValue)
    {
        statType = type;
        minValue = min;
        maxValue = max;
        SetValue(0.0f);
        SetValue(stringValue);
    }

    public void SetValue(float newValue)
    {
        value = Mathf.Clamp(newValue, minValue, maxValue);
    }
    
    public void SetValue(string newStringValue)
    {
        stringValue = newStringValue;
    }

    public void AddValue(float add)
    {
        SetValue(value + add);
    }

    public void MultiplyValue(float multiplier)
    {
        SetValue(value * multiplier);
    }

    public float GetValue() => value;
    public string GetStringValue() => stringValue;

    public StatData Clone()
    {
        return new StatData(statType, value, stringValue, minValue, maxValue);
    }

    public override string ToString()
    {
        string valStr = !string.IsNullOrEmpty(stringValue) ? $"\"{stringValue}\"" : value.ToString("F2");
        string log = $"[{GetType().Name}] Stat Type: {statType}, Value: {valStr}, Min Value: {minValue:F2}, Max Value: {maxValue:F2}";
        return log;
    }
}
