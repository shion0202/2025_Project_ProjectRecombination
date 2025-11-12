using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatModifier
{
    public EStatType statType;
    public EStackType modifierType;
    public float value;
    public object source;                       // 이 Modifier를 만든 객체 (버프, 스킬 등)

    public StatModifier(EStatType type, EStackType modifierType, float value, object source = null)
    {
        statType = type;
        this.modifierType = modifierType;
        this.value = value;
        this.source = source;
    }

    public override string ToString()
    {
        string sourceStr = source != null ? source.ToString() : "null";
        string log = $"[{GetType().Name}] Stat Type: {statType}, Modifier Type: {modifierType}, Value: {value:F2}, Source: {sourceStr}";
        return log;
    }
}
