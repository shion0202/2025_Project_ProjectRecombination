#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static FIMSpace.FProceduralAnimation.LegsAnimator;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_FadeGluingOnAnimatorParam", menuName = "FImpossible Creations/Legs Animator/LAM_FadeGluingOnAnimatorParam", order = 2)]
    public class LAM_FadeGluingOnAnimatorParam : LegsAnimatorControlModuleBase
    {
        int _hash = -1;
        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            string boolParamName = helper.RequestVariable("Disable Gluing On Bool Param", "Animator Param Name").GetString();
            _hash = Animator.StringToHash(boolParamName);
        }

        public override void OnUpdate(LegsAnimatorCustomModuleHelper helper)
        {
            if (helper.Parent.Mecanim.GetBool(_hash))
            {
                helper.Parent.MainGlueBlend = Mathf.MoveTowards(helper.Parent.MainGlueBlend, 0.001f, Time.deltaTime * 7f);
            }
            else
            {
                helper.Parent.MainGlueBlend = Mathf.MoveTowards(helper.Parent.MainGlueBlend, 1f, Time.deltaTime * 7f);
            }
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Fade off Legs Animator gluing when animator bool parameter is true.", UnityEditor.MessageType.Info);
            GUILayout.Space(3);

            var  boolParamName = helper.RequestVariable("Disable Gluing On Bool Param", "Animator Param Name");
            boolParamName.Editor_DisplayVariableGUI();

            if (helper.Parent.Mecanim == null)
            {
                EditorGUILayout.HelpBox("This module requires animator to be assigned under Legs Animator 'Extra -> Control' bookmark!", UnityEditor.MessageType.Warning);
            }

            if ( Initialized)
            {
                if (!legsAnimator.Mecanim) return;
                EditorGUILayout.LabelField("Hash " + _hash + " value for animator is = " + legsAnimator.Mecanim.GetBool(_hash));
            }
        }

#endif

        #endregion

    }
}