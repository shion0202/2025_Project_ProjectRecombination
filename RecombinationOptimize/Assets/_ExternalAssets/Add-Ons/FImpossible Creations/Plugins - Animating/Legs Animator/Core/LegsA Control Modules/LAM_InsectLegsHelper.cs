using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.FProceduralAnimation
{
    public class LAM_InsectLegsHelper : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable _onOneSideV;
        LegsAnimator.Variable _onStepCulldownV;
        LegsAnimator.Variable _legSideCulldownV;
        LegsAnimator.Variable _afterFullCulldownV;
        LegsAnimator.Variable _modeV;

        readonly string minSideS = "Minimum Standing Legs On One Side";
        readonly string stepculldS = "On Step Culldown";
        readonly string sideculldV = "Leg Side Culldown";
        readonly string waitV = "On Full Attach Culldown";
        readonly string modeV = "Mode";

        float mainCulldown = 0f;
        float sideLCulldown = 0f;
        float sideRCulldown = 0f;


        private List<LegHelper> legHelpers = null;
        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper hlp)
        {
            _onOneSideV = hlp.RequestVariable(minSideS, 2);
            _onStepCulldownV = hlp.RequestVariable(stepculldS, 0.025f);
            _legSideCulldownV = hlp.RequestVariable(sideculldV, 0.015f);
            _afterFullCulldownV = hlp.RequestVariable(waitV, 0f);
            _modeV = hlp.RequestVariable(modeV, 0);

            // Prepare calculation helper classes
            legHelpers = new List<LegHelper>();

            for (int l = 0; l < LA.Legs.Count; l++)
            {
                LegHelper helper = new LegHelper(LA.Legs[l]);
                legHelpers.Add(helper);
            }

            if (_onOneSideV.GetInt() >= LA.Legs.Count) _onOneSideV.SetValue(LA.Legs.Count / 2);
        }

        #region Leg Helper

        class LegHelper
        {
            public bool WasAttaching = false;
            public bool DetachTrigger = false;

            public LegsAnimator.Leg legRef { get; private set; }

            public float FullyAttachedAt = -1f;

            public LegHelper(LegsAnimator.Leg leg)
            {
                legRef = leg;
                WasAttaching = false;
                DetachTrigger = false;
                FullyAttachedAt = -1f;
            }
        }


        bool AllowDetach(LegHelper leg)
        {
            if (mainCulldown > 0) return false;

            if (leg.legRef.Side == LegsAnimator.ELegSide.Left) { if (sideLCulldown > 0) return false; }
            else if (leg.legRef.Side == LegsAnimator.ELegSide.Right) { if (sideRCulldown > 0) return false; }

            if (_onOneSideV.GetFloat() > 0)
            {
                int standing = 0;

                for (int l = 0; l < legHelpers.Count; l++)
                {
                    var ol = legHelpers[l].legRef;

                    if (ol.Side != leg.legRef.Side) continue;
                    if ((!ol.G_DuringAttaching || ol.G_Attached)
                        /*|| ol.G_AttachPossible*/ /* preventing nervous legs on slopes without ground detected beneath */ )
                        standing += 1;
                }

                if (standing < _onOneSideV.GetFloat()) return false;
            }

            if (Time.time - leg.FullyAttachedAt < _afterFullCulldownV.GetFloat()) return false;

            return true;
        }

        #endregion


        public override void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (legHelpers == null) return;

            // Culldown progress
            mainCulldown -= LA.DeltaTime;
            sideLCulldown -= LA.DeltaTime;
            sideRCulldown -= LA.DeltaTime;
        }

        public override void Leg_LateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper hlp, LegsAnimator.Leg leg)
        {
            if (_modeV.GetInt() == 2)
            {
                CheckConditionsV2(hlp, leg);
                return;
            }

            LegHelper helper = legHelpers[leg.PlaymodeIndex];

            if (leg.G_DuringAttaching) // If leg adjust animation started
            {
                if (helper.WasAttaching == false) // Trigger helper on change
                {
                    // Apply all legs culldown
                    mainCulldown = _onStepCulldownV.GetFloat();

                    // Apply side leg culldowns
                    if (leg.Side == LegsAnimator.ELegSide.Left) sideRCulldown = _legSideCulldownV.GetFloat();
                    else if (leg.Side == LegsAnimator.ELegSide.Right) sideLCulldown = _legSideCulldownV.GetFloat();
                }
            }

            // Detect if leg finished leg adjusting attach animation
            if (leg.G_Attached) { if (helper.FullyAttachedAt == -1) helper.FullyAttachedAt = Time.time; }
            else helper.FullyAttachedAt = -1f;

            // Force leg for not detaching if module's calculated conditions
            // are not allowing for it
            helper.legRef.G_CustomForceNOTDetach = !AllowDetach(helper);

            // Remember attaching state to detect it on change
            helper.WasAttaching = leg.G_DuringAttaching;
        }

        void CheckConditionsV2(LegsAnimator.LegsAnimatorCustomModuleHelper hlp, LegsAnimator.Leg leg)
        {
            LegHelper helper = legHelpers[leg.PlaymodeIndex];

            // Detect if leg finished leg adjust attaching animation
            if (leg.G_Attached) { if (helper.FullyAttachedAt == -1) { helper.FullyAttachedAt = Time.time; helper.DetachTrigger = false; } }
            else helper.FullyAttachedAt = -1f;

            bool preForceNotDetach = leg.G_CustomForceNOTDetach;
            leg.G_CustomForceNOTDetach = false; // For proper check 'Glue_Conditions_Detach' skipping G_CustomForceNOTDetach check
            bool detachCall = false;

            bool detachConditions = false;
            if (leg.G_Attached) if (leg.Glue_CheckDetachement() && leg.Glue_CheckIdleDetachementConfirm()) detachConditions = true;

            if (detachConditions)
            { // If leg adjust animation started / will start

                if (helper.WasAttaching == false && helper.DetachTrigger == false) // Trigger helper on change
                {
                    if (AllowDetach(helper))
                    {
                        detachCall = true;
                        preForceNotDetach = false;
                        helper.DetachTrigger = true;
                        leg.G_CustomForceDetach = true;

                        // Apply all legs culldown
                        mainCulldown = _onStepCulldownV.GetFloat();

                        // Apply side leg culldowns
                        if (leg.Side == LegsAnimator.ELegSide.Left) sideRCulldown = _legSideCulldownV.GetFloat();
                        else if (leg.Side == LegsAnimator.ELegSide.Right) sideLCulldown = _legSideCulldownV.GetFloat();
                    }
                }
            }

            leg.G_CustomForceNOTDetach = preForceNotDetach; // Restore proper check 'Glue_Conditions_Detach', now not skipping G_CustomForceNOTDetach check

            // Force leg for not detaching if module's calculated conditions
            // are not allowing for it
            if (detachCall == false)
            {
                helper.legRef.G_CustomForceNOTDetach = !AllowDetach(helper);
            }

            // Remember attaching state to detect it on change
            helper.WasAttaching = leg.G_DuringAttaching;
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_OnSceneGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (!Application.isPlaying) return;
            if (legHelpers == null) return;

            for (int l = 0; l < legHelpers.Count; l++)
            {
                if (AllowDetach(legHelpers[l])) UnityEditor.Handles.color = Color.red;
                else UnityEditor.Handles.color = Color.green;

                UnityEditor.Handles.SphereHandleCap(0, legHelpers[l].legRef._PreviousFinalIKPos, Quaternion.identity, legsAnimator.ScaleReference * 0.07f, EventType.Repaint);
            }
        }

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {

            EditorGUIUtility.labelWidth = 220;
            EditorGUILayout.HelpBox("Better leg controll for multiple legs creatures.", UnityEditor.MessageType.Info);
            GUILayout.Space(5);

            var legsOnSideV = helper.RequestVariable(minSideS, 2);
            legsOnSideV.Editor_DisplayVariableGUI();
            EditorGUIUtility.labelWidth = 0;

            var OnStepCulldowneV = helper.RequestVariable(stepculldS, 0.025f);
            OnStepCulldowneV.SetMinMaxSlider(0f, 0.15f);
            OnStepCulldowneV.Editor_DisplayVariableGUI();

            var LegSideCulldownV = helper.RequestVariable(sideculldV, 0.015f);
            LegSideCulldownV.SetMinMaxSlider(0f, 0.15f);
            LegSideCulldownV.Editor_DisplayVariableGUI();

            var waitAfterFull = helper.RequestVariable(waitV, 0.0f);
            waitAfterFull.SetMinMaxSlider(0f, 0.3f);
            if (!waitAfterFull.TooltipAssigned) waitAfterFull.AssignTooltip("Culldown measured since last full attach for single leg happened. Can fix sudden leg re-adjusting on being pushed/long creature rotating.");
            waitAfterFull.Editor_DisplayVariableGUI();

            GUILayout.Space(3);

            var modeVar = helper.RequestVariable(modeV, 0);
            int modeI = modeVar.GetInt();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Algorithm Mode:", GUILayout.MaxWidth(120));
            string title = "Basic";
            if (modeI == 2) title = "Advanced";
            if (GUILayout.Button(title, EditorStyles.layerMaskField))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Basic"), modeI != 2, () => { modeVar.SetValue(0); });
                menu.AddItem(new GUIContent("Advanced"), modeI == 2, () => { modeVar.SetValue(2); });
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (legHelpers == null) return;

            for (int l = 0; l < legHelpers.Count; l++)
            {
                UnityEditor.EditorGUILayout.LabelField("  [" + l + "] " + (AllowDetach(legHelpers[l]) ? " ALLOW " : " STOP "));
            }

            //GUILayout.Space(5);
            //for (int l = 0; l < legHelpers.Count; l++)
            //{
            //    bool preForce = legHelpers[l].legRef.G_CustomForceNOTDetach;
            //    legHelpers[l].legRef.G_CustomForceNOTDetach = false;
            //    legHelpers[l].legRef.G_CustomForceNOTDetach = preForce;
            //}

            //GUILayout.Space(5);
            //EditorGUILayout.LabelField("Main Culldown: " + mainCulldown);
            //EditorGUILayout.LabelField("Right Side Culldown: " + sideLCulldown);
            //EditorGUILayout.LabelField("Left Side Culldown: " + sideRCulldown);
        }

#endif
        #endregion


    }
}