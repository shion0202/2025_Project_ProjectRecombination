using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {
        bool wasWalid = false;

        static GUIContent __cont = null;
        static GUIContent _cont { get { if (__cont == null) __cont = new GUIContent(); return __cont; } }


        void View_Setup()
        {

            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

            EditorGUIUtility.labelWidth = 108;
            View_Setup_BaseTransform();
            View_Setup_Hips();
            //View_Setup_Spine();

            //GUILayout.Space(3);
            //View_Setup_GroundLayerMask();
            EditorGUIUtility.labelWidth = 0;

            if (Get.Hips == null) EditorGUILayout.HelpBox("Hips reference is required for Legs Animator to work!\nAssign it first!", UnityEditor.MessageType.Warning);

            EditorGUILayout.EndVertical();


            FGUI_Inspector.DrawUILine(0.4f, 0.4f, 1, 2, 0.975f);


            if (Get.Hips != null)
            {

                View_Setup_LegstListFoldableHeader();

                #region Trigger Drawing  View_Setup_DrawLegs()

                if (Get.Legs.Count > 0)
                {
                    //EditorGUILayout.HelpBox("Hips reference is required for Legs Animator to work!\nAssign it first!", UnityEditor.MessageType.Warning);
                    if (_foldout_legsList)
                    {
                        GUILayout.Space(2);
                        View_Setup_DrawLegs();
                    }
                }

                #endregion


                EditorGUILayout.EndVertical();



                // Auto update legs guides
                if (Application.isPlaying == false)
                    if (Get.LegsInitialized == false)
                        if (Get._EditorAllowAutoUpdateFeetParams)
                            if (_legsChanged_refresh)
                            {
                                bool doFeetUpdate = Get._EditorAllowAutoUpdateFeetParams;
                                if (!Get._EditorAllowAutoUpdateFeetParams)
                                {
                                    if (GUILayout.Button(new GUIContent("  Leg Setup Changed - Hit to refresh parameters", FGUI_Resources.Tex_Refresh), FGUI_Resources.ButtonStyle, GUILayout.Height(19)))
                                        doFeetUpdate = true;
                                }

                                if (doFeetUpdate)
                                {
                                    for (int l = 0; l < Get.Legs.Count; l++)
                                    {
                                        var leg = Get.Legs[l];
                                        leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform); OnChange();
                                    }

                                    OnChange();
                                    _legsChanged_refresh = false;
                                }
                            }


                bool legsChanged = View_Setup_SelectedLegSettings();

                if (legsChanged)
                {
                    OnChange();
                    _legsChanged_refresh = true;
                }

                if (Get.Legs.Count != 0)
                    if (_selected_leg < 0)
                    {
                        GUILayout.Space(2);
                        EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);
                        View_Setup_ScaleReference();

                        GUILayout.Space(2);
                        FGUI_Inspector.DrawUILineCommon();

                        EditorGUIUtility.labelWidth = 140;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(sp_DelayedInitialization);
                        var sp = sp_DelayedInitialization.Copy();
                        GUILayout.FlexibleSpace();
                        EditorGUIUtility.labelWidth = 74;
                        sp.Next(false); EditorGUILayout.PropertyField(sp);
                        EditorGUILayout.EndHorizontal();

                        EditorGUIUtility.labelWidth = 140;
                        EditorGUILayout.BeginHorizontal();

                        if (Get.Mecanim)
                        {
                            Get.AnimatePhysics = Get.Mecanim.updateMode == AnimatorUpdateMode.AnimatePhysics;
                        }

                        sp.Next(false);
                        if (!Get.Mecanim) EditorGUILayout.PropertyField(sp); // Animate physics only if no Mecanim
                        sp.Next(false);
                        EditorGUILayout.PropertyField(sp); // Unscaled Delta
                        EditorGUILayout.EndHorizontal();

                        EditorGUIUtility.labelWidth = 0;
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(-5);
                    }


                GUILayout.Space(8);

                EditorGUILayout.BeginHorizontal();

                if (_appliedPreset == EPres.Humanoid)
                    GUI.backgroundColor = Color.white;
                else
                    GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);

                if (GUILayout.Button(new GUIContent("Humanoid Preset", "Setting foot animating settings, raycast mode, leg stretch limit, push settings, stability and hips use."), GUILayout.Height(17)))
                {
                    _appliedPreset = EPres.Humanoid;
                    Get.AnimateFeet = true;
                    Get.SmoothSuddenSteps = 0.5f;
                    Get.LegElevateBlend = 0.6f;
                    Get.LimitFeetYaw = 30f;
                    Get.SwingHelper = 0.005f;
                    //Get.ScaleReferenceMode = LegsAnimator.ELegsScaleReference.PelvisToGroundDistance;

                    Get.RaycastStyle = LegsAnimator.ERaycastStyle.StraightDown;
                    if (Get.LimitLegStretch >= 1f) Get.LimitLegStretch = 0.99f;

                    Get.AllowGlueDrag = 0.6f;
                    Get.StabilizeCenterOfMass = 0.45f;

                    Get.NormalizePush = false;
                    Get.UseHips = true;
                    Get.StabilityAlgorithm = LegsAnimator.EStabilityMode.Universal;

                    var rotStab = Get.GetModuleHelper<LAM_RotationStability>();
                    if (rotStab != null) rotStab.RequestVariable("Rotation Power", 0.5f).SetValue(0.4f);

                    Get.GlueRangeThreshold = 0.5f;
                    Get.LegAnimatingSettings.StepMoveDuration = 0.35f;
                    Get.LegAnimatingSettings.MinFootRaise = 0.1f;
                    Get.LegAnimatingSettings.MaxFootRaise = 0.4f;
                    Get.LegAnimatingSettings.SpherizePower = 0.4f;
                    Get.LegAnimatingSettings.AllowDetachBefore = 1f;
                    Get.LegAnimatingSettings.Curves_RefreshRaiseYAxisCurve();
                    Get.LegAnimatingSettings.Curves_RefreshPushHipsOnMoveCurve();

                    if (Get.LegAnimatingSettings.SpherizeTrack.keys.Length == 3)
                    {
                        var spherizeKeyVal = Get.LegAnimatingSettings.SpherizeTrack.keys[1];
                        spherizeKeyVal.value = 0.5f;
                        Get.LegAnimatingSettings.SpherizeTrack.RemoveKey(1);
                        Get.LegAnimatingSettings.SpherizeTrack.AddKey(spherizeKeyVal);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(0, 1f);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(1, 1f);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(2, 1f);
                    }


                    for (int m = 0; m < Get.CustomModules.Count; m++)
                    {
                        if (Get.CustomModules[m].ModuleReference.GetType() == typeof(LAM_InsectLegsHelper))
                        {
                            Get.CustomModules.RemoveAt(m);
                            break;
                        }
                    }

                    OnChange();
                }


                if (_appliedPreset == EPres.Insect)
                    GUI.backgroundColor = Color.white;
                else
                    GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);

                if (GUILayout.Button(new GUIContent("Insect Preset", "Disabling foot animating, diabling foot adjusters, raycast mode along bones, leg stretch limit, normalize push an universal stability. Gluing animtion settings like move duration, step height, allow detach before, raise Y curve etc. Randomizing each leg slightly."), GUILayout.Height(17)))
                {
                    _appliedPreset = EPres.Insect;
                    Get.AnimateFeet = false;
                    Get.SmoothSuddenSteps = 0.0f;
                    Get.LegElevateBlend = 0.0f;
                    Get.RaycastStyle = LegsAnimator.ERaycastStyle.AlongBones;
                    if (Get.LimitLegStretch >= 1f) Get.LimitLegStretch = 0.99f;
                    Get.SwingHelper = 0f;
                    //Get.ScaleReferenceMode = LegsAnimator.ELegsScaleReference.FirstLegLength;

                    Get.LegAnimatingSettings.StepMoveDuration = 0.25f;
                    Get.LegAnimatingSettings.MinFootRaise = 0.2f;
                    Get.LegAnimatingSettings.MaxFootRaise = 0.65f;
                    Get.LegAnimatingSettings.SpherizePower = 0.1f;
                    Get.LegAnimatingSettings.AllowDetachBefore = 0.95f;

                    Get.NormalizePush = true;
                    Get.UseHips = true;
                    Get.StabilityAlgorithm = LegsAnimator.EStabilityMode.Universal;
                    Get.LegAnimatingSettings.Curves_RefreshRaiseYAxisCurveSpiderPreset();
                    Get.LegAnimatingSettings.Curves_RefreshPushHipsOnMoveCurveSpiderPreset();

                    Get.GlueRangeThreshold = 0.25f;
                    Get.PushHipsOnLegMove = 0.11f;
                    Get.AllowGlueDrag = 0.9f;
                    Get.StabilizeCenterOfMass = 0.8f;

                    var rotStab = Get.GetModuleHelper<LAM_RotationStability>();
                    if (rotStab != null) rotStab.RequestVariable("Rotation Power", 0.5f).SetValue(-0.25f);

                    if (Get._Editor_LegHelperModule)
                    {
                        if (Get.GetModuleHelper<LAM_InsectLegsHelper>() == null)
                        {
                            LegsAnimator.LegsAnimatorCustomModuleHelper helper = new LegsAnimator.LegsAnimatorCustomModuleHelper(Get);
                            helper.ModuleReference = Get._Editor_LegHelperModule;
                            Get.CustomModules.Add(helper);
                        }
                    }

                    if (Get.LegAnimatingSettings.SpherizeTrack.keys.Length == 3)
                    {
                        var spherizeKeyVal = Get.LegAnimatingSettings.SpherizeTrack.keys[1];
                        spherizeKeyVal.value = -0.25f;
                        Get.LegAnimatingSettings.SpherizeTrack.RemoveKey(1);
                        Get.LegAnimatingSettings.SpherizeTrack.AddKey(spherizeKeyVal);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(0, 1f);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(1, 1f);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(2, 1f);
                    }

                    for (int i = 0; i < Get.Legs.Count; i++)
                    {
                        Get.Legs[i].RandomizeIndividualSettings(0.8f, 1f);
                    }

                    OnChange();
                }

                GUI.backgroundColor = Color.white;






                if (_appliedPreset == EPres.Animal)
                    GUI.backgroundColor = Color.white;
                else
                    GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);

                if (GUILayout.Button(new GUIContent("Animal Preset", "Not much difference in comparison to insect preset.\nRaycast mode straigt down, leg stretch limit, normalize push an universal stability. Gluing animtion settings like move duration, step height, allow detach before, raise Y curve etc."), GUILayout.Height(17)))
                {
                    _appliedPreset = EPres.Animal;
                    Get.AnimateFeet = false;
                    Get.SmoothSuddenSteps = 0.0f;
                    Get.LegElevateBlend = 0.0f;
                    Get.RaycastStyle = LegsAnimator.ERaycastStyle.StraightDown;
                    if (Get.LimitLegStretch >= 1f) Get.LimitLegStretch = 0.99f;
                    Get.SwingHelper = 0f;

                    Get.LegAnimatingSettings.StepMoveDuration = 0.45f;
                    Get.LegAnimatingSettings.MinFootRaise = 0.1f;
                    Get.LegAnimatingSettings.MaxFootRaise = 0.4f;
                    Get.LegAnimatingSettings.SpherizePower = 0.175f;
                    Get.LegAnimatingSettings.AllowDetachBefore = 0.9f;

                    Get.NormalizePush = true;
                    Get.UseHips = true;
                    Get.StabilityAlgorithm = LegsAnimator.EStabilityMode.Universal;
                    Get.LegAnimatingSettings.Curves_RefreshRaiseYAxisCurveSpiderPreset();
                    Get.LegAnimatingSettings.Curves_RefreshPushHipsOnMoveCurveSpiderPreset();


                    Get.GlueRangeThreshold = 0.25f;
                    Get.PushHipsOnLegMove = 0.11f;
                    Get.AllowGlueDrag = 0.9f;
                    Get.StabilizeCenterOfMass = 0.8f;

                    var rotStab = Get.GetModuleHelper<LAM_RotationStability>();
                    if (rotStab != null) rotStab.RequestVariable("Rotation Power", 0.5f).SetValue(-0.25f);

                    if (Get._Editor_LegHelperModule)
                    {
                        if (Get.GetModuleHelper<LAM_InsectLegsHelper>() == null)
                        {
                            LegsAnimator.LegsAnimatorCustomModuleHelper helper = new LegsAnimator.LegsAnimatorCustomModuleHelper(Get);
                            helper.ModuleReference = Get._Editor_LegHelperModule;
                            Get.CustomModules.Add(helper);
                        }
                    }

                    if (Get.LegAnimatingSettings.SpherizeTrack.keys.Length == 3)
                    {
                        var spherizeKeyVal = Get.LegAnimatingSettings.SpherizeTrack.keys[1];
                        spherizeKeyVal.value = -0.25f;
                        Get.LegAnimatingSettings.SpherizeTrack.RemoveKey(1);
                        Get.LegAnimatingSettings.SpherizeTrack.AddKey(spherizeKeyVal);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(0, 1f);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(1, 1f);
                        Get.LegAnimatingSettings.SpherizeTrack.SmoothTangents(2, 1f);
                    }

                    OnChange();
                }

                GUI.backgroundColor = Color.white;




                EditorGUILayout.EndHorizontal();

                if (_appliedPreset == EPres.Insect || _appliedPreset == EPres.Animal)
                {
                    if (Get.GetModuleHelper<LAM_InsectLegsHelper>() == null)
                        EditorGUILayout.HelpBox("When animating Spider/Animal Creature, consider adding 'Multi Leg/Leg Helper' module!\nYou can add module under Motion/Modules.", UnityEditor.MessageType.Info);
                }
                else
                {
                    if (Get.Legs.Count > 3)
                        if (Get.GetModuleHelper<LAM_InsectLegsHelper>() == null)
                        {
                            EditorGUILayout.HelpBox("Your character has more than 3 legs, consider adding 'Multi Leg/Legs Helper' module!", UnityEditor.MessageType.Info);
                            if (Get._Editor_LegHelperModule)
                            {
                                if (GUILayout.Button("Add Leg Helper Module"))
                                {
                                    LegsAnimator.LegsAnimatorCustomModuleHelper helper = new LegsAnimator.LegsAnimatorCustomModuleHelper(Get);
                                    helper.ModuleReference = Get._Editor_LegHelperModule;
                                    Get.CustomModules.Add(helper);
                                }
                            }
                        }
                }

            }

        }

        enum EPres { None, Humanoid, Insect, Animal }
        EPres _appliedPreset = EPres.None;

        //bool ensured = false;
        bool _legsChanged_refresh = false;

        protected void View_Setup_GroundLayerMask()
        {
            EditorGUILayout.PropertyField(sp_GroundMask);
        }


        public void View_Setup_ScaleReference()
        {
            EditorGUILayout.BeginHorizontal();

            if (Application.isPlaying == false)
            {
                EditorGUIUtility.labelWidth = 140;
                EditorGUILayout.PropertyField(sp_ScRefMode, GUILayout.MinWidth(230));
                EditorGUIUtility.labelWidth = 0;

                if (Get.ScaleReferenceMode == LegsAnimator.ELegsScaleReference.Custom)
                {
                    _cont.text = " ";
                    EditorGUIUtility.labelWidth = 8;
                    EditorGUILayout.PropertyField(sp_customScaleReferenceValue, _cont);
                    EditorGUIUtility.labelWidth = 0;
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.FloatField(Get.ScaleReference);
                    GUI.enabled = true;
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUIUtility.labelWidth = 140;
                EditorGUILayout.PropertyField(sp_ScRefMode, GUILayout.MinWidth(230));
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.FloatField(Get.ScaleReference);
                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();
        }


        protected bool View_Setup_SelectedLegSettings()
        {
            bool changed = false;

            if (Get.Legs.ContainsIndex(_selected_leg) == false) _selected_leg = -1;

            if (_selected_leg > -1)
            {

                GUILayout.Space(3);
                GUI.backgroundColor = Color.green;
                EditorGUILayout.BeginHorizontal(FGUI_Resources.HeaderBoxStyle);
                GUI.backgroundColor = preBG;

                var sp = GetLegSerializedProperty(_selected_leg);
                var leg = Get.Legs[_selected_leg];

                string selLeg = " Leg  [" + (_selected_leg + 1) + "]  Settings ";
                if (leg.BoneStart) selLeg += " '" + leg.BoneStart.name.ToUpper() + "'";
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField(selLeg, FGUI_Resources.HeaderStyle);

                View_Setup_LegRemoveButton(_selected_leg);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(FGUI_Resources.FrameBoxStyle);


                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
                EditorGUIUtility.labelWidth = 94;

                Transform preSBone = leg.BoneStart; Transform preMBone = leg.BoneMid; Transform preEBone = leg.BoneEnd;

                EditorGUILayout.BeginHorizontal();
                View_Setup_Leg_BoneButton(Tex_smLegStart, leg.BoneStart, _selected_leg); GUILayout.Space(4);
                Leg_AssignStartBone(leg, (Transform)EditorGUILayout.ObjectField("Start Bone: ", leg.BoneStart, typeof(Transform), true));
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                View_Setup_Leg_BoneButton(Tex_smLegMid, leg.BoneMid, _selected_leg); GUILayout.Space(4);
                leg.BoneMid = (Transform)EditorGUILayout.ObjectField("Middle Bone: ", leg.BoneMid, typeof(Transform), true);
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) _requestRepaint = true;

                EditorGUILayout.BeginHorizontal();
                View_Setup_Leg_BoneButton(Tex_smLegEnd, leg.BoneEnd, _selected_leg); GUILayout.Space(4);
                EditorGUI.BeginChangeCheck();
                leg.BoneEnd = (Transform)EditorGUILayout.ObjectField("End Bone: ", leg.BoneEnd, typeof(Transform), true);
                GUI.color = Color.white;

                if (EditorGUI.EndChangeCheck())
                {
                    _requestRepaint = true;
                    if (leg.BoneEnd) { serializedObject.ApplyModifiedProperties(); leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform); OnChange(); serializedObject.ApplyModifiedProperties(); serializedObject.Update(); }
                }

                if (preSBone != leg.BoneStart || preMBone != leg.BoneMid || preEBone != leg.BoneEnd)
                {
                    changed = true;
                }

                if (leg.BoneEnd && Get.AnimateFeet)
                {
                    EditorGUIUtility.labelWidth = 58;
                    GUILayout.Space(8);
                    EditorGUILayout.PropertyField(sp.FindPropertyRelative("UseFeet"), true, GUILayout.Width(80));
                }

                EditorGUILayout.EndHorizontal();
                SerializedProperty spc = null;

                if (leg.BoneEnd && leg.UseFeet)
                {
                    EditorGUIUtility.labelWidth = 130;
                    EditorGUI.indentLevel += 2;
                    spc = sp.FindPropertyRelative("BoneFeet");
                    EditorGUILayout.PropertyField(spc, new GUIContent("└  Bone Feet:", spc.tooltip), true);
                    if (leg.BoneFeet) { spc.Next(false); EditorGUILayout.PropertyField(spc, true); }
                    EditorGUI.indentLevel -= 2;
                }

                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndVertical();

                GUILayout.Space(6);
                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Opposite Leg:", GUILayout.Width(84));
                View_Setup_Leg_OppositeLegButton(leg, _selected_leg, true);

                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 44;
                leg.Side = (LegsAnimator.ELegSide)EditorGUILayout.EnumPopup("Side:", leg.Side);
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(6);

                EditorGUILayout.EndVertical();

                GUILayout.Space(-4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(sp_AnimateFoot, new GUIContent("  Animate Feet:", Tex_FootRotate, sp_AnimateFoot.tooltip), true);
                if (Get.AnimateFeet) EditorGUILayout.HelpBox("For spider setups disable it", UnityEditor.MessageType.None);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8);

                if (GUILayout.Button("Close single leg setup view", FGUI_Resources.ButtonStyle, GUILayout.Height(17))) { _selected_leg = -1; }
                GUILayout.Space(2);

                EditorGUILayout.EndVertical();

                GUILayout.Space(6);
            }

            return changed;
        }

        protected void View_Setup_DrawLegMainSettings()
        {
            FGUI_Inspector.DrawUILine(0.4f, 0.4f, 1, 2, 0.975f);

            GUILayout.Space(2);
            EditorGUILayout.PropertyField(sp_AnimateFoot, new GUIContent("  Animate Feet:", Tex_FootRotate, sp_AnimateFoot.tooltip), true);

            GUILayout.Space(2);
            EditorGUILayout.HelpBox("More details for each Leg under IK category", UnityEditor.MessageType.None);

        }


        protected void View_Setup_LegstListFoldableHeader()
        {
            GUILayout.Space(6);
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            EditorGUILayout.BeginVertical(FGUI_Resources.HeaderBoxStyle);
            GUI.backgroundColor = Color.white;
            GUILayout.Space(1);

            EditorGUILayout.BeginVertical(FGUI_Resources.ViewBoxStyle);
            GUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();

            if (Get.CheckIfSomeOfTheLegsHasNullBone()) GUI.color = Color.yellow; else GUI.color = new Color(0.35f, .9f, 0.35f, 1f);

            if (GUILayout.Button("  " + FGUI_Resources.GetFoldSimbol(_foldout_legsList, true) + "  Legs:  " + Get.Legs.Count, EditorStyles.boldLabel)) _foldout_legsList = !_foldout_legsList;
            GUI.color = Color.white;

            string selectTitle = "[" + (_selected_leg + 1).ToString() + "]";
            if (_selected_leg == -1) selectTitle = "None Selected";
            else
            {
                if (Get.Legs.ContainsIndex(_selected_leg))
                {
                    var leg = Get.Legs[_selected_leg];
                    if (leg.BoneStart) selectTitle += " " + leg.BoneStart.name;
                }
            }

            if (GUILayout.Button(selectTitle, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Unselect"), _selected_leg == -1, () => { Leg_Select(-1); });

                for (int l = 0; l < Get.Legs.Count; l++)
                {
                    string title = "Select [" + (l + 1) + "]";
                    if (Get.Legs[l].BoneStart != null) title += " " + Get.Legs[l].BoneStart.name.ToUpper();

                    //if (Get.Legs[l].Side != LegsAnimator.ELegSide.Undefined) title += " SIDE:" + Get.Legs[l].Side;

                    int cInd = l;
                    menu.AddItem(new GUIContent(title), _selected_leg == cInd, () => { Leg_Select(cInd); });
                }

                menu.ShowAsContext();
            }

            //EditorGUILayout.LabelField("Setup legs to animate them", EditorStyles.helpBox, GUILayout.Width(100));

            if (Get.Legs.Count == 0) GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+ Add Leg", FGUI_Resources.ButtonStyle, GUILayout.Height(19), GUILayout.Width(80))) { Get.Legs_AddNewLeg(); serializedObject.ApplyModifiedProperties(); serializedObject.Update(); OnChange(); _selected_leg = Get.Legs.Count - 1; _foldout_legsList = true; }
            if (Get.Legs.Count == 0) GUI.backgroundColor = preBG;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            //GUILayout.Space(4);
        }


        void View_Setup_LegRemoveButton(int index)
        {
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f, 1f);
            if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(19), GUILayout.Width(25)))
            {
                _toRemove_leg = index;
            }
            GUI.backgroundColor = preC;
        }

        protected bool _foldout_legsList
        {
            get { return Get._Editor_Foldout_Setup_LegsList; }
            set { Get._Editor_Foldout_Setup_LegsList = value; }
        }

        GUIContent _guiC_view_setup_baseTr = null;

        protected void View_Setup_BaseTransform()
        {
            if (Get._Editor_BaseTransform == null)
            {
                if (_guiC_view_setup_baseTr == null || _guiC_view_setup_baseTr.text == "")
                {
                    _guiC_view_setup_baseTr = new GUIContent(sp_BaseTransform.displayName, sp_BaseTransform.tooltip);
                }

                GUI.color = new Color(1f, 1f, 1f, 0.8f);

                EditorGUILayout.BeginHorizontal();
                Transform preTr = (Transform)EditorGUILayout.ObjectField(_guiC_view_setup_baseTr, null, typeof(Transform), true);
                GUI.color = preC;
                GUILayout.Space(2);
                EditorGUILayout.LabelField("(Using this Transform)", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(110));
                EditorGUILayout.EndHorizontal();

                if (preTr != Get._Editor_BaseTransform) { Get._Editor_BaseTransform = preTr; OnChange(); }
            }
            else
            {
                EditorGUILayout.PropertyField(sp_BaseTransform);
            }
        }

        void UpdateLegsAnklesAxes()
        {
            Get.Finders_RefreshAllLegsAnkleAxes();
        }

        bool _displayHipsHubs = false;

        protected void View_Setup_Hips()
        {
            if (Get.Hips == null)
            {
                EditorGUILayout.BeginHorizontal();

                GUI.color = new Color(1f, 1f, 0.5f, 1f);
                EditorGUILayout.PropertyField(sp_Hips);
                GUI.color = preC;

                if (GUILayout.Button("Find", GUILayout.Width(52))) { Get.Finding_SearchForHips(); OnChange(); serializedObject.ApplyModifiedProperties(); UpdateLegsAnklesAxes(); serializedObject.ApplyModifiedProperties(); serializedObject.Update(); }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(-3);
                EditorGUILayout.LabelField("Hips: Parent Bone of the Leg Bones", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                GUI.color = Color.gray;
                if (GUILayout.Button(_displayHipsHubs ? FGUI_Resources.Tex_DownFold : FGUI_Resources.Tex_RightFold, EditorStyles.label, GUILayout.Width(18), GUILayout.Height(18)))
                    _displayHipsHubs = !_displayHipsHubs;
                GUI.color = Color.white;

                EditorGUIUtility.labelWidth = 87;
                EditorGUILayout.PropertyField(sp_Hips);
                EditorGUIUtility.labelWidth = 0;

                if (IsSceneViewVisible)
                    if (GUILayout.Button(FGUI_Resources.Tex_Gizmos, FGUI_Resources.ButtonStyle, GUILayout.Width(21), GUILayout.Height(18)))
                    {
                        SceneHelper_FocusOnInSceneView(Get.Hips, Get.ScaleReference);
                        SceneHelper_FocusOnBone = Get.Hips;
                        RedrawScene();
                    }

                EditorGUILayout.EndHorizontal();

                if (_displayHipsHubs)
                {
                    GUILayout.Space(3);
                    EditorGUILayout.HelpBox("If it's quadruped or some kind of insect, it's legs may be parented to further spine bones. To separate some of the animation calculations, assign parent bones of other legs here.", UnityEditor.MessageType.None);
                    GUILayout.Space(3);
                    EditorGUILayout.LabelField("EXPERIMENTAL FEATURE", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Space(3);
                    var sp = sp_ExtraHipsHubs.Copy();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sp, true);
                    GUILayout.Space(3);

                    if (Get.ExtraHipsHubs.Count > 0)
                    {
                        sp.Next(false);
                        EditorGUIUtility.labelWidth = 160;
                        EditorGUILayout.PropertyField(sp, true); // hubs handling
                        EditorGUIUtility.labelWidth = 0;

                        sp.Next(false);
                        EditorGUILayout.PropertyField(sp); // hubs blend

                        if (Get.HipsHubsHandling == LegsAnimator.EHipsHubsHandling.Detailed)
                        {
                            sp.Next(false);
                            EditorGUILayout.PropertyField(sp); // backbones blend curve
                            sp.Next(false);
                            EditorGUILayout.PropertyField(sp); // backbones elasticity
                        }
                    }

                    EditorGUI.indentLevel--;
                }


                //if (Application.isPlaying && Get.LegsInitialized)
                //{
                //    _Hub_DisplayDebug(Get.HipsSetup);
                //    for (int h = 0; h < Get.HipsHubs.Count; h++) _Hub_DisplayDebug(Get.HipsHubs[h]);
                //}

            }
        }

        //void _Hub_DisplayDebug(LegsAnimator.HipsReference hub)
        //{
        //    EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);

        //    EditorGUILayout.LabelField("Hub: " + hub.bone.name + " Legs: ");
        //    EditorGUILayout.BeginHorizontal();
        //    for (int i = 0; i < hub.ChildLegs.Count; i++)
        //        EditorGUILayout.ObjectField(hub.ChildLegs[i].BoneStart, typeof(Transform), true);
        //    EditorGUILayout.EndHorizontal();

        //    if (hub.HubBackBones?.Count > 0)
        //    {
        //        EditorGUILayout.LabelField("Hub: " + hub.bone.name + " Backbones: ");
        //        EditorGUILayout.BeginHorizontal();
        //        for (int i = 0; i < hub.HubBackBones.Count; i++)
        //            EditorGUILayout.ObjectField(hub.HubBackBones[i].bone, typeof(Transform), true);
        //        EditorGUILayout.EndHorizontal();
        //    }

        //    EditorGUILayout.EndVertical();
        //}

        void View_Setup_Spine()
        {
            if (Get.Hips == null) return;
            EditorGUILayout.BeginHorizontal();
            GUI.color = preC * 0.7f;
            EditorGUILayout.LabelField("(Optional)", GUILayout.Width(111));
            GUI.color = preC;
            _cont.text = "Spine Bone"; _cont.tooltip = "Spine bone used for hips rotation compensation under hips stabilize settings."; _cont.image = null;
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PropertyField(sp_Hips, _cont);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }


        protected void View_Setup_DrawLegs()
        {
            if (_toRemove_leg != -1)
            {
                Get.Legs.RemoveAt(_toRemove_leg);
                _toRemove_leg = -1;
                _selected_leg = -1;
                OnChange();
            }

            for (int i = 0; i < Get.Legs.Count; i++)
            {
                var leg = Get.Legs[i];
                View_Setup_Leg_SingleLine(leg, i);
            }



            if (_selected_leg < 0)
            {
                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);
                View_Setup_DrawLegMainSettings();
                EditorGUILayout.EndVertical();
            }
        }


        int _toRemove_leg = -1;
        int _selected_leg = -1;
        void View_Setup_Leg_SingleLine(LegsAnimator.Leg leg, int index)
        {
            if (_selected_leg == index) GUI.color = new Color(0.9f, 1f, 0.9f, 1f);
            Color preC = GUI.color;
            Color preBG = this.preBG;


            GUI.backgroundColor = Util_IndexColor(index);
            if (_selected_leg == index)
            {
                GUI.backgroundColor = new Color(0.25f, 0.7f, 0.25f, 1f);
                //preBG = GUI.backgroundColor;
            }

            EditorGUILayout.BeginHorizontal(FGUI_Resources.HeaderBoxStyleH);
            GUI.backgroundColor = this.preBG;

            if (_selected_leg == index) GUI.backgroundColor = Color.green;

            if (GUILayout.Button("[" + (index + 1) + "]", FGUI_Resources.ButtonStyle, GUILayout.Width(24), GUILayout.Height(18)))
            {
                if (_selected_leg == index) Leg_Select(-1);
                else
                {
                    SceneHelper_FocusOnBone = null;
                    Leg_Select(index);
                }

                RedrawScene();
            }

            if (_selected_leg == index) GUI.backgroundColor = preBG;

            //EditorGUILayout.LabelField("Bones:", GUILayout.Width(50));
            GUILayout.Space(8);

            EditorGUIUtility.labelWidth = 26;

            EditorGUI.BeginChangeCheck();

            Transform preSBone = leg.BoneStart; Transform preMBone = leg.BoneMid; Transform preEBone = leg.BoneEnd;

            if (View_Setup_Leg_BoneButton(Tex_smLegStart, leg.BoneStart, index)) { Setup_Leg_LegRMB(leg, 0); }
            Leg_AssignStartBone(leg, View_Setup_Leg_BoneField(leg.BoneStart));
            GUI.color = preC;

            GUILayout.Space(8);
            if (View_Setup_Leg_BoneButton(Tex_smLegMid, leg.BoneMid, index)) { Setup_Leg_LegRMB(leg, 1); }
            leg.BoneMid = View_Setup_Leg_BoneField(leg.BoneMid);
            GUI.color = preC;

            GUILayout.Space(8);
            if (View_Setup_Leg_BoneButton(Tex_smLegEnd, leg.BoneEnd, index)) { Setup_Leg_LegRMB(leg, 2); }

            leg.BoneEnd = View_Setup_Leg_BoneField(leg.BoneEnd);
            GUI.color = preC;

            if (preSBone != leg.BoneStart || preMBone != leg.BoneMid || preEBone != leg.BoneEnd)
            {
                _legsChanged_refresh = true;
            }

            EditorGUIUtility.labelWidth = 0;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (Get._EditorAllowAutoUpdateFeetParams) leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform);
                _requestRepaint = true;
                OnChange();
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            GUILayout.FlexibleSpace();


            EditorGUI.BeginChangeCheck();

            View_Setup_Leg_OppositeLegButton(leg, index);


            GUIContent _sideGuiContent = new GUIContent();
            _sideGuiContent.image = leg.Side == LegsAnimator.ELegSide.Left ? Tex_LeftSide : Tex_LeftSideOff;
            _sideGuiContent.tooltip = "Define leg's side of your character";



            if (GUILayout.Button(_sideGuiContent, EditorStyles.label, GUILayout.Width(20)))
            {
                if (IsRightMouseButton())
                {
                    //if (leg.Side == LegsAnimator.ELegSide.Undefined )
                    {
                        leg.DefineLegSide(Get);
                        if (leg.OppositeLegIndex != -1)
                        {
                            var cLeg = leg.GetOppositeLegReference(Get);
                            if (cLeg != null) cLeg.DefineLegSide(Get, leg);
                        }
                    }
                }
                else
                {
                    leg.Side = LegsAnimator.ELegSide.Left;
                    OnChange();
                }
            }

            _sideGuiContent.image = leg.Side == LegsAnimator.ELegSide.Right ? Tex_RightSide : Tex_RightSideOff;
            if (GUILayout.Button(_sideGuiContent, EditorStyles.label, GUILayout.Width(20)))
            {
                if (IsRightMouseButton())
                {
                    //if (leg.Side == LegsAnimator.ELegSide.Undefined )
                    {
                        leg.DefineLegSide(Get);
                        if (leg.OppositeLegIndex != -1)
                        {
                            var cLeg = leg.GetOppositeLegReference(Get);
                            if (cLeg != null) cLeg.DefineLegSide(Get, leg);
                        }
                    }
                }
                else
                {
                    leg.Side = LegsAnimator.ELegSide.Right;
                    OnChange();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);

            GUI.backgroundColor = this.preBG;
            GUI.color = this.preC;

            if (EditorGUI.EndChangeCheck())
            {
                _requestRepaint = true;
            }
        }

        public static bool IsRightMouseButton()
        {
            if (Event.current == null) return false;

            if (Event.current.type == EventType.Used)
                if (Event.current.button == 1)
                    return true;

            return false;
        }

        void Setup_Leg_LegRMB(LegsAnimator.Leg leg, int boneId)
        {
            GenericMenu genericMenu = new GenericMenu();

            genericMenu.AddItem(new GUIContent("Assign Child"), false, () =>
            {
                if (boneId == 0) { if (leg.BoneStart.childCount > 0) leg.BoneStart = leg.BoneStart.GetChild(0); }
                else if (boneId == 1) { if (leg.BoneMid.childCount > 0) leg.BoneMid = leg.BoneMid.GetChild(0); }
                else if (boneId == 2) { if (leg.BoneEnd.childCount > 0) leg.BoneEnd = leg.BoneEnd.GetChild(0); }
                if (Get._EditorAllowAutoUpdateFeetParams) RefreshAllLegsAnkleAxes();
                OnChange();
            });

            genericMenu.ShowAsContext();
        }


        void View_Setup_Leg_OppositeLegButton(LegsAnimator.Leg leg, int index, bool nameOnField = false)
        {
            string oppositeI = "[" + (leg.OppositeLegIndex + 1).ToString() + "]";
            if (leg.OppositeLegIndex == -1) oppositeI = "None";
            int _wdth = leg.OppositeLegIndex == -1 ? 53 : 41;

            if (nameOnField)
                if (leg.BoneStart)
                {
                    oppositeI += " - " + leg.BoneStart.name.ToUpper();
                    _wdth += 64;
                }

            if (leg.OppositeLegIndex < 0)
            {
                //if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_Warning, "Opposite leg should be assigned!"), EditorStyles.label, GUILayout.Height(18)))
                //{
                //    string name = "this leg";
                //    if (leg.BoneStart) name = leg.BoneStart.name;
                //    EditorUtility.DisplayDialog("Opposite leg for " + name + " should be assigned!", "Opposite leg should be assigned!\nChoose ID of the leg on the other side to this leg.\n\nIf bone is named like 'Leg RIGHT 2'\nthen select 'Leg LEFT 2'.", "Ok");
                //}

                GUI.color = new Color(1f, 1f, 0.3f, 1f);
            }

            EditorGUILayout.LabelField(new GUIContent(Tex_OppositeSide, "Leg's opposite leg reference by leg Index"), GUILayout.Height(18), GUILayout.Width(18));

            if (GUILayout.Button(oppositeI, EditorStyles.popup, GUILayout.Width(_wdth)))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("--- Set Opposite Leg to this Leg ---"), false, () =>
                {
                    EditorUtility.DisplayDialog("Set Opposite Leg", "Select which leg is opposite leg (other-mirror-symmetrical side leg) to this leg, it will help algorithm animating legs correctly.", "Ok");
                });


                bool areNotDefinedOppos = false;
                for (int l = 0; l < Get.Legs.Count; l++) if (Get.Legs[l].OppositeLegIndex < 0) { areNotDefinedOppos = true; break; }
                if (areNotDefinedOppos)
                {
                    menu.AddItem(new GUIContent(""), false, () => { });
                    menu.AddItem(new GUIContent(" - Find Opposite Legs Automatically - "), false, () =>
                    {
                        Get.Finder_AutoDefineOppositeLegs();
                        OnChange();
                    });
                    menu.AddItem(new GUIContent(""), false, () => { });
                }

                menu.AddItem(new GUIContent(""), false, () => { });

                menu.AddItem(new GUIContent("None"), leg.OppositeLegIndex == -1, () => { leg.AssignOppositeLegIndex(-1); OnChange(); });

                for (int l = 0; l < Get.Legs.Count; l++)
                {
                    if (l == index) continue;
                    if (Get.Legs[l].OppositeLegIndex > -1) continue;
                    if (Get.Legs[l].Side != LegsAnimator.ELegSide.Undefined && Get.Legs[l].Side == leg.Side) continue;

                    string title = "[" + (l + 1) + "]";
                    if (Get.Legs[l].BoneStart != null) title += " " + Get.Legs[l].BoneStart.name.ToUpper();
                    int cInd = l;
                    menu.AddItem(new GUIContent(title), leg.OppositeLegIndex == cInd, () => { leg.AssignOppositeLegIndex(cInd); OnChange(); });
                }

                menu.ShowAsContext();
            }

            GUI.color = Color.white;
            //if (leg.OppositeLegIndex < 0) GUI.backgroundColor = Color.white;
        }

        /// <summary> True if rmb pressed </summary>
        bool View_Setup_Leg_LegBoneButton(GUIContent g, Transform t, int index)
        {
            if (width < 440) return false;

            bool rmb = Event.current.button == 1;

            if (GUILayout.Button(g, EditorStyles.label, GUILayout.Width(22)))
            {
                if (t == null)
                {
                    EditorUtility.DisplayDialog("Assign Bone Reference, by drag & drop bone transform, from the hierarchy window", g.tooltip, "Ok");
                }
                else
                {
                    if (rmb)
                    {
                        return true;
                    }

                    SceneHelper_FocusOnBone = t;
                    PingObject(t);
                    SceneHelper_FocusOnInSceneView(t, Get.ScaleReference);
                    Leg_Select(index);
                }
            }

            return false;
        }

        /// <summary> True if rmb pressed </summary>
        bool View_Setup_Leg_BoneButton(Texture icon, Transform bRef, int index)
        {
            bool pressed = View_Setup_Leg_LegBoneButton(new GUIContent(icon, "Bone reference for the single leg"), bRef, index);
            if ( bRef == null) GUI.color = Color.yellow;
            return pressed;
        }

        Transform View_Setup_Leg_BoneField(Transform t, float? wdth = 0.11f)
        {
            if (wdth == null)
            {
                return (Transform)EditorGUILayout.ObjectField(GUIContent.none, t, typeof(Transform), true);
            }

            float widthMul = 0.11f;
            if (width < 440) widthMul = 0.135f;
            if (width < 380) widthMul = 0.115f;

            return (Transform)EditorGUILayout.ObjectField(GUIContent.none, t, typeof(Transform), true, GUILayout.Width(width * widthMul));
        }

    }
}