#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    /// <summary>
    /// Examples class for customized controll over the Legs Animator IK Redirecting features
    /// </summary>
    //[CreateAssetMenu(fileName = "LAM_SlopePusher", menuName = "FImpossible Creations/Legs Animator/Control Module - Slope Pusher", order = 1)]
    public class LAM_SlopePusher : LegsAnimatorControlModuleBase
    {

        LegsAnimator.Variable _powerV;
        LegsAnimator.Variable _thresV;
        LegsAnimator.Variable _rapidV;
        LegsAnimator.Variable _pushBackV;
        Vector3 offsetV = Vector3.zero;
        Vector3 _Sd_offsetV = Vector3.zero;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _powerV = helper.RequestVariable("Offset Power", 0.6f);
            _thresV = helper.RequestVariable("Push Threshold", 0.5f);
            _rapidV = helper.RequestVariable("Offset Rapidity", 0.5f);
            _pushBackV = helper.RequestVariable("Push Back", 0f);
        }

        public override void Leg_LateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg)
        {
            Vector3 targetOffset = Vector3.zero;

            // Push forward when going down big slope causing leg to be pulled too high
            // or push back when going up big slope
            if (leg.A_WasAligning)
            {
                float legSafeHeight = LA.ScaleReferenceNoScale * 0.25f * _thresV.GetFloat();
                if (leg.groundHitRootSpacePos.y > legSafeHeight)
                {
                    //float ang = Vector3.Angle(LA.Up, leg.LastGroundHit.normal);
                    float anglePower = Mathf.InverseLerp(5f, 50f, leg.raycastSlopeAngle);
                    float offFactor = (leg.groundHitRootSpacePos.y - legSafeHeight) / (legSafeHeight * 1.5f);
                    Vector2 flatHit = new Vector2(leg.groundHitRootSpacePos.x, leg.groundHitRootSpacePos.z).normalized;
                    targetOffset -= (new Vector3(flatHit.x, -0.033f, flatHit.y) * (offFactor * legSafeHeight)) * _powerV.GetFloat() * 2f * anglePower;
                }

                if (_pushBackV.GetFloat() > 0f)
                    if (LA.DesiredMovementDirection != Vector3.zero)
                    {
                        float pwr = _powerV.GetFloat(); if (pwr <= 0f) pwr = 1f;
                        float dot = -Vector3.Dot(targetOffset.normalized, LA.DesiredMovementDirection.normalized);
                        if (dot < 0f) targetOffset *= Mathf.Max(-1f, dot * 2f) * (_pushBackV.GetFloat() / pwr );
                    }
            }

            offsetV = Vector3.SmoothDamp(offsetV, targetOffset, ref _Sd_offsetV, 0.2f - _rapidV.GetFloat() * 0.199f, 1000000f, LA.DeltaTime);
            LA._Hips_Modules_ExtraWOffset += LA.RootToWorldSpaceVec(offsetV);
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Pushing body in opposite direction to big slope raycast, can help slightly humanoids movement on really steep slopes.", UnityEditor.MessageType.Info);
            GUILayout.Space(5);
            var rotateVar = helper.RequestVariable("Offset Power", 0.6f);
            rotateVar.SetMinMaxSlider(0f, 1f);
            rotateVar.Editor_DisplayVariableGUI();

            GUILayout.Space(5);
            var rapidVar = helper.RequestVariable("Offset Rapidity", 0.35f);
            rapidVar.SetMinMaxSlider(0f, 1f);
            rapidVar.Editor_DisplayVariableGUI();

            var pushShresh = helper.RequestVariable("Push Threshold", 0.25f);
            if (!pushShresh.TooltipAssigned) pushShresh.AssignTooltip( "Set lower to trigger pushing more often");
            pushShresh.SetMinMaxSlider(0.05f, 1f);
            pushShresh.Editor_DisplayVariableGUI();

            var pushBack = helper.RequestVariable("Push Back", 0f);
            if (!pushBack.TooltipAssigned) pushBack.AssignTooltip("Optional parameter to push body back towards slope if it's opposite direction to the 'Desired Movement Direction' (Desired movement direction assign required for this parameter)");
            pushBack.SetMinMaxSlider(0f, 1f);
            pushBack.Editor_DisplayVariableGUI();
        }

#endif
        #endregion

    }
}