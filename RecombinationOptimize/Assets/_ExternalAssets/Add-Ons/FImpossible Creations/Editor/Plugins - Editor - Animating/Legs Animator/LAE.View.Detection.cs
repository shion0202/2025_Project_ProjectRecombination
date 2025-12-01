using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {

        protected void View_Setup_Physics()
        {
            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

            SerializedProperty sp_cast = sp_CastDistance.Copy();

            if (Get.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting) GUI.color = new Color(1f, 1f, 1f, 0.6f);
            View_Setup_GroundLayerMask();

            FGUI_Inspector.DrawUILineCommon(11);

            EditorGUIUtility.labelWidth = 148;
            EditorGUIUtility.fieldWidth = 25;

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 98;
            EditorGUILayout.PropertyField(sp_cast); // Cast Distance

            bool raycastDropDraw = Get.RaycastStyle != LegsAnimator.ERaycastStyle.OriginToFoot && Get.RaycastStyle != LegsAnimator.ERaycastStyle.NoRaycasting;

            sp_cast.Next(false);
            _cont.text = "Origin:"; _cont.tooltip = sp_cast.tooltip; _cont.image = null;
            EditorGUIUtility.labelWidth = 48;
            GUILayout.Space(6);
            if (raycastDropDraw) EditorGUILayout.PropertyField(sp_cast, _cont, GUILayout.Width(100)); // Raycast Start Height

            EditorGUILayout.EndHorizontal();

            sp_cast.Next(false);
            if (raycastDropDraw)
                if (Get.RaycastStartHeight == LegsAnimator.ERaycastStartHeight.StaticScaleReference)
                {
                    GUILayout.Space(3);
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 172;
                    EditorGUIUtility.fieldWidth = 38;
                    EditorGUILayout.PropertyField(sp_cast); // Raycast Start Height mul
                    EditorGUI.indentLevel--;
                }

            EditorGUIUtility.labelWidth = 140;
            EditorGUIUtility.fieldWidth = 25;

            GUILayout.Space(9);
            sp_cast.Next(false);

            GUI.color = Color.white;
            EditorGUILayout.PropertyField(sp_cast); // Raycasting style
            sp_cast.Next(false);
            if (Get.RaycastStyle != LegsAnimator.ERaycastStyle.NoRaycasting) EditorGUILayout.PropertyField(sp_cast); // Raycasting shape
            sp_cast.Next(false);
            if (Get.RaycastShape == LegsAnimator.ERaycastMode.Spherecast)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(sp_cast);
                sp_cast.Next(false);
                if (sp_cast.floatValue > 2.99999f) EditorGUILayout.PropertyField(sp_cast); else EditorGUILayout.Slider(sp_cast, 0f, 3f);
                EditorGUI.indentLevel--;
            }
            else
                sp_cast.Next(false);


            if (Get.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting)
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("No raycasting means that the algorithm will simulate floor on zero level of the character. Gluing and modules will work, but aligning/legs elevate/smooth step/hips height adjusting will not be used.", UnityEditor.MessageType.Info);
            }
            else
            {
                GUILayout.Space(4);

                EditorGUI.BeginChangeCheck();

                sp_cast.Next(false);
                EditorGUIUtility.labelWidth = 180;
                EditorGUILayout.PropertyField(sp_cast); // No hit behaviour
                sp_cast.Next(false);

                if (Get.NoRaycastGroundBehaviour == LegsAnimator.ENoRaycastBehviour.KeepAttached)
                {
                    EditorGUI.indentLevel++;
                    sp_cast.Next(false);
                    EditorGUILayout.PropertyField(sp_cast, new GUIContent("Keep Until Stretch:", sp_cast.tooltip)); // Keep Attached until
                    EditorGUI.indentLevel--;
                }
            }

            //GUILayout.Space(4);
            //FGUI_Inspector.DrawUILineCommon(6);

            //sp_cast.Next(false);
            //_cont.text = "  " + sp_cast.displayName; _cont.tooltip = sp_cast.tooltip; _cont.image = Tex_Hips;
            //EditorGUILayout.PropertyField(sp_cast, _cont, GUILayout.Height(16)); // Step Down
            //_cont.image = null; _cont.tooltip = "";
            //sp_cast.Next(false); EditorGUILayout.PropertyField(sp_cast); // Step Up

            //FGUI_Inspector.DrawUILineCommon(6);
            //GUILayout.Space(4);
            //sp_cast.Next(false); sp_cast.Next(false);
            //sp_cast.Next(false); EditorGUILayout.PropertyField(sp_cast); // Unground Speed

            //GUILayout.Space(5);
            //sp_cast.Next(false); EditorGUILayout.PropertyField(sp_cast); // Raycast Mode

            //EditorGUIUtility.labelWidth = 108;
            //View_Setup_BaseTransform();
            //View_Setup_Hips();
            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.EndVertical();
            GUILayout.Space(-3);
        }


        protected void View_Setup_IKSetup()
        {
            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

            SerializedProperty sp_ik = sp_IKHint.Copy();

            EditorGUILayout.PropertyField(sp_ik); // Hint mode
            sp_ik.Next(false);
            EditorGUILayout.PropertyField(sp_ik); // Max Stretch

            GUILayout.Space(5);
            sp_ik.Next(false); EditorGUILayout.PropertyField(sp_ik); // Foot Y Offset
            //sp_ik.Next(false); if (!Get.AnimateFeet) GUI.color = Color.white * 0.75f; EditorGUILayout.PropertyField(sp_ik); // Foots Length
            GUI.color = Color.white;

            FGUI_Inspector.DrawUILineCommon(9);
            View_Setup_IKLegstListHeader();

            if (Get.Legs.ContainsIndex(_setupik_selected_leg) == false) _setupik_selected_leg = -1;
            if (_setupik_selected_leg != -1)
            {
                var leg = Get.Legs[_setupik_selected_leg];
                var legsp = GetLegProperty(_setupik_selected_leg);
                SerializedProperty sp;

                EditorGUI.indentLevel++;

                GUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                _setupik_indivFoldout = EditorGUILayout.Foldout(_setupik_indivFoldout, " Individual Parameters", true);

                if (_setupik_indivFoldout)
                {
                    if (GUILayout.Button(FGUI_Resources.Tex_Random, FGUI_Resources.ButtonStyle, GUILayout.Height(18), GUILayout.Width(23)))
                    {
                        leg.RandomizeIndividualSettings(0.8f, 1f);
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;

                    sp = legsp.FindPropertyRelative("LegBlendWeight");
                    EditorGUILayout.PropertyField(sp); // Leg Blend
                    GUILayout.Space(3);

                    EditorGUIUtility.labelWidth = 210;
                    sp.Next(false); EditorGUILayout.PropertyField(sp); // Duration
                    sp.Next(false); EditorGUILayout.PropertyField(sp); // Raise
                    sp.Next(false); EditorGUILayout.PropertyField(sp); // Glue Threshold
                    sp.Next(false); EditorGUILayout.PropertyField(sp); // Glue Point
                    Vector2 v2v = sp.vector2Value;
                    v2v.x = Mathf.Clamp(v2v.x, -0.5f, 0.5f);
                    v2v.y = Mathf.Clamp(v2v.y, -0.5f, 0.5f);
                    sp.vector2Value = v2v;

                    sp.Next(false); EditorGUILayout.PropertyField(sp); // Leg Stretch 
                    sp.Next(false); EditorGUILayout.PropertyField(sp); // Preset
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                }

                FGUI_Inspector.DrawUILineCommon(12);
                EditorGUILayout.LabelField("Internal Algorithm Parameters Correction:", EditorStyles.boldLabel);
                GUILayout.Space(2);

                if (Application.isPlaying == false)
                    Get._EditorAllowAutoUpdateFeetParams = EditorGUILayout.Toggle(new GUIContent("Auto Update Params", "Allowing to automatically refresh parameters below, when changing legs bones in the inspector window (Editor Only Feature).\nDisable if you want to adjust feet axes fully manually."), Get._EditorAllowAutoUpdateFeetParams);

                sp = legsp.FindPropertyRelative("InverseHint");
                EditorGUILayout.PropertyField(sp);//
                sp.Next(false);
                //sp = legsp.FindPropertyRelative("AnkleToHeel");

                EditorGUIUtility.fieldWidth = 24;

                EditorGUILayout.BeginHorizontal(); EditorGUIUtility.labelWidth = 130;
                EditorGUILayout.PropertyField(sp);
                if (Button_Refresh()) { leg.RefreshLegAnkleToHeelAndFeet(Get.BaseTransform); }
                EditorGUILayout.EndHorizontal();
                sp.Next(false); if (Get.AnimateFeet) EditorGUILayout.PropertyField(sp);

                if (Get.AnimateFeet)
                {
                    GUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();
                    _setupik_axisFoldout = EditorGUILayout.Foldout(_setupik_axisFoldout, " Foot Axis Setup", true);
                    if (Button_Refresh()) { leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform); _setupik_axisFoldout = true; OnChange(); }
                    EditorGUILayout.EndHorizontal();

                    EditorGUIUtility.labelWidth = 140;

                    if (_setupik_axisFoldout)
                    {
                        EditorGUI.indentLevel++;
                        sp.Next(false); EditorGUILayout.PropertyField(sp);
                        sp.Next(false); EditorGUILayout.PropertyField(sp);
                        sp.Next(false); EditorGUILayout.PropertyField(sp);

                        GUILayout.Space(4);
                        sp.Next(false); EditorGUIUtility.labelWidth = 160; EditorGUILayout.PropertyField(sp);
                        sp.Next(false);
                        sp.Next(false);

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(sp);
                        if (EditorGUI.EndChangeCheck()) { sp.serializedObject.ApplyModifiedProperties(); leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform); _requestRepaint = true; sp.serializedObject.Update(); OnChange(); }

                        EditorGUI.indentLevel--;
                    }
                    else { sp.Next(false); sp.Next(false); sp.Next(false); sp.Next(false); }
                }

                EditorGUI.indentLevel--;
                GUILayout.Space(3);
                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUILayout.EndVertical();

            DisplaySetupPoseGUI();

            EditorGUILayout.EndVertical();
        }



        void View_Setup_OptimSetup()
        {
            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

            GUILayout.Space(-5);
            EditorGUILayout.LabelField("Optimization Settings", FGUI_Resources.HeaderStyle);
            FGUI_Inspector.DrawUILineCommon();

            GUILayout.Space(4);

            EditorGUILayout.PropertyField(sp_DisableIfInvisible);
            if ( Get.DisableIfInvisible != null) EditorGUILayout.PropertyField(sp_DisableIfInvisibleArray);
            
            if (Application.isPlaying) if (Get.DisableIfInvisible) EditorGUILayout.HelpBox("The Renderer is " + (Get.DisableIfInvisible.isVisible ? "Visible" : "Invisible!"), UnityEditor.MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(sp_FadeOffAtDistance);
            if (sp_FadeOffAtDistance.floatValue < 0f) sp_FadeOffAtDistance.floatValue = 0f;
            if (sp_FadeOffAtDistance.floatValue == 0f) EditorGUILayout.HelpBox("Zero = not using", UnityEditor.MessageType.None);
            EditorGUILayout.EndHorizontal();
            if (Application.isPlaying) if (sp_FadeOffAtDistance.floatValue > 0f) EditorGUILayout.LabelField("Camera Distance = " + Get.FadeOff_lastCameraDistance + "   Fade = " + System.Math.Round(Get.GetCurrentCullingBlend(), 2), EditorStyles.centeredGreyMiniLabel);

            FGUI_Inspector.DrawUILineCommon(16);

            GUILayout.Space(4);
            EditorGUILayout.HelpBox("LODS ARE NOT YET IMPLEMENTED", UnityEditor.MessageType.Info);
            GUILayout.Space(4);

            GUI.enabled = false;
            int lHeight = 26;

            GUILayout.Space(4);
            EditorGUILayout.FloatField("LOD Max Distance", 100f);
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("LOD 0", FGUI_Resources.ButtonStyle, GUILayout.Height(lHeight))) { }
            if (GUILayout.Button("LOD 1", FGUI_Resources.ButtonStyle, GUILayout.Height(lHeight))) { }
            if (GUILayout.Button("LOD 2", FGUI_Resources.ButtonStyle, GUILayout.Height(lHeight))) { }
            if (GUILayout.Button("Culled", FGUI_Resources.ButtonStyle, GUILayout.Height(lHeight))) { }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.LabelField("LOD 1 : Distances  =  25  to  50");

            GUILayout.Space(4);
            EditorGUILayout.IntSlider("Main Update Rate", 60, 0, 90);
            EditorGUILayout.IntSlider("Raycating Rate", 30, 0, 90);

            GUILayout.Space(4);
            EditorGUILayout.Toggle("Disable Raycast", false);
            EditorGUILayout.Toggle("Fade Off Modules", false);
            EditorGUILayout.Toggle("Fade Off Gluing", false);

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }



        bool _setupik_axisFoldout = false;
        bool _setupik_indivFoldout = false;
        int _setupik_selected_leg = -1;
        void View_Setup_IKLegstListHeader()
        {
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            EditorGUILayout.BeginVertical(FGUI_Resources.HeaderBoxStyle);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginVertical(FGUI_Resources.ViewBoxStyle);
            GUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(" IK Leg Details:  " + Get.Legs.Count, EditorStyles.boldLabel)) { }

            string selectTitle = "[" + (_setupik_selected_leg + 1).ToString() + "]";
            if (_setupik_selected_leg == -1) selectTitle = "None Selected";
            else
            {
                if (Get.Legs.ContainsIndex(_setupik_selected_leg))
                {
                    var leg = Get.Legs[_setupik_selected_leg];
                    if (leg.BoneStart) selectTitle += " " + leg.BoneStart.name;
                }
            }

            if (GUILayout.Button(selectTitle, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Unselect"), _setupik_selected_leg == -1, () => { Leg_IKSetup_Select(-1); });

                for (int l = 0; l < Get.Legs.Count; l++)
                {
                    string title = "Select [" + (l + 1) + "]";
                    if (Get.Legs[l].BoneStart != null) title += " " + Get.Legs[l].BoneStart.name.ToUpper();

                    int cInd = l;
                    menu.AddItem(new GUIContent(title), _setupik_selected_leg == cInd, () => { Leg_IKSetup_Select(cInd); });
                }

                menu.ShowAsContext();
            }

            if (_setupik_selected_leg == -1)
                if (Button_Refresh("Refresh Legs IK feet preparation parameters"))
                {
                    RefreshAllLegsAnkleAxes();
                }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        protected void RefreshAllLegsAnkleAxes()
        {
            for (int l = 0; l < Get.Legs.Count; l++)
            {
                var leg = Get.Legs[l];
                leg.RefreshLegAnkleToHeelAndFeetAndAxes(Get.BaseTransform);
            }

            OnChange();
        }

    }
}