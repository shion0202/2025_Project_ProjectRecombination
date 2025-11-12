#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static FIMSpace.FProceduralAnimation.LegsAnimator;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_ParamChanger_UnglueOnAngle", menuName = "FImpossible Creations/Legs Animator/LAM_ParamChanger_UnglueOnAngle", order = 2)]
    public class LAM_ParamChanger_UnglueOnAngle : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable _UnglueAngleOnMoving;
        float initialUnglueOn = 30f;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _UnglueAngleOnMoving = helper.RequestVariable("Unglue Angle On Moving", 70f);
            initialUnglueOn = LA.UnglueOn;
        }

        public override void OnUpdate(LegsAnimatorCustomModuleHelper helper)
        {
            LA.UnglueOn = Mathf.Lerp(initialUnglueOn, _UnglueAngleOnMoving.GetFloat(), LA.IsMovingBlend);
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Just changing 'Unglue On' parameter value when character is in movement state.", UnityEditor.MessageType.Info);
            GUILayout.Space(3);

            _UnglueAngleOnMoving = helper.RequestVariable("Unglue Angle On Moving", 70f);
            _UnglueAngleOnMoving.SetMinMaxSlider(0f, 90f);
            _UnglueAngleOnMoving.Editor_DisplayVariableGUI();
        }

#endif

        #endregion

    }
}