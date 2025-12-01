using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigAimController : MonoBehaviour
{
    [SerializeField] private Transform targetObject;
    private Transform _ownerTransform;
    private Dictionary<string, MultiAimConstraint> _constraints = new();
    private Dictionary<string, Coroutine> _changeRoutines = new();

    [SerializeField] private float weightChangeSpeed = 10.0f;
    [SerializeField, Range(0, 180)] private float maxYawAngle = 90.0f;
    private float _currentWeight = 0.0f;
    private bool _isAim = false;

    public bool IsAim
    {
        get => _isAim;
        set => _isAim = value;
    }

    private void Awake()
    {
        _ownerTransform = transform;

        MultiAimConstraint[] constraintArray = gameObject.GetComponentsInChildren<MultiAimConstraint>();
        foreach (MultiAimConstraint constraint in constraintArray)
        {
            constraint.weight = 0.0f;
            _constraints.Add(constraint.gameObject.name, constraint);
            _changeRoutines.Add(constraint.gameObject.name, null);
        }
    }

    private void LateUpdate()
    {
        if (_ownerTransform == null || targetObject == null) return;
        if (!_isAim) return;

        // 타겟 로컬 좌표 계산
        Vector3 localTargetPos = _ownerTransform.InverseTransformPoint(targetObject.position);
        float targetAngle = Mathf.Atan2(localTargetPos.x, localTargetPos.z) * Mathf.Rad2Deg;

        // 제한 각도 넘으면 weight 줄이고, 아니면 늘림
        if (targetAngle >= maxYawAngle || targetAngle <= -maxYawAngle)
        {
            _currentWeight -= weightChangeSpeed * Time.deltaTime;
        }
        else
        {
            _currentWeight += weightChangeSpeed * Time.deltaTime;
        }

        // 0~1 clamp
        _currentWeight = Mathf.Clamp01(_currentWeight);
        SetBaseWeight(_currentWeight);
    }

    public void SetWeight(string key, float inWeight)
    {
        if ( _constraints.ContainsKey(key))
        {
            _constraints[key].weight = inWeight;
        }
    }

    public void SmoothChangeWeight(string key, bool isIncreasing = true, float changeSpeed = 0.0f)
    {
        if (!_constraints.ContainsKey(key)) return;

        if (changeSpeed <= 0.0f)
        {
            changeSpeed = weightChangeSpeed;
        }

        if (_changeRoutines[key] != null)
        {
            StopCoroutine(_changeRoutines[key]);
        }
        _changeRoutines[key] = StartCoroutine(CoSmoothChangeWeight(key, isIncreasing, changeSpeed));
    }

    public void SetBaseWeight(float inWeight)
    {
        SetWeight("HeadAim", inWeight);
        SetWeight("ChestAimX", inWeight);
    }
    
    public void SetWeaponWeight(float inWeight)
    {
        SetWeight("ArmLAim", inWeight);
        SetWeight("ArmRAim", inWeight);
    }

    public void SetAllWeight(float inWeight)
    {
        foreach (var constraint in _constraints.Keys)
        {
            SetWeight(constraint, inWeight);
        }
    }

    public void SmoothChangeBaseWeight(bool isIncreasing = true, float changeSpeed = 0.0f)
    {
        SmoothChangeWeight("HeadAim", isIncreasing, changeSpeed);
        SmoothChangeWeight("ChestAimX", isIncreasing, changeSpeed);
    }

    public void SmoothChangeWeaponWeight(bool isIncreasing = true, float changeSpeed = 0.0f)
    {
        SmoothChangeWeight("ArmLAim", isIncreasing, changeSpeed);
        SmoothChangeWeight("ArmRAim", isIncreasing, changeSpeed);
    }

    public void SmoothChangeAllWeight(bool isIncreasing = true, float changeSpeed = 0.0f)
    {
        foreach (var constraint in _constraints.Keys)
        {
            SmoothChangeWeight(constraint, isIncreasing, changeSpeed);
        }
    }

    public void ClearWeight(float inWeight)
    {
        foreach (var routine in _changeRoutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }

        SetAllWeight(inWeight);
    }

    private IEnumerator CoSmoothChangeWeight(string key, bool isIncreasing, float changeSpeed)
    {
        float value = _constraints[key].weight;
        float weightOperator = changeSpeed * (isIncreasing ? 1 : -1);

        while (true)
        {
            value += Time.deltaTime * weightOperator;
            value = Mathf.Clamp01(value);
            _constraints[key].weight = value;

            if (isIncreasing && value >= 1.0f) break;
            else if (!isIncreasing && value <= 0.0f) break;
            yield return null;
        }

        _changeRoutines[key] = null;
        yield break;
    }

    public override string ToString()
    {
        string targetName = targetObject != null ? targetObject.name : "Null";
        string ownerName = _ownerTransform != null ? _ownerTransform.name : "Null";

        string constraintsInfo = "";
        foreach (var kvp in _constraints)
        {
            constraintsInfo += $"({kvp.Key}) Weight: {kvp.Value.weight:F2}, ";
        }
        constraintsInfo = constraintsInfo.TrimEnd(',', ' ');

        string log = $"[{gameObject.name} ({GetType().Name})] Owner: {ownerName}, Target: {targetName}, IsAim: {_isAim}, " +
            $"Weight Change Speed: {weightChangeSpeed:F2}, Max Yaw Angle: {maxYawAngle:F2}, Constraints: {{ {constraintsInfo} }}";
        return log;
    }
}
