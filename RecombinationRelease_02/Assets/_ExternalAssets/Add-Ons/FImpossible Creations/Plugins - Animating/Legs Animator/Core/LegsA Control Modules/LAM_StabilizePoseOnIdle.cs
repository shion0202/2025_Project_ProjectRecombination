#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_StabilizePoseOnIdle", menuName = "FImpossible Creations/Legs Animator/Control Module - Stabilize Pose On Idle", order = 2)]
    public class LAM_StabilizePoseOnIdle : LegsAnimatorControlModuleBase
    {
        Vector3 currentHeightAdjust = Vector3.zero;
        Vector3 sd_currentHeightAdjust = Vector3.zero;

        LegsAnimator.Variable _blendV;
        LegsAnimator.Variable _adjSpeed;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _blendV = helper.RequestVariable("Blend", 1f);
            _adjSpeed = helper.RequestVariable("Adjusting Speed", 1f);
        }

        public override void OnReInitialize(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            currentHeightAdjust = Vector3.zero;
            sd_currentHeightAdjust = Vector3.zero;
        }

        public override void OnLateUpdatePreApply(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (LA.Legs.Count < 2) return;

            bool adjust = false;
            if (LA.IsMoving == false)
            {
                adjust = true;

                if (LA.UseGluing)
                    for (int l = 0; l < LA.Legs.Count; l++)
                        if (LA.Legs[l].G_Attached == false) { adjust = false; break; }
            }

            if (adjust)
            {
                var leg = LA.Legs[0];
                Vector3 middlePoint = leg._PreviousFinalIKPos + leg.AnkleH.Bone.TransformVector(leg.AnkleToFeetEnd * 0.6f);

                for (int l = 1; l < LA.Legs.Count; l++)
                {
                    leg = LA.Legs[l];
                    Vector3 legLocalP = leg._PreviousFinalIKPos + leg.AnkleH.Bone.TransformVector(leg.AnkleToFeetEnd * 0.6f);
                    //Vector3 legLocalP = LA.Legs[l]._PreviousFinalIKPos + LA.Legs[l]._PreviousFinalIKRot * (LA.Legs[l].AnkleForward * LA.Legs[l].FootMiddlePosition);
                    middlePoint = Vector3.LerpUnclamped(middlePoint, legLocalP, 0.5f);
                }

                middlePoint = LA.ToRootLocalSpace(middlePoint); // wanted center of mass
                middlePoint.y = 0f;

                Vector3 currentPos = LA.ToRootLocalSpace(LA._LastAppliedHipsFinalPosition);
                currentPos.y = 0f;

                middlePoint = middlePoint - currentPos;
                middlePoint = LA.RootToWorldSpaceVec(middlePoint);

                currentHeightAdjust = Vector3.SmoothDamp(currentHeightAdjust, middlePoint, ref sd_currentHeightAdjust, 0.05f + _adjSpeed.GetFloat() * 0.3f, 1000000f, LA.DeltaTime);
            }
            else
            {
                currentHeightAdjust = Vector3.SmoothDamp(currentHeightAdjust, Vector3.zero, ref sd_currentHeightAdjust, 0.05f + _adjSpeed.GetFloat() * 0.3f, 1000000f, LA.DeltaTime);
            }

            LA.Hips.position += currentHeightAdjust * EffectBlend * _blendV.GetFloat() * LA._MainBlend;

            Vector3 refPos = LA.BaseTransform.position + currentHeightAdjust + Vector3.up * 2.4f;
            refPos.y = LA._LastAppliedHipsFinalPosition.y + 2f;
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Extra calculations to keep character hips in the center. It's similar to the stability settings but providing extra correction.", UnityEditor.MessageType.Info);
            GUILayout.Space(3);

            var blendVar = helper.RequestVariable("Blend", 1f);
            blendVar.SetMinMaxSlider(0f, 1f);
            blendVar.Editor_DisplayVariableGUI();
            GUILayout.Space(3);

            var rotateVar = helper.RequestVariable("Adjusting Speed", 1f);
            rotateVar.SetMinMaxSlider(0f, 1f);
            rotateVar.Editor_DisplayVariableGUI();
            GUILayout.Space(5);

            if (!Application.isPlaying) return;
            UnityEditor.EditorGUILayout.LabelField(" Animator Current Adjust: " + currentHeightAdjust + "  A:  " + legsAnimator.HipsSetup._Hips_LastHipsOffset);
        }

#endif
        #endregion


    }
}