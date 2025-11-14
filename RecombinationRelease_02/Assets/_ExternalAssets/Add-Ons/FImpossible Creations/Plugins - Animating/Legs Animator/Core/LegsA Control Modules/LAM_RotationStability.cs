#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_SpineRotateOnStability", menuName = "FImpossible Creations/Legs Animator/Control Module - Spine Rotate On Stability", order = 1)]
    public class LAM_RotationStability : LegsAnimatorControlModuleBase
    {
        public override bool AskForSpineBone { get { return true; } }

        LegsAnimator.Variable _powerV;
        LegsAnimator.Variable _compenV;
        LegsAnimator.Variable _sideV;
        LegsAnimator.Variable _forwV;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            base.OnInit(helper);
            _powerV = helper.RequestVariable("Rotation Power", 0.4f);
            _compenV = helper.RequestVariable("Compensate Spine", 0.5f);
            _sideV = helper.RequestVariable("Side Multiplier", -1f);
            _forwV = helper.RequestVariable("Forward Multiplier", 1f);
        }

        //public override void OnLateUpdatePreApply(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        public override void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            float blend = _powerV.GetFloat() * EffectBlend;


            if (blend != 0f)
            {
                Vector3 stabilizeVector = Vector3.zero;

                stabilizeVector += LA._Get_Hips_StabilityLocalOffset;
                stabilizeVector += LA._Get_Hips_StabilityLocalAdjustement;

                stabilizeVector.x /= LA.ScaleReferenceNoScale;

                stabilizeVector.z += stabilizeVector.y * 0.4f;
                stabilizeVector.z /= LA.ScaleReferenceNoScale;

                stabilizeVector.x *= 60f;
                stabilizeVector.z *= 60f;

                Quaternion hipsRotationOffset = Quaternion.identity;

                float intensFactor = (1f / Mathf.Max(0.15f, LA.StabilizeCenterOfMass) ) * 0.5f; // Make Stabilize param indepentent

                hipsRotationOffset *= Quaternion.AngleAxis(stabilizeVector.z * blend * _forwV.GetFloat() * intensFactor, LA.BaseTransform.right);
                hipsRotationOffset *= Quaternion.AngleAxis(stabilizeVector.x * blend * _sideV.GetFloat() * intensFactor, LA.BaseTransform.forward);
                LA._LastHipsRotationOffsetOutsideInfo *= hipsRotationOffset;

                for (int i = 0; i < LA.HipsHubs.Count; i++)
                {
                    LA.HipsHubs[i]._LastHipsRotationOffsetOutsideInfo *= hipsRotationOffset;
                }

                if (LA.SpineBone != null)
                {
                    Quaternion lastChildRotation = Quaternion.identity;
                    if (LA.SpineBone != null) lastChildRotation = LA.SpineBone.rotation;

                    LA.Hips.rotation = hipsRotationOffset * LA.Hips.rotation;

                    LA.SpineBone.rotation = Quaternion.Lerp(LA.SpineBone.rotation, lastChildRotation, _compenV.GetFloat());
                }
                else
                {
                    LA.Hips.rotation = hipsRotationOffset * LA.Hips.rotation;
                }

            }
        }



        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Using Stability calculated data to rotate hips and spine bone, in order to add extra motion to the animation.", UnityEditor.MessageType.Info);
            GUILayout.Space(5);

            var rotateVar = helper.RequestVariable("Rotation Power", 0.4f);
            rotateVar.SetMinMaxSlider(-2f, 2f);
            rotateVar.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            var sideVar = helper.RequestVariable("Side Multiplier", -1f);
            sideVar.SetMinMaxSlider(-1f, 1f);
            sideVar.Editor_DisplayVariableGUI();

            var forwVar = helper.RequestVariable("Forward Multiplier", 1f);
            forwVar.SetMinMaxSlider(0f, 1f);
            forwVar.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            if (legsAnimator.SpineBone == null)
            {
                if (GUILayout.Button("Go To Extra/Control to assign optional Spine Bone"))
                {
                    legsAnimator._EditorCategory = LegsAnimator.EEditorCategory.Extra;
                    legsAnimator._EditorExtraCategory = LegsAnimator.EEditorExtraCategory.Control;
                }
            }
            else
            {
                var compensV = helper.RequestVariable("Compensate Spine", 0.5f);
                compensV.SetMinMaxSlider(0f, 1f);
                compensV.Editor_DisplayVariableGUI();
            }
        }

#endif
        #endregion

    }
}