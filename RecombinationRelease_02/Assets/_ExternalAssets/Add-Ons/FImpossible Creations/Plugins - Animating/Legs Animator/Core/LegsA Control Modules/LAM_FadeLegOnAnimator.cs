#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_FadeLegOnAnimator", menuName = "FImpossible Creations/Legs Animator/LAM_FadeLegOnAnimator", order = 1)]
    public class LAM_FadeLegOnAnimator : LegsAnimatorControlModuleBase
    {
        LegsAnimator.Variable _fadeSpeedV;
        LegsAnimator.Variable _layerV;
        LegsAnimator.Variable _idleGlueV;

        float enabledMultiplier = 1f;
        float sd_eneMul = 0f;

        [NonSerialized] LegsAnimator.Leg[] legs; // I have no idea but unity keeps creating serialization cycle on this variable, if not using [NonSerialized] even when it's private variable
        List<int> stateHashes;
        List<int> tagHashes;

        enum ELayerSelectMode { ByIndex, Auto }
        LegsAnimator.Variable _layerMode;
        LegsAnimator.Variable _layerSkip;
        List<int> layersToCheck = null;
        int lastAutoWeightIndex = 0;

        #region Auto Layers Check Init

        bool InitLayerCheck(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (helper.Parent.Mecanim == null) return false;
            if (_layerMode.GetInt() == 0) return false;
            if (_layerMode == null || _layerSkip == null) return false;

            layersToCheck = new List<int>();

            string[] args = _layerSkip.GetString().Split(',');

            for (int i = 0; i < helper.Parent.Mecanim.layerCount; i++) layersToCheck.Add(i);

            for (int a = 0; a < args.Length; a++)
            {
                int parsed;
                if (int.TryParse(args[a], out parsed))
                {
                    layersToCheck.Remove(parsed);
                }
                else
                {
                    int layerNameIndex = -1;
                    for (int i = 0; i < helper.Parent.Mecanim.layerCount; i++)
                    {
                        if (helper.Parent.Mecanim.GetLayerName(i) == args[a])
                        {
                            layerNameIndex = i;
                            break;
                        }
                    }

                    if (layerNameIndex != -1) layersToCheck.Remove(layerNameIndex);
                }
            }

            return true;
        }

        #endregion

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (LA.Mecanim == null)
            {
                Debug.Log("[Legs Animator] Fade On Animation Module: Not found animator reference in legs animator Extra/Control!");
                helper.Enabled = false;
                return;
            }


            _layerV = helper.RequestVariable("Animation Layer", 0);
            _fadeSpeedV = helper.RequestVariable("Fade Speed", 0.75f);
            _idleGlueV = helper.RequestVariable("Idle Glue During Fade", false);

            var tagsV = helper.RequestVariable("Animation State Tag", "");
            var statesV = helper.RequestVariable("Animation State Name", "");

            // Prepare target animation hashes for quick checking animator state
            string animStates = statesV.GetString();
            //animStates = animStates.Replace(" ", "");
            var statesSeparated = animStates.Split(',');

            #region Prepare mecanim hashes

            if (statesSeparated.Length > 0)
            {
                stateHashes = new List<int>();
                for (int i = 0; i < statesSeparated.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(statesSeparated[i])) continue;
                    stateHashes.Add(Animator.StringToHash(statesSeparated[i]));
                }
            }

            string tagNames = tagsV.GetString();
            //tagNames = tagNames.Replace(" ", "");
            var tagsSeparated = tagNames.Split(',');

            if (tagsSeparated.Length > 0)
            {
                tagHashes = new List<int>();
                for (int i = 0; i < tagsSeparated.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(tagsSeparated[i])) continue;
                    tagHashes.Add(Animator.StringToHash(tagsSeparated[i]));
                }
            }

            if (stateHashes.Count == 0 && tagHashes.Count == 0)
            {
                helper.Enabled = false;
                Debug.Log("[Legs Animator] Fade On Animation Module: No assigned animation state names/tags to control module on!");
                return;
            }

            #endregion

            if (helper.customStringList == null)
            {
                helper.Enabled = false;
                Debug.Log("[Legs Animator] Fade On Animation Module: No legs definition!");
                return;
            }

            // Prepare legs to work on
            List<LegsAnimator.Leg> preLegs = new List<LegsAnimator.Leg>();
            for (int i = 0; i < helper.customStringList.Count; i++)
            {
                if (helper.customStringList[i] == "1") preLegs.Add(LA.Legs[i]);
            }

            if (preLegs.Count == 0)
            {
                helper.Enabled = false;
                Debug.Log("[Legs Animator] Fade On Animation Module: No legs definition!");
                return;
            }

            legs = preLegs.ToArray();

            if (_layerV.GetInt() < 0) _layerV.SetValue(0); if (_layerV.GetInt() > LA.Mecanim.layerCount - 1) _layerV.SetValue(LA.Mecanim.layerCount - 1);

            // Auto Layers Check
            _layerMode = helper.RequestVariable("Mode", 0);
            _layerSkip = helper.RequestVariable("Skip", "");
            if (_layerMode.GetInt() == 1)
            {
                if (InitLayerCheck(helper) == false) _layerMode.SetValue(0);
            }
        }


        public override void OnAfterAnimatorCaptureUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            Animator anim = LA.Mecanim;
            if (anim == null) return;

            int layer = _layerV.GetInt();

            if (_layerMode.GetInt() == 1) 
            {
                #region Auto Layer Check

                float mostWeight = 0f;
                int mostWeightI = -1;

                for (int i = layersToCheck.Count-1; i >= 0; i--) // Reverse for to stop checking on 100% weight top layer
                {
                    int idx = layersToCheck[i];
                    float weight = helper.Parent.Mecanim.GetLayerWeight(idx);
                    if (weight > 0.95f) // Dont check if layer has 
                    {
                        mostWeightI = idx;
                        break;
                    }
                    else
                    {
                        if ( weight > mostWeight)
                        {
                            mostWeight = weight;
                            mostWeightI = idx;
                        }
                    }
                }

                layer = mostWeightI;
                lastAutoWeightIndex = layer;

                #endregion
            }

            AnimatorStateInfo animatorInfo = anim.IsInTransition(layer) ? anim.GetNextAnimatorStateInfo(layer) : anim.GetCurrentAnimatorStateInfo(layer);

            bool fadeOut = false;

            for (int n = 0; n < stateHashes.Count; n++)
            {
                if (animatorInfo.shortNameHash == stateHashes[n]) { fadeOut = true; break; }
            }

            if (!fadeOut)
            {
                for (int t = 0; t < tagHashes.Count; t++)
                {
                    if (animatorInfo.tagHash == tagHashes[t]) { fadeOut = true; break; }
                }
            }

            float fadeDur = 0.3f - _fadeSpeedV.GetFloat() * 0.299f;

            if (fadeOut)
            {
                enabledMultiplier = Mathf.SmoothDamp(enabledMultiplier, -0.001f, ref sd_eneMul, fadeDur * 0.9f, 100000f, LA.DeltaTime);
            }
            else
            {
                enabledMultiplier = Mathf.SmoothDamp(enabledMultiplier, 1.01f, ref sd_eneMul, fadeDur, 100000f, LA.DeltaTime);
            }

            enabledMultiplier = Mathf.Clamp01((float)enabledMultiplier);

            for (int l = 0; l < legs.Length; l++)
            {
                legs[l].InternalModuleBlendWeight = enabledMultiplier;
                legs[l].IK_UpdateParamsBase();
            }

            if (_idleGlueV.GetBool())
            {
                if (enabledMultiplier < 0.5f)
                {
                    LA._glueModeExecuted = LegsAnimator.EGlueMode.Idle;
                }
            }
        }


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (legsAnimator.Mecanim == null)
            {
                EditorGUILayout.HelpBox("Unity Animator Reference (Mecanim) is required by this module. Go to Extra/Control category and assign Mecanim reference there!", UnityEditor.MessageType.Warning);
                if (GUILayout.Button("Go to Extra/Control")) { legsAnimator._EditorCategory = LegsAnimator.EEditorCategory.Extra; legsAnimator._EditorExtraCategory = LegsAnimator.EEditorExtraCategory.Control; }
            }

            EditorGUILayout.HelpBox("This module will help to disable legs animator motion, when playing special animations!\nUseful when using Legs Animator on insect creature which playes attack animations using front legs.", UnityEditor.MessageType.Info);

            Animator anim = legsAnimator.Mecanim;
            bool drawLayer = true;
            if (anim)
            {
                if (anim.layerCount < 2) drawLayer = false;
            }

            if (drawLayer)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 34;
                var layerMode = helper.RequestVariable("Mode", 0);

                if (Initialized) GUI.enabled = false;
                ELayerSelectMode selMode = (ELayerSelectMode)layerMode.GetInt();
                selMode = (ELayerSelectMode)EditorGUILayout.EnumPopup(new GUIContent("", "If layer to read animator state/tag from should be selected by index, or by top layer with biggest weight fade"), selMode, GUILayout.MaxWidth(74));
                layerMode.SetValue((int)selMode);
                GUI.enabled = true;

                EditorGUIUtility.labelWidth = 40;

                if (selMode == ELayerSelectMode.ByIndex)
                {
                    GUILayout.Space(6);
                    var layerInd = helper.RequestVariable("Animation Layer", 0);
                    EditorGUIUtility.labelWidth = 42;
                    int indx = EditorGUILayout.IntField(new GUIContent("Index:", "Index to read animator state/tag from"), layerInd.GetInt());
                    if (indx < 0) indx = 0;
                    if (anim) if (indx > anim.layerCount - 1) indx = anim.layerCount - 1;
                    layerInd.SetValue(indx);
                }
                else
                {
                    GUILayout.Space(6);
                    var skipVar = helper.RequestVariable("Skip", "");
                    EditorGUIUtility.labelWidth = 35;
                    string skip = skipVar.GetString();
                    if (Initialized) GUI.enabled = false;
                    skip = EditorGUILayout.TextField(new GUIContent("Skip:", "Write here indexes of upper body layers to skip checking them. You can also write here layer names. To skip multiple layers, use command ',' like: 3,4,6"), skip);
                    skipVar.SetValue(skip);
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 0;

                if (selMode == ELayerSelectMode.Auto) EditorGUILayout.HelpBox("Automatic Layer: " + lastAutoWeightIndex, UnityEditor.MessageType.None);
            }

            #region Draw legs list

            if (helper.customStringList == null) helper.customStringList = new List<string>();
            var list = helper.customStringList;
            int targetCount = legsAnimator.Legs.Count;

            if (list.Count < targetCount)
                while (list.Count < targetCount) list.Add("");
            else
                while (list.Count > targetCount) list.RemoveAt(list.Count - 1);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Select legs to DO FADE-OUT EFFECT ON:", EditorStyles.helpBox);
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

            GUILayout.Space(6);

            var fadeSpd = helper.RequestVariable("Fade Speed", 0.75f);
            fadeSpd.SetMinMaxSlider(0f, 1f);
            fadeSpd.Editor_DisplayVariableGUI();

            if (legsAnimator.UseGluing)
            {
                var idleglueV = helper.RequestVariable("Idle Glue During Fade", false);
                if (!idleglueV.TooltipAssigned) idleglueV.AssignTooltip("Switch to idle glue mode during the fade - it can make possible slow-steps-moving when static animation is being played!");
                idleglueV.Editor_DisplayVariableGUI();
            }

            GUILayout.Space(4);
            FGUI_Inspector.DrawUILineCommon(8);

            GUI.enabled = !legsAnimator.LegsInitialized;
            EditorGUILayout.LabelField("Disable Legs On:", EditorStyles.centeredGreyMiniLabel);
            var hipsVar = helper.RequestVariable("Animation State Tag", "");
            hipsVar.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            var extraMultiplier = helper.RequestVariable("Animation State Name", "");
            extraMultiplier.Editor_DisplayVariableGUI();
            EditorGUILayout.LabelField("Use commas ',' to take into account multiple clips/tags", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(3);
            GUI.enabled = true;

            if (legsAnimator.LegsInitialized)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.enabled = false;
                EditorGUILayout.Slider("Current Weight: ", enabledMultiplier, 0f, 1f);
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
            }
        }

#endif
        #endregion


    }
}