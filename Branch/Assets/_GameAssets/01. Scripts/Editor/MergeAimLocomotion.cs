using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// [1회 실행용] 사격 중 이동 애니메이션을 컨트롤러 교체 방식에서 블렌드 파라미터 방식으로 변환한다.
///
/// 기존에는 사격 시 Player_Aim* 오버라이드 컨트롤러로 통째로 교체했는데,
/// runtimeAnimatorController를 교체하면 모든 레이어가 초기화되어 이동 모션이 처음부터 다시 재생됐다.
/// 두 컨트롤러의 차이는 BT_Move의 이동 클립 4개뿐이므로, 이를 하나의 블렌드 트리로 합친다.
///
///  - BT_Move를 aimWeight(1D) 블렌드 트리로 감싸고, 0에는 기존 이동 트리, 1에는 사격 중 이동 트리를 둔다.
///  - 사격 중 이동 트리는 새 슬롯 클립(AimMoveSlot_*)을 원본으로 쓴다.
///    오버라이드 컨트롤러는 원본 클립을 키로 교체하므로, 기존 이동 클립과 다른 클립이어야 따로 지정할 수 있다.
///  - Player_Aim*에 지정돼 있던 이동 클립을 대응하는 일반 오버라이드 컨트롤러의 슬롯으로 옮긴다.
///
/// 변환 후에는 Player_Aim* 컨트롤러를 사용하지 않는다. 결과 확인 후 이 파일과 함께 정리한다.
/// 되돌리려면 Player.controller, Player_*.overrideController, AimMoveSlot_*.anim을 git에서 되돌린다.
/// </summary>
public static class MergeAimLocomotion
{
    private const string Folder = "Assets/_GameAssets/05. Art/04. Animations/Player/Animator/";
    private const string ControllerPath = Folder + "Player.controller";
    private const string MoveStateName = "BT_Move";
    private const string AimParameter = "aimWeight";

    private static readonly (string normal, string aim)[] OverridePairs =
    {
        ("Player_Base", "Player_AimBase"),
        ("Player_Hover", "Player_AimHover"),
        ("Player_Roller", "Player_AimRoller"),
        ("Player_Heavy", "Player_AimHeavy"),
    };

    [MenuItem("Tools/Merge Aim Locomotion Into Blend Tree")]
    private static void Run()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[MergeAimLocomotion] 컨트롤러를 찾을 수 없음: {ControllerPath}");
            return;
        }

        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == AimParameter)
            {
                Debug.LogWarning($"[MergeAimLocomotion] '{AimParameter}' 파라미터가 이미 있어 변환을 건너뜀 (이미 실행됨)");
                return;
            }
        }

        AnimatorState moveState = FindState(controller.layers[0].stateMachine, MoveStateName);
        if (moveState == null || !(moveState.motion is BlendTree moveTree))
        {
            Debug.LogError($"[MergeAimLocomotion] Base Layer에서 블렌드 트리를 가진 '{MoveStateName}' 상태를 찾을 수 없음");
            return;
        }

        if (!EditorUtility.DisplayDialog("Merge Aim Locomotion",
                "Player.controller와 Player_*.overrideController를 수정합니다.\n실행 전 커밋 상태인지 확인하세요.", "실행", "취소"))
        {
            return;
        }

        // 1. 사격 중 이동 트리와 슬롯 클립 생성
        ChildMotion[] aimChildren = moveTree.children;
        Dictionary<AnimationClip, AnimationClip> originalToSlot = new();

        for (int i = 0; i < aimChildren.Length; ++i)
        {
            if (!(aimChildren[i].motion is AnimationClip original))
            {
                Debug.LogError($"[MergeAimLocomotion] '{MoveStateName}'의 {i}번 자식이 AnimationClip이 아님. 변환 중단");
                return;
            }

            // 원본 클립을 복제해 루프 설정과 길이를 그대로 유지한다. (빈 클립이면 블렌드 트리 시간 동기화가 깨진다)
            AnimationClip slot = Object.Instantiate(original);
            slot.name = $"AimMoveSlot_{original.name}";
            AssetDatabase.CreateAsset(slot, AssetDatabase.GenerateUniqueAssetPath($"{Folder}{slot.name}.anim"));

            originalToSlot[original] = slot;
            aimChildren[i].motion = slot;
        }

        BlendTree aimTree = new BlendTree
        {
            name = "BT_Move_Aim",
            hideFlags = HideFlags.HideInHierarchy,
            blendType = moveTree.blendType,
            blendParameter = moveTree.blendParameter,
            blendParameterY = moveTree.blendParameterY,
            useAutomaticThresholds = moveTree.useAutomaticThresholds,
            minThreshold = moveTree.minThreshold,
            maxThreshold = moveTree.maxThreshold,
        };
        AssetDatabase.AddObjectToAsset(aimTree, controller);
        aimTree.children = aimChildren;

        // 2. aimWeight 루트 트리로 감싸기
        controller.AddParameter(AimParameter, AnimatorControllerParameterType.Float);

        moveTree.name = "BT_Move_Normal";

        BlendTree rootTree = new BlendTree
        {
            name = "BT_Move_Root",
            hideFlags = HideFlags.HideInHierarchy,
            blendType = BlendTreeType.Simple1D,
            blendParameter = AimParameter,
            useAutomaticThresholds = false,
        };
        AssetDatabase.AddObjectToAsset(rootTree, controller);
        rootTree.AddChild(moveTree, 0.0f);
        rootTree.AddChild(aimTree, 1.0f);

        moveState.motion = rootTree;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 3. Player_Aim*의 이동 클립을 일반 오버라이드 컨트롤러의 슬롯으로 이동
        foreach (var (normalName, aimName) in OverridePairs)
        {
            MergeOverride(controller, normalName, aimName, originalToSlot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[MergeAimLocomotion] 변환 완료. Player.controller의 BT_Move와 각 오버라이드 컨트롤러를 확인하세요.");
    }

    private static void MergeOverride(AnimatorController controller, string normalName, string aimName,
        Dictionary<AnimationClip, AnimationClip> originalToSlot)
    {
        var normal = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{Folder}{normalName}.overrideController");
        var aim = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{Folder}{aimName}.overrideController");
        if (normal == null || aim == null)
        {
            Debug.LogError($"[MergeAimLocomotion] {normalName} 또는 {aimName}을 찾을 수 없어 건너뜀");
            return;
        }

        // 원본 컨트롤러에 슬롯 클립이 추가됐으므로 다시 연결해 오버라이드 목록을 갱신한다.
        normal.runtimeAnimatorController = controller;

        var aimOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aim.overridesCount);
        aim.GetOverrides(aimOverrides);
        Dictionary<AnimationClip, AnimationClip> aimLookup = new();
        foreach (var pair in aimOverrides)
        {
            aimLookup[pair.Key] = pair.Value;
        }

        var normalOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(normal.overridesCount);
        normal.GetOverrides(normalOverrides);

        for (int i = 0; i < normalOverrides.Count; ++i)
        {
            AnimationClip key = normalOverrides[i].Key;

            // 이동 클립 외에 Aim 쪽과 다른 항목이 있으면 변환 후 사라지므로 알린다.
            if (!originalToSlot.ContainsKey(key) && !originalToSlot.ContainsValue(key)
                && aimLookup.TryGetValue(key, out AnimationClip aimValue) && aimValue != normalOverrides[i].Value)
            {
                Debug.LogWarning($"[MergeAimLocomotion] {normalName}/{aimName}: 이동 클립이 아닌 '{key.name}'의 오버라이드가 서로 다름. 사격 중 값은 반영되지 않음");
            }
        }

        foreach (KeyValuePair<AnimationClip, AnimationClip> slotPair in originalToSlot)
        {
            AnimationClip original = slotPair.Key;
            AnimationClip slot = slotPair.Value;

            // Aim에 오버라이드가 없으면 원본 이동 클립을 그대로 쓰던 것이므로 원본을 지정한다.
            AnimationClip aimClip = aimLookup.TryGetValue(original, out AnimationClip value) && value != null ? value : original;

            int index = normalOverrides.FindIndex(pair => pair.Key == slot);
            if (index < 0)
            {
                Debug.LogError($"[MergeAimLocomotion] {normalName}에서 슬롯 '{slot.name}'을 찾을 수 없음");
                continue;
            }

            normalOverrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(slot, aimClip);
            Debug.Log($"[MergeAimLocomotion] {normalName}: {slot.name} -> {aimClip.name}");
        }

        normal.ApplyOverrides(normalOverrides);
        EditorUtility.SetDirty(normal);
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == stateName) return child.state;
        }

        foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
        {
            AnimatorState found = FindState(child.stateMachine, stateName);
            if (found != null) return found;
        }

        return null;
    }
}
