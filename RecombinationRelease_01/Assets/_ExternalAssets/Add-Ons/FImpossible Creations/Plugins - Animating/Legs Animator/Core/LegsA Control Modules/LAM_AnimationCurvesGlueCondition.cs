using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static FIMSpace.FProceduralAnimation.LegsAnimator;

namespace FIMSpace.FProceduralAnimation
{
    /// <summary>
    /// Examples class for customized controll over the Legs Animator IK Redirecting features
    /// </summary>
    //[CreateAssetMenu(fileName = "LAM_AnimCurveGlueCondition", menuName = "FImpossible Creations/Legs Animator/Control Module - Animation Curves Glue Condition", order = 5)]
    public class LAM_AnimationCurvesGlueCondition : LegsAnimatorControlModuleBase
    {
        LegsAnimatorCustomModuleHelper _useHelper = null;
        Variable FloorValueBelowVar { get { return _useHelper.RequestVariable("Floor Value Below", 0.01f); } }
        Variable _play_FloorValueBelow = null;
        Variable IgnoreMidConditionsVar { get { return _useHelper.RequestVariable("Ignore Mid Conditions", false); } }
        Variable _play_IgnoreMidConditions = null;
        Variable AllowHeightGlueOnLevelVar { get { return _useHelper.RequestVariable("Allow Height Glue On Level", -1f); } }
        Variable _play_AllowHeightGlueOnLevels = null;

        private List<int> animatorHashes = null;
        private bool initialized = false;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (LA.Mecanim == null) return;
            if (helper.customStringList == null) return;

            _useHelper = helper;

            _play_FloorValueBelow = FloorValueBelowVar;
            _play_IgnoreMidConditions = IgnoreMidConditionsVar;
            _play_AllowHeightGlueOnLevels = AllowHeightGlueOnLevelVar;

            animatorHashes = new List<int>();

            for (int l = 0; l < LA.Legs.Count; l++)
            {
                if (l >= helper.customStringList.Count) break;
                animatorHashes.Add(Animator.StringToHash(helper.customStringList[l]));
            }

            initialized = true;
        }


        public override void Leg_LateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg)
        {
            if (initialized == false) return;
            if (leg.G_CustomForceAttach) return; // If other module forces attach, skip calculations

            float value = LA.Mecanim.GetFloat(animatorHashes[leg.PlaymodeIndex]);

            if (value <= _play_AllowHeightGlueOnLevels.GetFloat()) // If allowing to glue on foot overlaps
            {
                if (leg.A_PreWasAligning)
                {
                    value = _play_FloorValueBelow.GetFloat() - 0.01f;
                }
            }

            if (value <= _play_FloorValueBelow.GetFloat()) // Value below ground level - GROUNDED
            {
                leg.G_CustomForceAttach = LA.GroundedTime > 0.2f;
                //leg.G_CustomForceNOTAttach = false;

                if (_play_IgnoreMidConditions.GetBool())
                {
                    leg.G_CustomForceNOTDetach = true;
                    //leg.G_CustomForceDetach = false;
                }
            }
            else // Value above grounded level - Foot UNGROUNDED
            {
                //leg.G_CustomForceAttach = false;
                leg.G_CustomForceNOTAttach = true;

                if (_play_IgnoreMidConditions.GetBool())
                {
                    //leg.G_CustomForceNOTDetach = false;
                    leg.G_CustomForceDetach = true;
                }
            }
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimatorCustomModuleHelper helper)
        {
            _useHelper = helper;

            if (legsAnimator.Mecanim == null)
            {
                UnityEditor.EditorGUILayout.HelpBox("No Animator found to handle animation curves!", UnityEditor.MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox("Using animation curve parameters to controll gluing timing. It requires extra curves inside animation clips but can provide better controll for gluing feature.", UnityEditor.MessageType.Info);
            GUILayout.Space(5);

            var floorValV = FloorValueBelowVar;
            if (!floorValV.TooltipAssigned) floorValV.AssignTooltip( "Gluing condition basing on animation curves");
            floorValV.Editor_DisplayVariableGUI();

            var ignMidV = IgnoreMidConditionsVar;
            if (!ignMidV.TooltipAssigned) ignMidV.AssignTooltip( "When enabled algorithm will not check detaching conditions on foot rotation angles");
            ignMidV.Editor_DisplayVariableGUI();

            //var allowHV = AllowHeightGlueOnLevelVar;
            //if (allowHV.Tooltip == "") allowHV.Tooltip = "If at some value you want to allow glue if character is walking on steep terrain. If parameter is below choosed value it will be allowed";
            //allowHV.Editor_DisplayVariableGUI();

            GUILayout.Space(5);

            if (helper.customStringList == null) helper.customStringList = new List<string>();
            var list = helper.customStringList;
            int targetCount = legsAnimator.Legs.Count;

            if (list.Count < targetCount)
                while (list.Count < targetCount) list.Add("");
            else
                while (list.Count > targetCount) list.RemoveAt(list.Count - 1);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Mecanim parameters per leg", EditorStyles.helpBox);
            GUILayout.Space(3);

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = EditorGUILayout.TextField(new GUIContent("Leg [" + i + "] Curve Parameter:", "Legs name = " + legsAnimator.Legs[i].BoneStart.name + "\nAnimator parameter to read curve value for triggering gluing with more control"), list[i]);
            }

            GUILayout.Space(5);
            if (initialized == false) return;

            for (int l = 0; l < animatorHashes.Count; l++)
            {
                UnityEditor.EditorGUILayout.BeginHorizontal();
                float val = LA.Mecanim.GetFloat(animatorHashes[l]);
                UnityEditor.EditorGUILayout.LabelField("  [" + l + "] " + val, GUILayout.Width(106));
                UnityEditor.EditorGUILayout.LabelField(LA.Legs[l].Side.ToString(), GUILayout.Width(54));
                UnityEditor.EditorGUILayout.LabelField("  " + (val < _play_FloorValueBelow.GetFloat() ? "FOOT GROUNDED" : "FOOT MOVING"));
                UnityEditor.EditorGUILayout.EndHorizontal();
            }
        }


#endif
        #endregion


    }
}