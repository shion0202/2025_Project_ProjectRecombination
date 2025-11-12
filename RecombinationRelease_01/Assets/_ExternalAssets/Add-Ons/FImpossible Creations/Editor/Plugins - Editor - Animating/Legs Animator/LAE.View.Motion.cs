using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimatorEditor
    {
        //int _motionMainSet = 0;
        int _hipsMainSet = 0;
        int _glueMainSet = 0;

        Rect _legAnimRect;
        float _sim_leg = 0f;
        readonly Color motionBSelCol = new Color( 0.7f, 0.8f, 1.1f, 1f );

        protected void View_Motion_Main()
        {
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxBlankStyle );

            EditorGUIUtility.labelWidth = 146;

            GUI.backgroundColor = new Color( 0.5f, 1f, 0.65f, 1f );
            EditorGUILayout.PropertyField( sp_LegsAnimatorBlend, new GUIContent( "  " + sp_LegsAnimatorBlend.displayName, FGUI_Resources.Tex_MiniMotion, sp_LegsAnimatorBlend.tooltip ) );
            GUI.backgroundColor = Color.white;

            //EditorGUIUtility.labelWidth = 0;
            FGUI_Inspector.DrawUILineCommon( 12 );

            EditorGUILayout.PropertyField( sp_AnimateFoot, new GUIContent( "  Animate Feet:", Tex_FootRotate, sp_AnimateFoot.tooltip ), true );
            SerializedProperty sp;

            if( Get.AnimateFeet )
            {
                sp = sp_AnimateFoot.Copy();
                sp.Next( false );
                EditorGUILayout.PropertyField( sp );
            }

            View_Motion_Main_SubMenu();


            GUILayout.Space( 7 );
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 180;
            sp = sp_StepPointsOverlapRadius.Copy();
            EditorGUILayout.PropertyField( sp );
            if( sp.floatValue < -0.5f ) sp.floatValue = 0f;

            sp.Next( false );
            GUILayout.Space( 6 );
            EditorGUIUtility.labelWidth = 70;
            EditorGUILayout.PropertyField( sp, new GUIContent( "On Moving:", "You can blend step overlap radius to different size during running, which is recommended to set it lower during running animations." ), GUILayout.MaxWidth( 90 ) );
            sp.Next( false );
            if( Get.UseStepPointsOverlapRadiusOnMoving )
                EditorGUILayout.PropertyField( sp, GUIContent.none, GUILayout.MaxWidth( 48 ) );
            EditorGUIUtility.labelWidth = 0;
            if( sp.floatValue < -0.5f ) sp.floatValue = 0f;

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;

        }

        bool _align_drawAdv = false;
        void View_Motion_Main_SubMenu()
        {

            EditorGUILayout.EndVertical();
            GUILayout.Space( 5 );

            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

            if( Get.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting )
            {
                EditorGUILayout.HelpBox( "Using No Raycasting mode : so feet adjustements will not be applied!", UnityEditor.MessageType.Info );
                GUI.color = new Color( 1f, 1f, 1f, 0.7f );
            }
            else
            {
                GUILayout.Space( -2 );
                EditorGUILayout.LabelField( "Leg-Foot Align Settings", EditorStyles.centeredGreyMiniLabel );
                GUILayout.Space( -4 );
            }

            EditorGUIUtility.labelWidth = 146;

            GUILayout.Space( 2 );
            var sp = sp_SmoothSuddenSteps.Copy();
            EditorGUILayout.PropertyField( sp ); // Aling blend in speed

            EditorGUILayout.BeginHorizontal();
            sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Leg Elevate
            sp.Next( false ); /*if (Get.LegElevateBlend > 0f) EditorGUILayout.PropertyField(sp); */ // Leg Elevate Height Limit
            if( _align_drawAdv ) GUI.backgroundColor = selCol;
            if( GUILayout.Button( new GUIContent( FGUI_Resources.Tex_Expose, "Few more feet align settings (optional and more details related)" ), FGUI_Resources.ButtonStyle, GUILayout.Width( 22 ), GUILayout.Height( 19 ) ) ) _align_drawAdv = !_align_drawAdv;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if( _align_drawAdv )
            {
                EditorGUILayout.PropertyField( sp_AnimationFloorLevel );
            }

            bool anyFoot = false;
            for( int i = 0; i < Get.Legs.Count; i++ ) if( Get.AnimateFeet ) { anyFoot = true; break; }

            GUI.enabled = anyFoot;

            sp.Next( false );
            GUILayout.Space( 4 );
            _cont.text = "  Foot Align Blend"; _cont.tooltip = sp.tooltip; _cont.image = Tex_FootRotate;
            EditorGUILayout.PropertyField( sp, _cont ); // Foot Rotation Blend

            if( anyFoot )
            {
                //sp.Next(false); EditorGUILayout.PropertyField(sp); // Foot Angle Limit
                //sp.Next(false); EditorGUILayout.PropertyField(sp); // Roll Blend
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Foot Rotation Rapidity
            }
            else
            {
                GUILayout.Space( 4 );
                EditorGUILayout.HelpBox( "Foot Animating is disabled", UnityEditor.MessageType.None );
            }

            GUI.enabled = true;
            EditorGUIUtility.labelWidth = 0;
            GUILayout.Space( 2 );
            EditorGUILayout.EndVertical();
        }



        protected void View_Motion_Hips()
        {
            EditorGUIUtility.labelWidth = 141;
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxBlankStyle );

            var sp = sp_HipsAdjustingBlend.Copy();

            EditorGUILayout.PropertyField( sp );

            if( Get.UseHips == false )
            {
                GUI.enabled = false;
                _hipsMainSet = -1;
            }

            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0;

            View_Motion_Hips_SubMenu( sp );
        }


        void View_Motion_Hips_SubMenu( SerializedProperty sp_hipsAdjBlend )
        {
            GUILayout.Space( 4 );

            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            GUILayout.Space( 2 );

            EditorGUILayout.BeginHorizontal();

            _cont.text = "  Body Adjust";
            _cont.tooltip = "Whole body height-level adaptation with current legs placement on the ground.";
            _cont.image = Tex_Hips;
            if( _hipsMainSet == 0 ) GUI.backgroundColor = motionBSelCol;
            if( GUILayout.Button( _cont, EditorStyles.miniButtonLeft, GUILayout.Height( 16 ) ) ) { if( _hipsMainSet == 0 ) _hipsMainSet = -1; else _hipsMainSet = 0; }

            _cont.text = "  Stability";
            _cont.tooltip = "Changing position of pelvis bone to be synced with legs apart.";
            _cont.image = Tex_Stabilize;
            if( _hipsMainSet == 1 ) GUI.backgroundColor = motionBSelCol; else GUI.backgroundColor = Color.white;
            if( GUILayout.Button( _cont, EditorStyles.miniButtonMid, GUILayout.Height( 16 ) ) ) { if( _hipsMainSet == 1 ) _hipsMainSet = -1; else _hipsMainSet = 1; }
            GUI.backgroundColor = Color.white;

            _cont.text = "  Elasticity";
            _cont.tooltip = "Extra animating process for the hips motion, making it more realistic - less artificial and less stiff.\nUsed in many parts of the legs animator system.";
            _cont.image = Tex_HipsMotion;
            if( _hipsMainSet == 2 ) GUI.backgroundColor = motionBSelCol; else GUI.backgroundColor = Color.white;
            if( GUILayout.Button( _cont, EditorStyles.miniButtonRight, GUILayout.Height( 16 ) ) ) { if( _hipsMainSet == 2 ) _hipsMainSet = -1; else _hipsMainSet = 2; }
            GUI.backgroundColor = Color.white;
            _cont.tooltip = "";


            _cont.image = null;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space( 2 );

            if( _hipsMainSet == 0 )
            {
                GUILayout.Space( 5 );
                EditorGUIUtility.labelWidth = 144;

                if( Get.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting )
                {
                    EditorGUILayout.HelpBox( "Using No Raycasting mode : so body adjustements will not be applied!", UnityEditor.MessageType.Info );
                    GUI.color = new Color( 1f, 1f, 1f, 0.7f );
                }

                var sp = sp_hipsAdjBlend.Copy();
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Anim speed
                EditorGUILayout.BeginHorizontal();
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Anim speed
                sp.Next( false ); EditorGUILayout.PropertyField( sp, GUIContent.none, GUILayout.Width( 50 ) ); // adjust Style
                EditorGUILayout.EndHorizontal();
                GUILayout.Space( 4 );

                FGUI_Inspector.DrawUILineCommon( 8 );

                // Step Params from Setup
                sp = sp_MaxStepDown.Copy();
                EditorGUILayout.PropertyField( sp ); // Max Step Down
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Max Step Up

                FGUI_Inspector.DrawUILineCommon( 8 );
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Unground Speed

                if( Application.isPlaying )
                {
                    EditorGUILayout.LabelField( "Grounded Blend = " + Get.IsGroundedBlend, EditorStyles.helpBox );
                }

                GUILayout.Space( 4 );
            }
            else if( _hipsMainSet == 1 )
            {
                EditorGUIUtility.labelWidth = 150;

                GUILayout.Space( 5 );

                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

                if( Get.UseGluing == false )
                {
                    EditorGUILayout.HelpBox( "Stability is Chained with GLUING. Enable Gluing to see stability effect.", UnityEditor.MessageType.Info );
                }

                GUILayout.Space( 2 );
                EditorGUILayout.PropertyField( sp_StabilityAlgorithm, true );
                GUILayout.Space( 5 );


                var sp = sp_hipsAdjBlend.Copy();
                sp.Next( false );
                sp.Next( false );
                sp.Next( false );
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Stabilize Center of mass
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Keyfr Anim is stable
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Speed

                EditorGUILayout.EndVertical();


                sp.Next( false );
                GUILayout.Space( 5 );


                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.fieldWidth = 30;
                EditorGUILayout.PropertyField( sp ); // Push Hips on legs
                EditorGUIUtility.fieldWidth = 0;

                EditorGUIUtility.labelWidth = 24; sp.Next( false );
                _cont.text = " N:"; _cont.tooltip = "Normalize Pushes: " + sp.tooltip;
                //if (Get.Legs.Count > 2)
                EditorGUILayout.PropertyField( sp, _cont, GUILayout.Width( 44 ) );
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = 0;
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Y Blend
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Push Reaction Rapidity

                EditorGUILayout.EndVertical();


                GUILayout.Space( 5 );
                EditorGUILayout.BeginHorizontal();
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Hips Stretch

                GUILayout.Space( 5 );
                EditorGUIUtility.labelWidth = 44;
                sp.Next( false ); EditorGUILayout.PropertyField( sp, new GUIContent( "Speed:", "Smooth reaction speed of the hips stretch preventer" ), GUILayout.Width( 72 ) ); // Hips Stretch
                sp.floatValue = Mathf.Clamp01( sp.floatValue );
                EditorGUIUtility.labelWidth = 0;

                EditorGUILayout.EndHorizontal();

                //GUILayout.Space(4);

                //EditorGUILayout.BeginHorizontal();
                //if (GUILayout.Button(FGUI_Resources.GetFoldSimbolTex(_extraHipsSettingsFoldout, true), EditorStyles.label, GUILayout.Height(17), GUILayout.Width(22)))
                //    _extraHipsSettingsFoldout = !_extraHipsSettingsFoldout;
                //if (GUILayout.Button("Extra Hips Settings", EditorStyles.boldLabel))
                //    _extraHipsSettingsFoldout = !_extraHipsSettingsFoldout;
                //EditorGUILayout.EndHorizontal();

                //if (_extraHipsSettingsFoldout)
                //{
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Stabilize on move

                // Hips Rotation // UseHipsRotation
                //    GUILayout.Space(5);
                //    sp = sp_UseHipsRotation.Copy();
                //    EditorGUILayout.PropertyField(sp); // Hips Rotation
                //    sp.Next(false); EditorGUILayout.PropertyField(sp); // Spine Child
                //    sp.Next(false); if (Get.HipsChildSpineBone != null) EditorGUILayout.PropertyField(sp); // Child compensate
                //}

                if( Get.CustomModules.Count == 0 )
                {
                    GUILayout.Space( 4 );
                    EditorGUILayout.HelpBox( "Consider using 'Extra/Rotation Stability' module to improve stability animation!", UnityEditor.MessageType.Info );
                }

            }
            else if( _hipsMainSet == 2 )
            {
                GUILayout.Space( 2 );

                var sp = sp_HipsSetup.Copy();
                sp.Next( true );
                EditorGUIUtility.labelWidth = 134;

                EditorGUILayout.PropertyField( sp );
                //sp.Next(true); EditorGUILayout.PropertyField(sp);
                MotionInfluenceProcessor._EditorDrawGUI( sp_MotionInfluence );

                if( Get.HipsSetup.HipsElasticityBlend > 0f )
                {
                    GUILayout.Space( 3 );
                    FGUI_Inspector.DrawUILineCommon();

                    sp = sp_HipsSetup.Copy();
                    sp.Next( true ); sp.NextVisible( false ); sp.Next( true ); sp.NextVisible( false );
                    EditorGUILayout.PropertyField( sp );
                    sp.NextVisible( false ); EditorGUILayout.PropertyField( sp );
                    sp.NextVisible( false ); EditorGUILayout.PropertyField( sp );
                    sp.NextVisible( false ); EditorGUILayout.PropertyField( sp );
                }
            }

            EditorGUIUtility.labelWidth = 0;
            GUILayout.Space( 2 );
            EditorGUILayout.EndVertical();

        }

        //bool _extraHipsSettingsFoldout = false;

        protected void View_Motion_Glue()
        {
            EditorGUIUtility.labelWidth = 92;
            EditorGUIUtility.fieldWidth = 28;
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxBlankStyle );

            var sp = sp_GlueBlend.Copy();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( sp, GUILayout.Width( 142 ) ); // Glue Enable

            if( Get.UseGluing == false )
            {
                GUI.enabled = false;
                _glueMainSet = -1;
            }

            sp.Next( false );
            EditorGUIUtility.labelWidth = 50;
            if( Get.UseGluing ) EditorGUILayout.PropertyField( sp, new GUIContent( "Blend:", sp.tooltip ) ); // Glue Blend
            else EditorGUILayout.Slider( "Blend:", 0f, 0f, 1f );
            EditorGUILayout.EndHorizontal();

            if( Get.IsSlidingBlend > 0f )
            {
                EditorGUILayout.LabelField( "Is Sliding gluing fade = " + ( 1f - Get.IsSlidingBlend ), EditorStyles.centeredGreyMiniLabel );
            }

            EditorGUIUtility.labelWidth = 137;

            bool areOpposites = false;
            for( int l = 0; l < Get.Legs.Count; l++ ) if( Get.Legs[l].OppositeLegIndex != -1 ) { areOpposites = true; break; }
            if( !areOpposites )
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox( "No Opposite Legs! It will result in raising all legs in the same time instead of moving them step by step", UnityEditor.MessageType.Warning );
                if( GUILayout.Button( "Go to setup" ) )
                { Get._EditorCategory = LegsAnimator.EEditorCategory.Setup; Get._EditorSetupCategory = LegsAnimator.EEditorSetupCategory.Main; }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUIUtility.fieldWidth = 28;
            sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Glue Range Treshold
            sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Glue Blend In Speed

            EditorGUILayout.EndVertical();
            EditorGUIUtility.fieldWidth = 0;
            EditorGUIUtility.labelWidth = 0;

            View_Motion_Glue_SubMenu( sp );

            if( Get.UseGluing == false ) GUI.enabled = true;
        }


        bool _showSpherize = false;

        void View_Motion_Glue_SubMenu( SerializedProperty sp_blendinspd )
        {
            GUILayout.Space( 3 );

            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            GUILayout.Space( 5 );

            EditorGUILayout.BeginHorizontal();

            _cont.text = "  Main Glue";
            _cont.tooltip = "";
            _cont.image = Tex_FootGlue;
            if( _glueMainSet == 0 ) GUI.backgroundColor = motionBSelCol;
            if( GUILayout.Button( _cont, EditorStyles.miniButtonLeft, GUILayout.Height( 16 ) ) ) { if( _glueMainSet == 0 ) _glueMainSet = -1; else _glueMainSet = 0; }

            _cont.text = "  Idle Glue Motion";
            _cont.image = Tex_LegMotion;
            _cont.tooltip = "Settings for the automatic leg animation, when character is during Idle mode.";
            if( _glueMainSet == 1 ) GUI.backgroundColor = motionBSelCol; else GUI.backgroundColor = Color.white;
            if( GUILayout.Button( _cont, EditorStyles.miniButtonRight, GUILayout.Height( 16 ) ) ) { if( _glueMainSet == 1 ) _glueMainSet = -1; else _glueMainSet = 1; }
            GUI.backgroundColor = Color.white;

            _cont.image = null;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space( 2 );

            if( _glueMainSet == 0 )
            {
                EditorGUIUtility.labelWidth = 134;
                GUILayout.Space( 4 );
                var sp = sp_blendinspd.Copy();
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Glue Below Foot
                sp.Next( false ); EditorGUILayout.PropertyField( sp ); // Glue Fade out speed
                sp.Next( false ); /*if (Get.AnimateFeet) */EditorGUILayout.PropertyField( sp ); // Unglue On

                GUILayout.Space( 4 );
                sp.Next( false );

                // Allow Drag
                sp.floatValue = EditorGUILayout.Slider( new GUIContent( sp.displayName, sp.tooltip ), sp.floatValue, 0f, sp.floatValue > 1f ? 2f : 1.00001f );

                //sp.Next(false); EditorGUILayout.PropertyField(sp); // Speedup on rot

                GUILayout.Space( 6 );
                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
                GUILayout.Space( -4 );
                EditorGUILayout.LabelField( "Extra, Optional Prameters", EditorStyles.centeredGreyMiniLabel );
                GUILayout.Space( -5 );
                sp = sp_SwingHelper.Copy();
                // Swing Helper
                EditorGUILayout.PropertyField( sp ); sp.Next( false );
                if( Get.LegsInitialized ) EditorGUILayout.HelpBox( "Swing[0] = " + Get.Legs[0]._G_RefernceSwing, UnityEditor.MessageType.None );
                // Glue Floor Level
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField( sp );
                if( sp.floatValue < -0.5f ) sp.floatValue = 0f;

                sp.Next( false );
                GUILayout.Space( 6 );
                EditorGUIUtility.labelWidth = 70;
                EditorGUILayout.PropertyField( sp, new GUIContent( "On Moving:", "You can blend floor height to be lower/higher when moving, to ease feet's floor level detection." ), GUILayout.MaxWidth( 90 ) );
                sp.Next( false );
                if( Get.GluingFloorLevelUseOnMoving )
                    EditorGUILayout.PropertyField( sp, GUIContent.none, GUILayout.MaxWidth( 48 ) );
                EditorGUIUtility.labelWidth = 0;
                if( sp.floatValue < -0.5f ) sp.floatValue = 0f;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();


            }
            else if( _glueMainSet == 1 )
            {

                #region Leg Adjust Animation Display

                EditorGUIUtility.labelWidth = 152;
                EditorGUILayout.BeginVertical();

                GUILayout.Space( 4 );
                var sp = sp_BaseLegAnimating.Copy();

                EditorGUILayout.BeginHorizontal();
                sp.Next( true ); EditorGUILayout.PropertyField( sp );
                _cont.text = ""; _cont.tooltip = "Refresh all curves to default";
                _cont.image = FGUI_Resources.Tex_Refresh;

                bool rmb = Event.current.button == 1;
                if( GUILayout.Button( _cont, FGUI_Resources.ButtonStyle, GUILayout.Height( 18 ), GUILayout.Width( 24 ) ) )
                {
                    if( rmb )
                    {
                        Get.LegAnimatingSettings.LogCurve( "PushHipsOnMoveCurve", Get.LegAnimatingSettings.PushHipsOnMoveCurve );
                        Get.LegAnimatingSettings.LogCurve( "FootRotationCurve", Get.LegAnimatingSettings.FootRotationCurve );
                        Get.LegAnimatingSettings.LogCurve( "SpherizeTrack", Get.LegAnimatingSettings.SpherizeTrack );
                        Get.LegAnimatingSettings.LogCurve( "RaiseYAxisCurve", Get.LegAnimatingSettings.RaiseYAxisCurve );
                        Get.LegAnimatingSettings.LogCurve( "MoveToGoalCurve", Get.LegAnimatingSettings.MoveToGoalCurve );
                    }
                    else
                        Get.LegAnimatingSettings.RefreshDefaultCurves();

                    OnChange();
                }

                EditorGUILayout.EndHorizontal();
                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp );

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();

                GUI.color = Color.gray;
                if( GUILayout.Button( _showSpherize ? FGUI_Resources.Tex_DownFold : FGUI_Resources.Tex_RightFold, EditorStyles.label, GUILayout.Width( 18 ), GUILayout.Height( 18 ) ) )
                    _showSpherize = !_showSpherize;
                GUI.color = Color.white;

                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // Spherize track
                EditorGUILayout.EndHorizontal();

                if( _showSpherize )
                {
                    sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // Spherize multiply
                }
                else
                {
                    sp.NextVisible( false );
                }

                EditorGUILayout.EndVertical();

                FGUI_Inspector.DrawUILineCommon();

                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // Min max step height
                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp );
                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // Height curve
                FGUI_Inspector.DrawUILineCommon();


                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // Speedup
                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // Detach sooner

                //_cont.text = sp.displayName; _cont.tooltip = sp.tooltip; _cont.image = null;
                //Vector2 counterRange = sp.vector2Value; EditorGUIUtility.labelWidth = 182;
                //EditorGUILayout.MinMaxSlider(_cont, ref counterRange.x, ref counterRange.y, 0f, 1f);
                //sp.vector2Value = counterRange; EditorGUIUtility.labelWidth = 152;

                FGUI_Inspector.DrawUILineCommon();



                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp );
                sp.NextVisible( false ); if( Get.AnimateFeet ) EditorGUILayout.PropertyField( sp );
                sp.NextVisible( false ); EditorGUILayout.PropertyField( sp ); // DoStepAnimationOnDistanceFactor

                EditorGUILayout.EndVertical();

                _legAnimRect = GUILayoutUtility.GetLastRect();

                var sett = Get.LegAnimatingSettings;
                if( sett.StepMoveDuration <= 0f ) sett.StepMoveDuration = 0.7f;

                //GUI.Box(_legAnimRect, GUIContent.none, FGUI_Resources.BGInBoxStyleH);

                _sim_leg += ( _editorDelta * 0.7f ) / sett.StepMoveDuration;
                _sim_leg %= 2f;

                Handles.BeginGUI();
                Handles.color = Color.white * 0.7f;

                float wdth = _legAnimRect.width;
                float hght = _legAnimRect.height;
                Vector2 startPos = _legAnimRect.position + new Vector2( wdth * 0.15f, _legAnimRect.size.y );
                Vector2 endPos = _legAnimRect.position + new Vector2( wdth * 0.6f, _legAnimRect.size.y );

                float hOffset = _legAnimRect.height * -1f * Mathf.LerpUnclamped( Get.LegAnimatingSettings.MinFootRaise, Get.LegAnimatingSettings.MaxFootRaise, 0.5f );
                Vector2 currHOffset = new Vector2( 0f, hOffset * sett.RaiseYAxisCurve.Evaluate( _sim_leg ) );

                Vector2 currPos;

                if( _sim_leg < 1f ) currPos = Vector2.LerpUnclamped( startPos + currHOffset, endPos + currHOffset, sett.MoveToGoalCurve.Evaluate( _sim_leg ) );
                else currPos = Vector2.Lerp( endPos, startPos, sett.MoveToGoalCurve.Evaluate( _sim_leg - 1f ) );


                Vector2 tighPos = _legAnimRect.position /*+ new Vector2(wdth * 0.2f, 0f);*/ + new Vector2( wdth * 0.2f, currPos.x * ( 0.05f + 0.2f * sett.PushHipsOnMoveCurve.Evaluate( _sim_leg ) ) );
                Vector2 kneePos = _legAnimRect.center + new Vector2( -wdth * 0.35f + currPos.x * 0.5f, -hght * 0.9f + currPos.y * 0.4f );

                Vector2 footPos = currPos;

                if( Get.AnimateFeet )
                {
                    float footLen = wdth * 0.15f;
                    float rot = 180f + 80f * sett.FootRotationCurve.Evaluate( _sim_leg );

                    Vector2 animP = new Vector2( Mathf.Cos( Mathf.Deg2Rad * rot ), Mathf.Sin( Mathf.Deg2Rad * rot ) ) * footLen;
                    if( _sim_leg > 1f ) animP = Vector2.Lerp( animP, new Vector2( -footLen, 0f ), ( _sim_leg - 1f ) * 3f );
                    currPos += animP;
                }
                else
                {
                    footPos += new Vector2( wdth * 0.035f, 0f );
                }


                Handles.DrawAAPolyLine( 2f, tighPos, kneePos, currPos, footPos );

                Handles.EndGUI();

                #endregion

            }

            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space( 2 );
            EditorGUILayout.EndVertical();



            var spg = sp_GlueMode.Copy();
            GUILayout.Space( 4 );
            GUI.backgroundColor = selCol * 1.25f;
            EditorGUILayout.PropertyField( spg );
            GUI.backgroundColor = Color.white;

            if( Application.isPlaying )
            {
                if( Get.GlueMode == LegsAnimator.EGlueMode.Automatic )
                {
                    GUI.enabled = false;
                    EditorGUILayout.EnumPopup( "Current Mode:", Get._glueModeExecuted );
                    GUI.enabled = true;
                    GUILayout.Space( 4 );
                }
            }

            GUILayout.Space( 2 );

            spg.Next( false );
            EditorGUILayout.PropertyField( spg );
            GUILayout.Space( 4 );

        }



        protected void View_Motion_Modules()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxBlankStyle );

            GUILayout.Space( -5 );
            EditorGUILayout.BeginHorizontal();

            bool reverseDisable = Get.DisableCustomModules;
            reverseDisable = !reverseDisable;
            reverseDisable = EditorGUILayout.Toggle( reverseDisable, GUILayout.Width( 22 ) );
            Get.DisableCustomModules = !reverseDisable;
            GUILayout.Space( 44 );

            GUI.enabled = !Application.isPlaying;

            EditorGUILayout.LabelField( "Extra Features using Modules", FGUI_Resources.HeaderStyle );

            GUI.backgroundColor = selCol;
            if( GUILayout.Button( new GUIContent( FGUI_Resources.Tex_SearchDirectory, "Select available feature module, to be added to this Legs Animator" ), FGUI_Resources.ButtonStyle, GUILayout.Width( 44 ), GUILayout.Height( 18 ) ) )
                View_Motion_Modules_BuiltInSelector();

            if( GUILayout.Button( new GUIContent( " + ", "Add field for new Legs Animator Module" ), FGUI_Resources.ButtonStyle, GUILayout.Width( 22 ), GUILayout.Height( 18 ) ) )
            {
                LegsAnimator.LegsAnimatorCustomModuleHelper helper = new LegsAnimator.LegsAnimatorCustomModuleHelper( Get );
                Get.CustomModules.Add( helper );
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            FGUI_Inspector.DrawUILineCommon( 8 );

            if( Get.CustomModules != null )
                if( !Get.CustomModules.ContainsIndex( _selectedModuleIndex ) ) _selectedModuleIndex = -1;

            if( Get.DisableCustomModules )
            {
                EditorGUILayout.HelpBox( "  All Custom Modules are Disabled", UnityEditor.MessageType.Info );
            }
            else if( Get.CustomModules.Count == 0 )
            {
                EditorGUILayout.LabelField( "No Modules Added Yet", EditorStyles.centeredGreyMiniLabel );
            }
            else
            {
                View_Motion_Modules_DisplayModulesList();
            }

            EditorGUILayout.EndVertical();
            EditorGUIUtility.fieldWidth = 0;
            EditorGUIUtility.labelWidth = 0;

            if( EditorGUI.EndChangeCheck() ) OnChange();
        }

        static int _selectedModuleIndex = -1;

        void View_Motion_Modules_DisplayModulesList()
        {
            for( int i = 0; i < Get.CustomModules.Count; i++ )
            {
                var mod = Get.CustomModules[i];

                if( _selectedModuleIndex == i ) EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
                else EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxBlankStyle );

                View_Motion_Modules_DisplayModuleField( i, mod );

                if( _selectedModuleIndex == i )
                {
                    FGUI_Inspector.DrawUILineCommon();
                    View_Motion_Modules_DisplaySelectedModulePanel( mod );
                    //FGUI_Inspector.DrawUILineCommon(2);
                }

                EditorGUILayout.EndVertical();
            }

            if( _customModuleToRemove > -1 )
            {
                Get.CustomModules.RemoveAt( _customModuleToRemove );
                _customModuleToRemove = -1;
            }
        }

        int _customModuleToRemove = -1;
        void View_Motion_Modules_DisplayModuleField( int index, LegsAnimator.LegsAnimatorCustomModuleHelper module )
        {
            string disp = "";
            int wdth = 22;

            if( module.ModuleReference == null )
            {
                if( index > -1 ) disp = index.ToString();
            }
            else
            {
                if( string.IsNullOrWhiteSpace( module.formattedName ) )
                {
                    int ind = module.ModuleReference.name.IndexOf( "_" );

                    if( ind > 0 )
                        module.formattedName = module.ModuleReference.name.Substring( ind + 1, module.ModuleReference.name.Length - ( ind + 1 ) );
                    else
                        module.formattedName = module.ModuleReference.name;

                    if( module.formattedName.Length > 24 )
                    {
                        module.formattedName = module.formattedName.Substring( 0, 20 ) + "...";
                    }
                }

                disp = module.formattedName;
                wdth = 170;
            }

            EditorGUILayout.BeginHorizontal();

            module.Enabled = EditorGUILayout.Toggle( module.Enabled, GUILayout.Width( 18 ) );
            GUILayout.Space( 4 );


            if( _selectedModuleIndex == index ) GUI.backgroundColor = selCol;
            if( GUILayout.Button( disp, FGUI_Resources.ButtonStyle, GUILayout.MaxWidth( wdth ), GUILayout.Height( 18 ) ) )
            {
                if( _selectedModuleIndex == index )
                    _selectedModuleIndex = -1;
                else
                    _selectedModuleIndex = index;
            }

            GUI.backgroundColor = Color.white;



            if( !Application.isPlaying )
                module.ModuleReference = (LegsAnimatorControlModuleBase)EditorGUILayout.ObjectField( module.ModuleReference, typeof( LegsAnimatorControlModuleBase ), false );
            else
            {
                EditorGUILayout.ObjectField( module.ModuleReference, typeof( LegsAnimatorControlModuleBase ), false, GUILayout.Width( 48 ) );
                GUILayout.Space( 4 );
                EditorGUIUtility.labelWidth = 70;
                EditorGUILayout.ObjectField( "Playmode:", module.PlaymodeModule, typeof( LegsAnimatorControlModuleBase ), true );
                EditorGUIUtility.labelWidth = 0;
            }


            if( index > -1 )
            {
                GUI.backgroundColor = new Color( 1f, 0.75f, 0.75f, 1f );
                GUI.enabled = !Application.isPlaying;

                if( GUILayout.Button( FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Width( 22 ), GUILayout.Height( 18 ) ) )
                {
                    _customModuleToRemove = index;
                }

                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        void View_Motion_Modules_DisplaySelectedModulePanel( LegsAnimator.LegsAnimatorCustomModuleHelper module )
        {
            if( module.CurrentModule == null )
            {
                EditorGUILayout.HelpBox( "First choose some module file for this slot", UnityEditor.MessageType.None );
                return;
            }

            module.CurrentModule.Editor_InspectorGUI( Get, module );

            //FGUI_Inspector.DrawUILineCommon(6);
            //if (GUILayout.Button("Close Module Settings Panel", FGUI_Resources.ButtonStyle, GUILayout.Height(16)))
            //{
            //    _selectedModuleIndex = -1;
            //}
        }

        void View_Motion_Modules_BuiltInSelector()
        {
            if( ModulesDirectory == null )
            {
                EditorUtility.DisplayDialog( "Not Found Presets Directory!", "Can't find Modules Presets directory. You probably removed it from the project. Please try importing the Legs Animator plugin again.", "Ok" );
                return;
            }

            string path = AssetDatabase.GetAssetPath( ModulesDirectory );
            var files = System.IO.Directory.GetFiles( path, "*.asset" );

            if( files != null )
            {
                if( files.Length == 0 )
                {
                    EditorUtility.DisplayDialog( "Not Found Presets in the Directory!", "Can't find Modules Preset files. You probably removed them from the project. Please try importing the Legs Animator plugin again.", "Ok" );
                    return;
                }

                // Reorder
                for( int i = files.Length - 1; i >= 0; i-- )
                {
                    if( System.IO.Path.GetFileNameWithoutExtension( files[i] ).Contains( "_" ) )
                    {
                        for( int o = files.Length - 1; o >= 0; o-- )
                            if( !System.IO.Path.GetFileNameWithoutExtension( files[o] ).Contains( "_" ) )
                            {
                                string swap = files[o];
                                files[o] = files[i];
                                files[i] = swap;
                                break;
                            }
                    }
                }

                GenericMenu draftsMenu = new GenericMenu();

                for( int i = 0; i < files.Length; i++ )
                {
                    LegsAnimatorControlModuleBase modl = AssetDatabase.LoadAssetAtPath<LegsAnimatorControlModuleBase>( files[i] );
                    if( modl )
                    {
                        string displayName = modl.name;
                        displayName = displayName.Replace( "_", "/" );

                        draftsMenu.AddItem( new GUIContent( displayName ), false, (GenericMenu.MenuFunction)( () =>
                        {
                            LegsAnimator.LegsAnimatorCustomModuleHelper helper = new LegsAnimator.LegsAnimatorCustomModuleHelper( Get );
                            helper.ModuleReference = modl;
                            Get.CustomModules.Add( helper );
                            _selectedModuleIndex = Get.CustomModules.Count - 1;
                            this.OnChange();
                        } ) );
                    }
                }

                draftsMenu.ShowAsContext();
            }
        }

    }
}