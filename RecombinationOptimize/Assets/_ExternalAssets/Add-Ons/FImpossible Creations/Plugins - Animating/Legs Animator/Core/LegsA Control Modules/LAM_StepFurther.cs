#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_StepFurther", menuName = "FImpossible Creations/Legs Animator/Control Module - Rigibody Step Further", order = 1)]
    public class LAM_StepFurther : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable _hipsV;
        LegsAnimator.Variable _powerV;
        LegsAnimator.Variable _mulV;

        public bool UsingCustomVelo { get; set; } = false;
        Vector3 customVelo = Vector3.zero;

        /// <summary> If you want to use your own calculated velocity value instead of automatic rigidbody velocity </summary>
        public void ProvideVelocity(Vector3 velocity)
        {
            UsingCustomVelo = true;
            customVelo = velo;
        }

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            base.OnInit(helper);
            UsingCustomVelo = false;
            _powerV = helper.RequestVariable("Predict Forward Offset", 0.1f);
            _hipsV = helper.RequestVariable("Predict Forward Hips Offset", 0.0f);
            _mulV = helper.RequestVariable("Extra Multiplier", 1f);
            lastPos = LA.transform.position;
        }

        Vector3 velo = Vector3.zero;
        Vector3 finalVelo = Vector3.zero;
        Vector3 _sd_velo = Vector3.zero;
        Vector3 lastPos;
        public override void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            Vector3 yNeutralVelo;

            if (UsingCustomVelo == false)
            {
                if (LA.Rigidbody && LA.Rigidbody.isKinematic == false)
                {
                    yNeutralVelo = LA.ToRootLocalSpaceVec(LA.Rigidbody.velocity);
                }
                else if (LA.usingCustomDesiredMovementDirection)
                {
                    yNeutralVelo = LA.ToRootLocalSpaceVec(LA.DesiredMovementDirection * LA.IsMovingBlend);
                }
                else
                {
                    if (LA.DeltaTime > 0f)
                        yNeutralVelo = LA.ToRootLocalSpaceVec((LegsAnim.BaseTransform.position - lastPos) / LA.DeltaTime);
                    else
                        yNeutralVelo = Vector3.zero;
                }
            }
            else
            {
                yNeutralVelo = LA.ToRootLocalSpaceVec(customVelo);
            }

            lastPos = LegsAnim.BaseTransform.position;

            yNeutralVelo.y = 0f;
            yNeutralVelo = LA.RootToWorldSpaceVec(yNeutralVelo);

            velo = Vector3.SmoothDamp(velo, yNeutralVelo, ref _sd_velo, 0.1f, 1000000f, LA.DeltaTime);
            finalVelo = velo * (_powerV.GetFloat() * _mulV.GetFloat() * EffectBlend);
        }

        public override void Leg_LatePreRaycastingUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg)
        {
            if (_powerV.GetFloat() > 0f)
            {
                leg.OverrideFinalAndSourceIKPos(leg.GetFinalIKPos() + finalVelo);
                leg.OverrideControlPositionsWithCurrentIKState();
            }

            if (leg.PlaymodeIndex == 0) // Call only once regardless legs count
                if (_hipsV.GetFloat() > 0f)
                {
                    LA._Hips_Modules_ExtraWOffset += velo * (_hipsV.GetFloat() * EffectBlend);
                }
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (legsAnimator.Rigidbody == null)
            {
                EditorGUILayout.HelpBox("This module works best with rigidbody or when using 'Desired Movement Direction'", UnityEditor.MessageType.Info);
                GUILayout.Space(5);
            }

            EditorGUILayout.HelpBox("Pushing leg step raycast position further or pushing hips with character velocity.", UnityEditor.MessageType.None);
            GUILayout.Space(5);
            var rotateVar = helper.RequestVariable("Predict Forward IK Offset", 0.1f);
            rotateVar.SetMinMaxSlider(0f, 0.3f);
            rotateVar.Editor_DisplayVariableGUI();

            var hipsVar = helper.RequestVariable("Predict Forward Hips Offset", 0.0f);
            hipsVar.SetMinMaxSlider(0f, 0.3f);
            hipsVar.Editor_DisplayVariableGUI();

            GUILayout.Space(5);
            var extraMultiplier = helper.RequestVariable("Extra Multiplier", 1f);
            extraMultiplier.Editor_DisplayVariableGUI();
        }

#endif
        #endregion

    }
}