#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_DynamicStability", menuName = "FImpossible Creations/Legs Animator/LAM_DynamicStability", order = 1)]
    public class LAM_HeightStabilizer : LegsAnimatorControlModuleBase
    {
        public override bool AskForSpineBone { get { return true; } }

        LegsAnimator.Variable _powerV;
        LegsAnimator.Variable _reactV;
        LegsAnimator.Variable _thresV;
        LegsAnimator.Variable _extrV;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            base.OnInit(helper);
            _powerV = helper.RequestVariable("Blend", 1f);
            _reactV = helper.RequestVariable("Reaction Speed", 0.7f);
            _thresV = helper.RequestVariable("Blend Y", 0.5f);
            _extrV = helper.RequestVariable("Extra Push Down Blend", 0.1f);
        }

        public override void OnReInitialize(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            adjustement = Vector3.zero;
            sd_adjustement = Vector3.zero;
        }


        Vector3 adjustement = Vector3.zero;
        Vector3 sd_adjustement = Vector3.zero;
        public override void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            Vector3 hipsPos = LA._LastAppliedHipsFinalPosition;
            Vector3 hipsLocal = LA.ToRootLocalSpace(hipsPos);
            // Obj local is zero

            Vector3 keyframeLocal = LA.HipsSetup.LastKeyframeLocalPosition;
            hipsLocal.y = 0f;
            keyframeLocal.y = 0f;

            // Difference between applied hips position and keyframe position (zero if no changes)
            Vector3 localDiff = keyframeLocal - hipsLocal;

            float diffDistance = localDiff.magnitude;

            localDiff.y -= diffDistance * _thresV.GetFloat();

            localDiff *= _powerV.GetFloat();

            localDiff.y -= Mathf.InverseLerp(LA.ScaleReferenceNoScale * 0.001f, LA.ScaleReferenceNoScale * (0.15f + _extrV.GetFloat() * 0.1f), diffDistance) * _extrV.GetFloat() * 0.25f;

            localDiff *= EffectBlend;

            adjustement = Vector3.SmoothDamp(adjustement, localDiff, ref sd_adjustement, Mathf.Lerp(0.4f, 0.005f, _reactV.GetFloat()), 1000000f, LA.DeltaTime);

            LA._Hips_Modules_ExtraWOffset += LA.RootToWorldSpaceVec(adjustement);
        }



        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Extra stability calculation, dynamically adapting to movement of the character and manipulating the hips position to keep right height.", UnityEditor.MessageType.Info);
            GUILayout.Space(5);

            _powerV = helper.RequestVariable("Blend", 1f);
            _reactV = helper.RequestVariable("Reaction Speed", 0.7f);
            _thresV = helper.RequestVariable("Reaction Threshold", 0.05f);

            var rotateVar = helper.RequestVariable("Blend", 1f);
            rotateVar.SetMinMaxSlider(0f, 1f);
            rotateVar.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            var sideVar = helper.RequestVariable("Reaction Speed", 0.7f);
            sideVar.SetMinMaxSlider(0f, 1f);
            sideVar.Editor_DisplayVariableGUI();

            var forwVar = helper.RequestVariable("Blend Y", 0.5f);
            forwVar.SetMinMaxSlider(0f, 1f);
            forwVar.Editor_DisplayVariableGUI();

            var extrVar = helper.RequestVariable("Extra Push Down Blend", 0.1f);
            extrVar.SetMinMaxSlider(0f, 1f);
            extrVar.Editor_DisplayVariableGUI();

        }

#endif
        #endregion

    }
}