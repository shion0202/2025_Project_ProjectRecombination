#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_InsectBodyElevateHelper", menuName = "FImpossible Creations/Legs Animator/Control Module - Insect Body Elevate Helper", order = 2)]
    public class LAM_InsectBodyElevateHelper : LegsAnimatorControlModuleBase
    {
        float currentHeightAdjust = 0f;
        float sd_currentHeightAdjust = 0f;

        LegsAnimator.Variable _AdjustPowerV;
        LegsAnimator.Variable _AdjustingSpeedV;
        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _AdjustPowerV = helper.RequestVariable("Adjust Power", 1f);
            _AdjustingSpeedV = helper.RequestVariable("Adjusting Speed", 1f);
        }

        public override void OnLateUpdatePreApply(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            float currentAdjustement = LA.HipsSetup._Hips_LastHipsOffset;

            if (currentAdjustement < 0f)
            {
                currentAdjustement = -currentAdjustement;
            }
            else currentAdjustement = 0f;

            if (_AdjustingSpeedV.GetFloat() >= 1f) currentHeightAdjust = currentAdjustement;
            else
            {
                currentHeightAdjust = Mathf.SmoothDamp(currentHeightAdjust, currentAdjustement, ref sd_currentHeightAdjust, Mathf.Lerp(0.16f, 0.005f, _AdjustingSpeedV.GetFloat()));
            }

            LA.Hips.position += LA.Up * currentHeightAdjust * EffectBlend * _AdjustPowerV.GetFloat();
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Pushing hips up when adjusting body down to avoid ground overlaps. Can occur on the insect creatures.", UnityEditor.MessageType.Info);
            GUILayout.Space(3);

            var adjPowerV = helper.RequestVariable("Adjust Power", 1f);
            adjPowerV.SetMinMaxSlider(0f, 2f);
            adjPowerV.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            var rotateVar = helper.RequestVariable("Adjusting Speed", 1f);
            rotateVar.SetMinMaxSlider(0f, 1f);
            rotateVar.Editor_DisplayVariableGUI();
            GUILayout.Space(5);

            if (Application.isPlaying == false) return;
            UnityEditor.EditorGUILayout.LabelField(" Animator Current Height Adjust: " + legsAnimator.HipsSetup._Hips_LastHipsOffset);
            UnityEditor.EditorGUILayout.LabelField(" Current Height: " + currentHeightAdjust);
        }

#endif

        #endregion


    }
}