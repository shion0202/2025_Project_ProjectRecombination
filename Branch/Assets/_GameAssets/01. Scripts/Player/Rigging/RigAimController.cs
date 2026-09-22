using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigAimController : MonoBehaviour
{
    [SerializeField] private Transform targetObject;
    private Transform _ownerTransform;
    // 조준 제약(MultiAim)과 팔 IK(TwoBoneIK)를 같은 방식으로 다룬다. 키는 제약 오브젝트 이름이다.
    private Dictionary<string, IRigConstraint> _constraints = new();
    private Dictionary<string, Coroutine> _changeRoutines = new();
    // 제약별 최대 가중치 설정. 플레이 중 조절한 값이 바로 반영되도록 값이 아니라 컴포넌트를 들고 있는다.
    private Dictionary<string, AimWeightScale> _weightScales = new();
    // 0~1로 관리하는 가중치. 실제 제약 가중치는 여기에 최대 가중치(AimWeightScale)를 곱한 값이다.
    private Dictionary<string, float> _normalizedWeights = new();
    private readonly List<string> _groupBuffer = new();

    [SerializeField] private float weightChangeSpeed = 10.0f;
    [SerializeField, Range(0, 180)] private float maxYawAngle = 90.0f;
    [Tooltip("한 팔 사격과 양팔 사격의 최대 가중치 사이를 오가는 속도")]
    [SerializeField] private float dualBlendSpeed = 6.0f;
    private float _currentWeight = 0.0f;
    private float _dualBlend = 0.0f;
    private bool _isAim = false;

    private bool _isInit;

    public bool IsAim
    {
        get => _isAim;
        set => _isAim = value;
    }

    // 양팔을 함께 사격 중인지. PlayerController가 매 프레임 갱신한다.
    public bool IsDualAim { get; set; }

    // 최대 가중치는 양팔 사격 여부와 인스펙터 조절값에 따라 바뀌므로 매 프레임 다시 적용한다.
    private void Update()
    {
        if (!_isInit) return;

        _dualBlend = Mathf.MoveTowards(_dualBlend, IsDualAim ? 1.0f : 0.0f, dualBlendSpeed * Time.deltaTime);

        foreach (string name in _constraints.Keys)
        {
            ApplyWeight(name);
        }
    }

    private void LateUpdate()
    {
        if (!_isInit) return;

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

    /// <summary>
    /// key와 이름이 같거나 "key_"로 시작하는 제약들을 한 그룹으로 모은다.
    /// 예: "ArmRAim"은 위팔 제약 ArmRAim과 전완 제약 ArmRAim_Forearm을 함께 다룬다.
    /// </summary>
    private List<string> GetGroup(string key)
    {
        _groupBuffer.Clear();
        foreach (string name in _constraints.Keys)
        {
            if (name == key || name.StartsWith(key + "_"))
            {
                _groupBuffer.Add(name);
            }
        }

        return _groupBuffer;
    }

    // AimWeightScale이 없으면 1이다. 한 팔/양팔 사격 상한 사이를 _dualBlend로 섞는다.
    private float GetMaxWeight(string name)
    {
        AimWeightScale scale = _weightScales[name];
        if (scale == null) return 1.0f;

        return Mathf.Lerp(scale.maxWeight, scale.dualMaxWeight, _dualBlend);
    }

    private void ApplyWeight(string name)
    {
        _constraints[name].weight = _normalizedWeights[name] * GetMaxWeight(name);
    }

    public void SetWeight(string key, float inWeight)
    {
        foreach (string name in GetGroup(key))
        {
            _normalizedWeights[name] = Mathf.Clamp01(inWeight);
            ApplyWeight(name);
        }
    }

    // delay: 변화를 시작하기 전 대기 시간. 대기 중에 다시 호출되면 이전 요청은 취소된다.
    public void SmoothChangeWeight(string key, bool isIncreasing = true, float changeSpeed = 0.0f, float delay = 0.0f)
    {
        if (changeSpeed <= 0.0f)
        {
            changeSpeed = weightChangeSpeed;
        }

        foreach (string name in GetGroup(key))
        {
            if (_changeRoutines[name] != null)
            {
                StopCoroutine(_changeRoutines[name]);
            }
            _changeRoutines[name] = StartCoroutine(CoSmoothChangeWeight(name, isIncreasing, changeSpeed, delay));
        }
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

    private IEnumerator CoSmoothChangeWeight(string key, bool isIncreasing, float changeSpeed, float delay = 0.0f)
    {
        if (delay > 0.0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float value = _normalizedWeights[key];
        float weightOperator = changeSpeed * (isIncreasing ? 1 : -1);

        while (true)
        {
            value += Time.deltaTime * weightOperator;
            value = Mathf.Clamp01(value);
            _normalizedWeights[key] = value;
            ApplyWeight(key);

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

    public void Init(GameObject getComponentInChildren)
    {
        if (_isInit) return;

        _ownerTransform = transform;
        targetObject = getComponentInChildren.transform;

        // 팔 조준은 팔마다 별도 타깃을 쓴다. 같은 점을 겨누면 애니메이션 포즈에 따라 한쪽 팔이 바깥으로 벌어져 보여,
        // 타깃 위치를 팔별로 옮겨 보정한다. 타깃은 IKTargets 프리팹에서 AimTarget의 형제 오브젝트로 둔다.
        Transform leftArmTarget = FindSiblingTarget("AimTargetL");
        Transform rightArmTarget = FindSiblingTarget("AimTargetR");

        foreach (MultiAimConstraint constraint in gameObject.GetComponentsInChildren<MultiAimConstraint>())
        {
            string constraintName = constraint.gameObject.name;
            Transform source = targetObject;
            if (constraintName.StartsWith("ArmLAim")) source = leftArmTarget;
            else if (constraintName.StartsWith("ArmRAim")) source = rightArmTarget;

            var data = constraint.data;
            var sources = constraint.data.sourceObjects;
            sources.Clear();
            sources.Add(new WeightedTransform(source, 1.0f));
            data.sourceObjects = sources;
            constraint.data = data;

            Register(constraint);
        }

        // 팔 IK. 이름을 ArmLAim_IK / ArmRAim_IK처럼 지으면 팔 조준 그룹에 포함되어 사격 시 함께 켜진다.
        // 타깃과 힌트 위치는 ArmShootIKTargets가 매 프레임 계산한다.
        foreach (TwoBoneIKConstraint constraint in gameObject.GetComponentsInChildren<TwoBoneIKConstraint>())
        {
            Register(constraint);
        }

        // 비활성 오브젝트의 제약은 등록되지 않고 리그에서도 평가되지 않는다. 설정 확인용으로 목록을 남긴다.
        Debug.Log($"[RigAimController] 등록된 제약: {string.Join(", ", _constraints.Keys)}");

        _isInit = true;
    }

    private void Register(IRigConstraint constraint)
    {
        string constraintName = constraint.component.gameObject.name;
        constraint.weight = 0.0f;

        _constraints.Add(constraintName, constraint);
        _changeRoutines.Add(constraintName, null);
        _weightScales.Add(constraintName, constraint.component.GetComponent<AimWeightScale>());
        _normalizedWeights.Add(constraintName, 0.0f);
    }

    public bool IsInit => _isInit;

    // 그룹 내 제약들의 0~1 가중치 중 가장 큰 값. 팔 IK가 켜져 있는지 판단할 때 쓴다.
    public float GetNormalizedWeight(string key)
    {
        float result = 0.0f;
        foreach (string name in GetGroup(key))
        {
            result = Mathf.Max(result, _normalizedWeights[name]);
        }

        return result;
    }

    // 팔별 조준 타깃(AimTargetL/R). 없으면 기본 조준 타깃이다.
    public Transform GetArmAimTarget(bool isLeft)
    {
        if (targetObject == null) return null;
        return FindSiblingTarget(isLeft ? "AimTargetL" : "AimTargetR");
    }

    // 기본 조준 타깃과 같은 부모 아래의 이름이 같은 오브젝트. 없으면 기본 타깃을 쓴다.
    private Transform FindSiblingTarget(string targetName)
    {
        Transform parent = targetObject.parent;
        Transform found = parent != null ? parent.Find(targetName) : null;
        return found != null ? found : targetObject;
    }
}
