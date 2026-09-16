#if UNITY_EDITOR
using UnityEditor;
#endif
using FIMSpace.FProceduralAnimation;
using UnityEngine;

/// <summary>
/// Legs Animator 커스텀 모듈: 발을 올려놓을 수 있는 최대 높이를 제한한다.
///
/// Legs Animator는 골반 높이에서 발밑으로 레이를 쏴 지면을 찾기 때문에(Raycast Start Height = Hips),
/// 골반보다 낮은 물체면 캐릭터가 올라설 수 없는 상자나 큰 단차 위라도 발판으로 인식해 다리를 기괴하게 꺾어 올린다.
///
/// 원래 레이와 같은 높이에서 먼저 확인해, 잡힌 지면이 최대 높이를 넘을 때만 개입한다.
/// 평지, 계단, 경사로처럼 최대 높이 이내인 경우에는 Legs Animator 기본 처리를 그대로 쓴다.
/// </summary>
[CreateAssetMenu(fileName = "LAM_StepHeightLimit", menuName = "FImpossible Creations/Legs Animator/Recombination/Step Height Limit")]
public class LAM_StepHeightLimit : LegsAnimatorControlModuleBase
{
    private const string MaxStepHeightName = "Max Step Height";
    private const float DefaultMaxStepHeight = 0.35f;

    private LegsAnimator.Variable _maxStepHeightV;

    public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
    {
        _maxStepHeightV = helper.RequestVariable(MaxStepHeightName, DefaultMaxStepHeight);
    }

    // Legs Animator가 이 다리의 레이캐스트를 하기 직전에 호출된다.
    public override void Leg_LatePreRaycastingUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg)
    {
        float maxStepHeight = _maxStepHeightV.GetFloat();

        // 모든 높이는 캐릭터 루트(발바닥 기준) 로컬 공간 값이다.
        Vector3 footLocal = leg.AnkleH.LastKeyframeRootPos;
        float sourceOriginHeight = LegsAnim.ToRootLocalSpace(leg.lastRaycastingOrigin).y;
        float castBottom = -LegsAnim.ScaleReference * LegsAnim.CastDistance;

        Vector3 castEnd = RootPoint(footLocal, castBottom);

        // 1. 원래 레이와 같은 높이에서 쏴서, 최대 높이를 넘는 지면이 잡히는지 확인한다.
        //    레이 시작점이 최대 높이보다 낮으면 애초에 높은 지면을 잡을 수 없으므로 기본 처리를 쓴다.
        if (sourceOriginHeight > maxStepHeight &&
            Physics.Linecast(RootPoint(footLocal, sourceOriginHeight), castEnd, out RaycastHit sourceHit, LegsAnim.GroundMask, LegsAnim.RaycastHitTrigger) &&
            LegsAnim.ToRootLocalSpace(sourceHit.point).y > maxStepHeight)
        {
            // 2. 최대 높이에서 다시 쏴서 그 아래의 지면에 발을 둔다.
            //    시작점이 상자 안쪽이면 레이는 상자를 무시하고 그 아래 바닥을 잡는다.
            if (Physics.Linecast(RootPoint(footLocal, maxStepHeight), castEnd, out RaycastHit lowHit, LegsAnim.GroundMask, LegsAnim.RaycastHitTrigger))
            {
                leg.User_OverrideRaycastHit(lowHit, true);
                return;
            }

            // 아래에서 지면을 못 찾았다(내부가 비어 있는 메시 콜라이더 지형 등).
            // 기본 처리로 돌리면 다시 높은 지면 위로 다리를 올리므로, 발을 애니메이션 높이 그대로 둔다.
            RaycastHit flatHit = new RaycastHit
            {
                point = RootPoint(footLocal, 0.0f),
                normal = LegsAnim.Up
            };
            leg.User_OverrideRaycastHit(flatHit, true);
            return;
        }

        leg.User_RestoreRaycasting();
    }

    private Vector3 RootPoint(Vector3 footLocal, float height)
    {
        return LegsAnim.RootToWorldSpace(new Vector3(footLocal.x, height, footLocal.z));
    }

    #region Editor Code

#if UNITY_EDITOR
    public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
    {
        EditorGUILayout.HelpBox("원래 레이에 잡힌 지면이 Max Step Height보다 높으면, 그 높이 아래의 지면에 발을 둔다.", MessageType.None);

        LegsAnimator.Variable maxStepHeight = helper.RequestVariable(MaxStepHeightName, DefaultMaxStepHeight);
        maxStepHeight.SetMinMaxSlider(0.05f, 1.0f);
        maxStepHeight.AssignTooltip("발을 올려놓을 수 있는 최대 높이(캐릭터 발바닥 기준, m)");
        maxStepHeight.Editor_DisplayVariableGUI();
    }
#endif

    #endregion
}
