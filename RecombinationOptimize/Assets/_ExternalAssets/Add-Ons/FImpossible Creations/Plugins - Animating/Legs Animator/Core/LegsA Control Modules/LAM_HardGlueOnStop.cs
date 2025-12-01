#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_HardGlueOnStop", menuName = "FImpossible Creations/Legs Animator/Control Module - Hard Glue On Stop", order = 1)]
    public class LAM_HardGlueOnStop : LegsAnimatorControlModuleBase
    {
        public float FrontMargin = 0.3f;
        public float ForceForSeconds = 0.6f;

        LegsAnimator.Variable _beforeV;
        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _beforeV = helper.RequestVariable("Hard Glue Before Move", 0.0f);
        }

        public override void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (!LA.IsMoving && LA.IsGrounded && LA.StoppedTime < ForceForSeconds)
            {
                for (int i = 0; i < LA.Legs.Count; i++)
                {
                    LegsAnimator.Leg leg = LA.Legs[i];
                    // If leg is not behind character (swing back run) just front foot gluing
                    if (leg.AnkleH.LastKeyframeRootPos.z > -LA.ScaleReferenceNoScale * FrontMargin) leg.G_CustomForceAttach = true;
                }
            }

            if (_beforeV.GetFloat() > 0f)
            {
                if (LA.IsMoving && LA.IsGrounded && LA.MovingTime < _beforeV.GetFloat())
                    for (int i = 0; i < LA.Legs.Count; i++)
                    {
                        LegsAnimator.Leg leg = LA.Legs[i];
                        // If leg is not stretching too much, then still hard glue it
                        if (leg.IKProcessor.GetStretchValue(leg.IKProcessor.IKTargetPosition) < 1.01f) 
                            leg.G_CustomForceAttach = true;
                    }
            }
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Trying to quickly glue foot on ground when LegsAnimator IsMoving changes from true to false.", UnityEditor.MessageType.Info);

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("(Optional)", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(54));
            var beforeV = helper.RequestVariable("Hard Glue Before Move", 0.0f);
            beforeV.SetMinMaxSlider(0f, 0.3f);
            if (!beforeV.TooltipAssigned) beforeV.AssignTooltip("(Optional Parameter) Keep feet glued on ground for a shorty moment before character's velocity builds up");
            beforeV.Editor_DisplayVariableGUI();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

#endif
        #endregion

    }
}