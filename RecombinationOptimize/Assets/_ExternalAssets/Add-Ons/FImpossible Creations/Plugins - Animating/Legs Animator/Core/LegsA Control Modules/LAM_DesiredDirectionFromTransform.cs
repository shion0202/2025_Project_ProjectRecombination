#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using UnityEngine;
using static FIMSpace.FProceduralAnimation.LegsAnimator;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_DesiredDirectionFromTransform", menuName = "FImpossible Creations/Legs Animator/LAM_DesiredDirectionFromTransform", order = 2)]
    public class LAM_DesiredDirectionFromTransform : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable _Reaction;
        LegsAnimator.Variable _Thres;
        LegsAnimator.Variable _IsMov;

        Vector3 calculatedVelo = Vector3.zero;
        Vector3 _sd_average = Vector3.zero;
        Vector3 previousPosition = Vector3.zero;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _Reaction = helper.RequestVariable("Reaction Speed", .8f);
            _IsMov = helper.RequestVariable("Control 'IsMoving'", false);
            _Thres = helper.RequestVariable("Not Moving Threshold", .2f);
            previousPosition = LA.BaseTransform.position;
        }

        public override void OnUpdate(LegsAnimatorCustomModuleHelper helper)
        {
            Vector3 diff = LA.BaseTransform.position - previousPosition;
            previousPosition = LA.BaseTransform.position;

            Vector3 newVelo = diff / LA.DeltaTime;
            newVelo = LA.ToRootLocalSpaceVec(newVelo);
            newVelo.y = 0f; // Neutralize jump velo in LA local Up space
            newVelo = LA.RootToWorldSpaceVec(newVelo);

            float magnitude = calculatedVelo.magnitude;
            newVelo = Vector3.Slerp(newVelo, newVelo.normalized, Mathf.InverseLerp(0f, magnitude, LA.ScaleReference));

            calculatedVelo = Vector3.SmoothDamp(calculatedVelo, newVelo, ref _sd_average, (0.00005f + (1f - _Reaction.GetFloat()) * 0.15f), 100000f, LA.DeltaTime);

            if (_IsMov.GetBool())
            {
                LA.User_SetIsMoving(magnitude > LA.ScaleReference * _Thres.GetFloat());
            }

            LA.User_SetDesiredMovementDirection(calculatedVelo);
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Reading world translation of the character and providing as 'Desired Movement Direction'.\nIt's not precise as rigidbody desired direction read.", UnityEditor.MessageType.Info);
            GUILayout.Space(3);

            var adjPowerV = helper.RequestVariable("Reaction Speed", .8f);
            adjPowerV.SetMinMaxSlider(0f, 1f);
            adjPowerV.Editor_DisplayVariableGUI();

            var adjIsMoving = helper.RequestVariable("Control 'IsMoving'", false);
            if (!adjIsMoving.TooltipAssigned) adjIsMoving.AssignTooltip("Change IsMoving flag basing on computed transform velocity. ! Can be delayed, it's more precise to set it through Animator parameter, rigidbody or through coding !");
            adjIsMoving.Editor_DisplayVariableGUI();

            if (adjIsMoving.GetBool())
            {
                var adjThres = helper.RequestVariable("Not Moving Threshold", .2f);
                adjThres.SetMinMaxSlider(0f, 0.5f);
                if (!adjThres.TooltipAssigned) adjThres.AssignTooltip("Threshold on which IsMoving should be set to false (for quicker no-moving response)");
                adjThres.Editor_DisplayVariableGUI();
            }

            GUILayout.Space(3);
            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);

            if (legsAnimator.LegsInitialized)
            {
                EditorGUILayout.LabelField("Calculated Direction Velocity: " + calculatedVelo);

                if (adjIsMoving.GetBool())
                    EditorGUILayout.LabelField("Is Moving: " + LA.IsMoving);

            }

            EditorGUILayout.EndVertical();

        }

#endif

        #endregion

    }
}