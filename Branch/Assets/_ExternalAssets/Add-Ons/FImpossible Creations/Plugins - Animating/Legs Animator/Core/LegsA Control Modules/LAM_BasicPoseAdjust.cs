using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static FIMSpace.FProceduralAnimation.LegsAnimator;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_BasicPoseAdjust", menuName = "FImpossible Creations/Legs Animator/LAM_BasicPoseAdjust", order = 2)]
    public class LAM_BasicPoseAdjust : LegsAnimatorControlModuleBase
    {

        LegsAnimator.Variable _AdjustPowerX;
        LegsAnimator.Variable _AdjustPowerZ;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _AdjustPowerX = helper.RequestVariable("Adjust X Positioning", 1f);
            _AdjustPowerZ = helper.RequestVariable("Adjust Z Positioning", 1f);


            #region Selective legs use implementation

            // Prepare legs to work on
            List<LegsAnimator.Leg> preLegs = new List<LegsAnimator.Leg>();
            if (helper.customStringList == null || helper.customStringList.Count == 0)
            {
                for (int i = 0; i < LA.Legs.Count; i++) preLegs.Add(LA.Legs[i]); // Add All
            }
            else
                for (int i = 0; i < helper.customStringList.Count; i++)
                {
                    if (helper.customStringList[i] == "1") preLegs.Add(LA.Legs[i]); // Add Selective
                }

            if (preLegs.Count == 0)
            {
                helper.Enabled = false;
                Debug.Log("[Legs Animator] Fade On Animation Module: No legs definition!");
                return;
            }

            legs = preLegs.ToArray();

            #endregion
        }

        [NonSerialized] LegsAnimator.Leg[] legs; // I have no idea but unity keeps creating serialization cycle on this variable, if not using [NonSerialized] even when it's private variable


        public override void OnAfterAnimatorCaptureUpdate(LegsAnimatorCustomModuleHelper helper)
        {
            if (legs == null) return;

            float blend = EffectBlend;
            for (int l = 0; l < legs.Length; l++)
            {
                var leg = legs[l];

                Vector3 mainLoc = LA.ToRootLocalSpace(leg._AnimatorEndBonePos);
                Vector3 local = mainLoc;
                local.x *= _AdjustPowerX.GetFloat();
                local.z *= _AdjustPowerZ.GetFloat();

                if (blend < 1f) local = Vector3.LerpUnclamped(mainLoc, local, blend);

                leg.OverrideAnimatorAnklePosition(LA.RootToWorldSpace(local));
            }
        }

        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            EditorGUILayout.HelpBox("Multiplying IK positions in local space, so you can adjust legs spacing.", UnityEditor.MessageType.Info);
            GUILayout.Space(3);

            var adjPowerV = helper.RequestVariable("Adjust X Positioning", 1f);
            adjPowerV.SetMinMaxSlider(0f, 2f);
            adjPowerV.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            var rotateVar = helper.RequestVariable("Adjust Z Positioning", 1f);
            rotateVar.SetMinMaxSlider(0f, 2f);
            rotateVar.Editor_DisplayVariableGUI();
            GUILayout.Space(3);



            #region Draw legs list

            if (helper.customStringList == null) helper.customStringList = new List<string>();
            var list = helper.customStringList;
            int targetCount = legsAnimator.Legs.Count;

            if (list.Count < targetCount)
                while (list.Count < targetCount) list.Add("1");
            else
                while (list.Count > targetCount) list.RemoveAt(list.Count - 1);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Select legs to apply module effect on:", EditorStyles.helpBox);
            GUILayout.Space(3);

            GUI.enabled = !legsAnimator.LegsInitialized;

            for (int i = 0; i < list.Count; i++)
            {
                var boneStart = legsAnimator.Legs[i].BoneStart;

                if (boneStart == null)
                {
                    EditorGUILayout.LabelField("[" + (i + 1) + "] LEG LACKING BONE REFERENCES");
                    continue;
                }

                EditorGUILayout.BeginHorizontal();

                var str = list[i];
                bool target;
                if (str.Length == 0 || str[0] != '1') target = false; else target = true;
                target = EditorGUILayout.Toggle("[" + (i + 1) + "]: " + boneStart.name, target);

                if (target == false)
                    list[i] = "0";
                else
                    list[i] = "1";

                EditorGUILayout.ObjectField(boneStart, typeof(Transform), true, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = true;

            #endregion

        }

#endif

        #endregion

    }
}