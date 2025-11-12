using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {

        protected void View_Extra_Main()
        {
            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

            EditorGUILayout.PropertyField(sp_ExtraPelvisOffset);

            var sp = sp_ExtraPelvisOffset.Copy();

            GUILayout.Space(4);
            sp.Next(false); EditorGUILayout.PropertyField(sp); // Repose After
            sp.Next(false); EditorGUILayout.PropertyField(sp); // Glue on idle

            GUILayout.Space(4);
            sp.Next(false); EditorGUILayout.PropertyField(sp); // Local World Up

            //EditorGUIUtility.labelWidth = 180;
            //EditorGUILayout.PropertyField(sp_StepPointsOverlapRadius);
            //EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(7);

            View_Extra_Main_SubMenu(sp);

            EditorGUILayout.EndVertical();
        }


        //int _extraMainSet = -1;
        void View_Extra_Main_SubMenu(SerializedProperty sp_localup)
        {

            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
            GUILayout.Space(2);

            //_cont.text = "   Hips Push Impulses";
            //_cont.image = Tex_HipsMotion;
            //EditorGUILayout.LabelField(_cont, FGUI_Resources.HeaderStyle);

            EditorGUILayout.HelpBox("You can trigger hips push impulses with coding or using Legs Animator events.", UnityEditor.MessageType.None);

            GUILayout.Space(4);

            EditorGUIUtility.labelWidth = 170;
            var sp = sp_ImpulsesPowerMultiplier.Copy();
            EditorGUILayout.PropertyField(sp); // Impulses mul
            sp.Next(false); EditorGUILayout.PropertyField(sp);
            sp.Next(false); EditorGUILayout.PropertyField(sp); // Damp up

            //GUILayout.Space(5);
            //EditorGUILayout.LabelField("Helper Features", FGUI_Resources.HeaderStyle);
            //sp.Next(false); EditorGUILayout.PropertyField(sp); // On grounded change impulse
            //sp.Next(false); EditorGUILayout.PropertyField(sp); // On Stop impulse

            GUILayout.Space(4);
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Pelvis Push Impulse"))
            {
                Get.DebugPushHipsImpulse.LocalTranslation = _testImpulse;
                Get.DebugPushHipsImpulse.ImpulseDuration = _testImpulseDuration;
                Get.DebugPushHipsImpulse.InheritElasticness = _testImpulseElastic;
                Get.User_AddImpulse(Get.DebugPushHipsImpulse);
            }

            GUILayout.Space(2);
            if (GUI.enabled)
            {
                _testImpulse = EditorGUILayout.Vector3Field("Impulse Power", _testImpulse);
                _testImpulseDuration = EditorGUILayout.FloatField("Impulse Duration", _testImpulseDuration);
                _testImpulseElastic = EditorGUILayout.Slider("Hips Elasticity Blend", _testImpulseElastic, 0f, 1f);
            }

            GUI.enabled = true;

            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(2);
            EditorGUILayout.EndVertical();

        }

        Vector3 _testImpulse = new Vector3(0f, -.1f, 0f);
        float _testImpulseDuration = 0.35f;
        float _testImpulseElastic = 0.75f;

        protected void View_Extra_Events()
        {
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxBlankStyle );

            // Step Event Trigger
            EditorGUILayout.PropertyField( sp_Event_OnStep );
            GUILayout.Space( 4 );

            EditorGUIUtility.labelWidth = 170;
            var sp = sp_Event_OnStep.Copy();

            GUILayout.Space( 6 );
            EditorGUIUtility.labelWidth = 150;

            sp.Next( false );
            EditorGUILayout.PropertyField( sp ); // Step Delay

            EditorGUILayout.BeginHorizontal();
            sp.Next( false );

            EditorGUILayout.PropertyField( sp ); // Step Send on land
            EditorGUIUtility.labelWidth = 120;
            GUILayout.FlexibleSpace();
            sp.Next( false );

            EditorGUILayout.PropertyField( sp ); // Step Prevent
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;

            sp.Next( false );
            EditorGUILayout.PropertyField( sp ); // Step Events On Moving

            bool receiverDetected = false;
            if( Get.StepInfoReceiver )
            {
                if( Get.StepInfoReceiver.gameObject.GetComponent<LegsAnimator.ILegStepReceiver>() == null ) receiverDetected = false; else receiverDetected = true;
            }

            sp.Next( false );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( sp ); // Step Info Receiver

            if( receiverDetected )
            {
                GUILayout.Space( 4 );
                EditorGUILayout.LabelField( new GUIContent( FGUI_Resources.Tex_FCorrect, "ILegStepReceiver found" ), GUILayout.Width( 20 ), GUILayout.Height( 18 ) );
            }

            EditorGUILayout.EndHorizontal();

            if( receiverDetected == false && Get.StepInfoReceiver != null )
            {
                EditorGUILayout.BeginHorizontal( EditorStyles.helpBox );
                EditorGUILayout.LabelField( new GUIContent( FGUI_Resources.Tex_Warning ), GUILayout.Width( 20 ) );
                GUILayout.Space( 6 );
                EditorGUILayout.LabelField( "ILegStepReceiver not found in " + Get.StepInfoReceiver.name );
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox( "Receiver needs to implement 'ILegStepReceiver' interface", UnityEditor.MessageType.None );
                if( GUILayout.Button( new GUIContent( EditorGUIUtility.IconContent( "Clipboard" ).image, "Copy full interface name to the clipboard" ), FGUI_Resources.ButtonStyle, GUILayout.Width( 20 ) ) ) { GUIUtility.systemCopyBuffer = "FIMSpace.FProceduralAnimation.LegsAnimator.ILegStepReceiver"; }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndVertical();
        }

        GUIContent _guic_animatorParam = new GUIContent();

        void GUI_PropertyWithAnimatorVariableSelector(SerializedProperty prop, UnityEngine.AnimatorControllerParameterType type, bool floatOrBoolParam = false)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop);

            if (_guic_animatorParam.image == null) _guic_animatorParam = new GUIContent(" >", EditorGUIUtility.IconContent("AnimatorController Icon").image);
            if (GUILayout.Button(_guic_animatorParam, EditorStyles.boldLabel, GUILayout.Width(28), GUILayout.Height(18)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("None"), prop.stringValue == "", () => { prop.stringValue = ""; prop.serializedObject.ApplyModifiedProperties(); });
                var sp = prop.Copy();

                if (Get.Mecanim.runtimeAnimatorController != null)
                {
                    for (int i = 0; i < Get.Mecanim.parameterCount; i++)
                    {
                        var param = Get.Mecanim.parameters[i];

                        if (param.type == type)
                        {
                            menu.AddItem(new GUIContent(param.name + " (" + param.type.ToString() + ")"), sp.stringValue == param.name, () => { sp.stringValue = param.name; sp.serializedObject.ApplyModifiedProperties(); });
                            continue;
                        }

                        if (floatOrBoolParam)
                            if (param.type == AnimatorControllerParameterType.Bool || param.type == AnimatorControllerParameterType.Float)
                            {
                                menu.AddItem(new GUIContent(param.name + " (" + param.type.ToString() + ")"), sp.stringValue == param.name, () => { sp.stringValue = param.name; sp.serializedObject.ApplyModifiedProperties(); });
                            }
                    }
                }

                menu.ShowAsContext();
            }

            GUILayout.EndHorizontal();
        }

        protected void View_Extra_Controll(bool drawExtras = true)
        {
            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);

            if (drawExtras)
            {
                _cont.text = sp_Mecanim.displayName + " (Optional)"; _cont.tooltip = sp_Mecanim.tooltip; _cont.image = null;
                EditorGUILayout.PropertyField(sp_Mecanim, _cont);
            }

            var sp = sp_Mecanim.Copy();
            if (drawExtras)
            {
                sp.Next(false); if (Get.Mecanim) GUI_PropertyWithAnimatorVariableSelector(sp, AnimatorControllerParameterType.Bool); // EditorGUILayout.PropertyField(sp); // Grounded Param
                sp.Next(false); if (Get.Mecanim) GUI_PropertyWithAnimatorVariableSelector(sp, AnimatorControllerParameterType.Float, true); // Movings Param

                sp.Next(false);

                if (string.IsNullOrWhiteSpace(Get.MovingParameter) == false)
                    if (Get.Mecanim && Get.Mecanim.runtimeAnimatorController != null)
                    {
                        bool movIsFloat = false;
                        int hash = Animator.StringToHash(Get.MovingParameter);

                        for (int i = 0; i < Get.Mecanim.parameterCount; i++)
                        {
                            if (Get.Mecanim.GetParameter(i).nameHash == hash)
                                if (Get.Mecanim.GetParameter(i).type == AnimatorControllerParameterType.Float) movIsFloat = true;
                        }

                        if (movIsFloat)
                        {
                            EditorGUILayout.LabelField("Movement Param is Float, assign Not-Moving Threshold", EditorStyles.centeredGreyMiniLabel);
                            GUILayout.Space(-4);
                            EditorGUILayout.PropertyField(sp, new GUIContent("Stop/Move Threshold:", sp.tooltip));
                            GUILayout.Space(5);
                        }
                    }

                GUILayout.Space(7);

                sp.Next(false); _cont.text = sp.displayName + " (Optional)"; _cont.tooltip = sp.tooltip; _cont.image = null;
                EditorGUILayout.PropertyField(sp, _cont); // Rigidbody
                EditorGUIUtility.labelWidth = 240;
                sp.Next(false); if (string.IsNullOrWhiteSpace(Get.MovingParameter)) EditorGUILayout.PropertyField(sp); // rigidbody ismoving
                sp.Next(false); if (string.IsNullOrWhiteSpace(Get.GroundedParameter)) EditorGUILayout.PropertyField(sp); // grounding with raycasts
                EditorGUIUtility.labelWidth = 0;
                _cont.text = "Current Desired Move Direction: " + Get.DesiredMovementDirection; _cont.tooltip = "(world space move direction)\n(When rigidbody is assigned, rigidbody velocity is used here)\n Control value which helps animation based gluing detection";
                EditorGUILayout.LabelField(_cont, EditorStyles.helpBox);
                // Desired Direction
                GUILayout.Space(7);
            }
            else
            {
                sp.Next(false); sp.Next(false); sp.Next(false); sp.Next(false);
            }


            bool asksSpine = false;
            for (int m = 0; m < Get.CustomModules.Count; m++)
            {
                if (Get.CustomModules[m].ModuleReference == null) continue;
                if (Get.CustomModules[m].ModuleReference.AskForSpineBone) { asksSpine = true; break; }
            }


            sp.Next(false); if (Get.Mecanim) GUI_PropertyWithAnimatorVariableSelector(sp, AnimatorControllerParameterType.Bool); // Sliding
            if (Get.Mecanim) GUI_PropertyWithAnimatorVariableSelector(sp_RagdolledParameter, AnimatorControllerParameterType.Bool); // Ragdolled

            sp.Next(false);
            if (asksSpine) EditorGUILayout.PropertyField(sp);

            bool asksChest = false;
            for (int m = 0; m < Get.CustomModules.Count; m++)
            {
                if (Get.CustomModules[m].ModuleReference == null) continue;
                if (Get.CustomModules[m].ModuleReference.AskForChestBone) { asksChest = true; break; }
            }

            sp.Next(false);
            if (asksChest) EditorGUILayout.PropertyField(sp);

            //sp.Next(false); EditorGUILayout.PropertyField(sp); // Swing Helper
            //sp.Next(false); EditorGUILayout.PropertyField(sp); // Floor Level
            ////sp.Next(false); EditorGUILayout.PropertyField(sp); // IK Point Overlap
            //if (sp.floatValue < -0.2f) sp.floatValue = 0f;

            sp.Next(false);

            //if (Get.FootStepDetection == LegsAnimator.EFootStepDetection.AnimationCurves)
            //{
            //    EditorGUILayout.TextField("[0] Leg Height Param:", "LLegH");
            //    EditorGUILayout.TextField("[1] Leg Height Param:", "RLegH");
            //}

            if (Application.isPlaying)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Controll Report:", FGUI_Resources.HeaderStyle);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Is Grounded: " + Get.IsGrounded, FGUI_Resources.HeaderBoxStyle);
                EditorGUILayout.LabelField("Grounded Time: " + Rnd(Get.GroundedTime), FGUI_Resources.HeaderBoxStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Is Moving: " + Get.IsMoving, FGUI_Resources.HeaderBoxStyle);
                EditorGUILayout.LabelField("Moving Time: " + Rnd(Get.MovingTime), FGUI_Resources.HeaderBoxStyle);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("During playmode there will be displayed debug parameters.", UnityEditor.MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }


        protected void View_Extra_HeatmapControls()
        {
            //_HeatmapDebugLeg = EditorGUILayout.IntField("Heatmap Debug Leg", _HeatmapDebugLeg);
            //EditorGUILayout.PropertyField(sp__StepHeatPenaltyCurve);
            //var sp = sp__StepHeatPenaltyCurve.Copy(); sp.Next(false);
            //EditorGUILayout.PropertyField(sp);
        }


        float Rnd(float v)
        {
            return (float)System.Math.Round(v, 2);
        }

    }
}