using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public class LAM_LegsOnlyOnIdle : LegsAnimatorControlModuleBase
    {
        public override void OnUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (LA.IsMoving)
            {
                LA.LegsAnimatorBlend = Mathf.MoveTowards(LA.LegsAnimatorBlend, 0.001f, LA.DeltaTime * 5f);
            }
            else
            {
                LA.LegsAnimatorBlend = Mathf.MoveTowards(LA.LegsAnimatorBlend, 1f, LA.DeltaTime * 8f);
            }
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            UnityEditor.EditorGUILayout.HelpBox("Fading off whole Legs Animator motion when IsMoving is set to true.", UnityEditor.MessageType.Info);
            UnityEditor.EditorGUILayout.LabelField("Now legsAnimator.Is Moving = " + legsAnimator.IsMoving);
        }

#endif
        #endregion

    }
}