using Monster;
using Monster.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PartBase : MonoBehaviour
{
    [Header("파츠 기본 설정")]
    [SerializeField] protected int partId = 2000;
    [SerializeField] protected EPartType partType;
    [SerializeField] protected EAttackType attackType;
    [SerializeField] protected EPartMeshType meshType;
    [SerializeField] protected Vector3 staticOffset = Vector3.zero;
    [SerializeField] protected Vector3 staticRotation = Vector3.zero;

    protected PlayerController _owner;
    protected StatDictionary _stats = new();
    protected List<StatModifier> _partModifiers = new();

    protected bool _isAnimating = true;

    public EPartType PartType => partType;
    public EAttackType AttackType => attackType;
    public EPartMeshType MeshType => meshType;
    public StatDictionary Stats => _stats;
    public List<StatModifier> PartModifiers => _partModifiers;
    public bool IsAnimating => _isAnimating;
    public Vector3 StaticOffset => staticOffset;
    public Vector3 StaticRotation => staticRotation;

    public abstract void UseAbility();
    public abstract void UseCancleAbility();
    public abstract void FinishActionForced();

    public void Init(PlayerController owner)
    {
        SetOwner(owner);
        SetPartStat();
    }

    public void SetOwner(PlayerController owner)
    {
        _owner = owner;
    }

    public void SetPartStat()
    {
        GoogleSheetLoader baseParam = Resources.Load<GoogleSheetLoader>("Params/ParamDatas");
        if (baseParam != null)
        {
            var row = baseParam.GetRow("CharacterParts", partId);
            if (row != null)
            {
                InitializeFromRow(row);
            }
            else
            {
                Debug.LogWarning($"Part Stat Index({partId}) 데이터가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("파츠 파라미터 데이터가 없습니다.");
        }
    }

    public void InitializeFromRow(RowData row)
    {
        _stats = row.Stats.Clone();
    }

    public void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        IDamagable monster = target.GetComponent<IDamagable>();
        if (monster != null)
        {
            monster.ApplyDamage((_owner.Stats.CombinedPartStats[partType][EStatType.Damage].value * coefficient));
        }
        else
        {
            monster = target.transform.GetComponentInParent<IDamagable>();
            if (monster != null)
            {
                monster.ApplyDamage((_owner.Stats.CombinedPartStats[partType][EStatType.Damage].value * coefficient));
            }
        }
    }
}
