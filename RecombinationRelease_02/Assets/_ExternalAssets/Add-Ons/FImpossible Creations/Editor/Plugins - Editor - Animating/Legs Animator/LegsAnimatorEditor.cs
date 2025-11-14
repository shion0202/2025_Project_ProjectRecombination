using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    [CanEditMultipleObjects]
    [CustomEditor( typeof( LegsAnimator ), true )]
    public partial class LegsAnimatorEditor : Editor
    {
        public LegsAnimator Get { get { if( _get == null ) _get = (LegsAnimator)target; return _get; } }
        private LegsAnimator _get;

        protected bool _requestRepaint = false;

        public override bool UseDefaultMargins()
        {
            return false;
        }

        [MenuItem( "CONTEXT/LegsAnimator/Switch Runtime Performance Debugging", true )]
        static bool IsDebuggingPerformance( MenuCommand menuCommand )
        {
            if( Application.isPlaying == false ) return false;

            if( menuCommand.context is LegsAnimator )
            {
                LegsAnimator targetComponent = (LegsAnimator)menuCommand.context;
                if( targetComponent ) return true;
            }

            return false;
        }

        [MenuItem( "CONTEXT/LegsAnimator/Switch Runtime Performance Debugging" )]
        static void SwitchDebug( MenuCommand menuCommand )
        {
            if( menuCommand.context is LegsAnimator )
            {
                LegsAnimator targetComponent = (LegsAnimator)menuCommand.context;

                if( targetComponent )
                {
                    if( targetComponent == null ) return;
                    if( targetComponent._perf_main == null ) return;
                    targetComponent._perf_main.Editor_SwitchFoldout();
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            if( Application.isPlaying ) return true;

            if( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion )
                if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Glue )
                    if( _glueMainSet == 1 )
                    {
                        return true;
                    }

            return false;
        }


        System.DateTime _lastUpdateTime = new System.DateTime();
        float _editorDelta = 0.1f;
        void UpdateDelta()
        {
            if( Event.current.type == EventType.Repaint )
            {
                _editorDelta = (float)( System.DateTime.Now - _lastUpdateTime ).TotalSeconds;
                _lastUpdateTime = System.DateTime.Now;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical( BGInBoxStyle );

            LegsAnimator._Editor_LastSelectedLA = Get;

            UpdateDelta();
            GUI_Prepare();

            if( Get._Editor_OnValidateTrigger )
            {
                Get._Editor_OnValidateTrigger = false;
                OnChange( false );
            }

            serializedObject.Update();

            DrawLegsAnimatorGUI();

            serializedObject.ApplyModifiedProperties();

            if( _requestRepaint )
            {
                _requestRepaint = false;
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndVertical();
        }


        protected virtual void DrawLegsAnimatorGUI()
        {
            wasWalid = Get.IsSetupValid();

            if( wasWalid == false )
            {
                Helper_Header( "Setup", FGUI_Resources.Tex_GearSetup );
                View_Setup();

                if( Get.Hips != null )
                {
                    EditorGUILayout.Space( 8 );
                    EditorGUILayout.HelpBox( "The setup is not valid yet.\nPrepare LEG BONES FIRST! Then more options will be unlocked!", UnityEditor.MessageType.Warning );

                    EditorGUILayout.Space( 4 );

                    GUI.backgroundColor = new Color( 0.4f, 1f, 0.4f, 1f );
                    if( GUILayout.Button( _guic_autoFind, FGUI_Resources.ButtonStyle, GUILayout.Height( 28 ) ) )
                    {
                        Get.Finding_LegBonesByNamesAndParenting();
                        OnChange();
                    }

                    GUI.backgroundColor = Color.white;
                }

                //FGUI_Inspector.DrawUILineCommon(32);
                GUILayout.Space( 20 );

                EditorGUILayout.BeginHorizontal();

                if( GUILayout.Button( new GUIContent( "  Watch Tutorials", FGUI_Resources.Tex_Tutorials, "Opening link to the tutorials playlist on the youtube" ), FGUI_Resources.ButtonStyle, GUILayout.Height( 22 ) ) )
                {
                    Application.OpenURL( "https://www.youtube.com/playlist?list=PL6MURe5By90lCAwLGntwMrcad4XAAvUNl" );
                }

                if( UserManualFile )
                    if( GUILayout.Button( new GUIContent( "  User Manual", FGUI_Resources.Tex_Manual, "Opening User Manual .pdf file" ), FGUI_Resources.ButtonStyle, GUILayout.Height( 22 ) ) )
                    {
                        EditorGUIUtility.PingObject( UserManualFile );
                        Application.OpenURL( Application.dataPath + AssetDatabase.GetAssetPath( UserManualFile ).Replace( "Assets", "" ) );
                    }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space( 4 );

                if( DemosPackage )
                {
                    bool loaded = false;
                    string demosPath = AssetDatabase.GetAssetPath( DemosPackage );
                    if( AssetDatabase.LoadAssetAtPath( demosPath.Replace( "Legs Animator - Demos.unitypackage", "Demos - Legs Animator" ), typeof( UnityEngine.Object ) ) != null ) loaded = true;

                    if( loaded == false )
                    {
                        EditorGUILayout.BeginHorizontal();
                        if( GUILayout.Button( new GUIContent( " Import Legs Animator Demos", EditorGUIUtility.IconContent( "UnityLogo" ).image ), GUILayout.Height( 22 ) ) ) AssetDatabase.ImportPackage( demosPath, true );
                        if( GUILayout.Button( new GUIContent( FGUI_Resources.TexTargetingIcon, "Go to legs animator directory in the project window." ), GUILayout.Width( 24 ), GUILayout.Height( 22 ) ) ) { EditorGUIUtility.PingObject( DemosPackage ); }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                if( Get.Hips == null )
                    if( AssemblyDefinitions || AssemblyDefinitionsAll )
                    {
                        GUI.color = new Color( 1f, 1f, 1f, 0.5f );
                        EditorGUILayout.BeginHorizontal();
                        if( GUILayout.Button( "Import Assembly Definitions" ) ) AssetDatabase.ImportPackage( AssetDatabase.GetAssetPath( AssemblyDefinitions ), true );
                        if( GUILayout.Button( new GUIContent( "All Fimpossible AssemDefs", "Importing all fimpossible creations assembly definitions, if you use multiple plugins from Fimpossible Creations." ) ) ) AssetDatabase.ImportPackage( AssetDatabase.GetAssetPath( AssemblyDefinitionsAll ), true );
                        EditorGUILayout.EndHorizontal();
                        GUI.color = Color.white;
                    }
            }
            else
            {
                Rect selRect = new Rect();

                selCol = selCol1;
                EditorGUILayout.BeginHorizontal();
                DrawCategoryButton( LegsAnimator.EEditorCategory.Setup, FGUI_Resources.Tex_GearSetup, "Setup" );
                if( Get._EditorCategory == LegsAnimator.EEditorCategory.Setup ) selRect = GUILayoutUtility.GetLastRect();


                var rect = GUILayoutUtility.GetLastRect();
                rect.width = 10;
                rect.position = new Vector2( 2, rect.position.y );
                if( GUI.Button( rect, "", FGUI_Resources.ButtonStyle ) )
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem( new GUIContent( "--- Quick Category Selector ---" ), false, () => { } );
                    menu.AddItem( new GUIContent( "Setup/Main" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Setup; Get._EditorSetupCategory = LegsAnimator.EEditorSetupCategory.Main; } );
                    menu.AddItem( new GUIContent( "Setup/Detection" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Setup; Get._EditorSetupCategory = LegsAnimator.EEditorSetupCategory.Physics; } );
                    menu.AddItem( new GUIContent( "Setup/IK" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Setup; Get._EditorSetupCategory = LegsAnimator.EEditorSetupCategory.IK; } );
                    menu.AddItem( new GUIContent( "Setup/Optimizing" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Setup; Get._EditorSetupCategory = LegsAnimator.EEditorSetupCategory.Optimizing; } );

                    menu.AddItem( new GUIContent( "Motion/Main" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Main; } );
                    //menu.AddItem(new GUIContent("Motion/Hips"), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Hips; });
                    menu.AddItem( new GUIContent( "Motion/Hips/Body Adjust" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Hips; _hipsMainSet = 0; } );
                    menu.AddItem( new GUIContent( "Motion/Hips/Stability" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Hips; _hipsMainSet = 1; } );
                    menu.AddItem( new GUIContent( "Motion/Hips/Elasticity" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Hips; _hipsMainSet = 2; } );
                    //menu.AddItem(new GUIContent("Motion/Gluing"), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Glue; _glueMainSet = 0; });
                    menu.AddItem( new GUIContent( "Motion/Gluing/Main Glue" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Glue; _glueMainSet = 0; } );
                    menu.AddItem( new GUIContent( "Motion/Gluing/Idle Gluing Motion" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Glue; _glueMainSet = 1; } );
                    menu.AddItem( new GUIContent( "Motion/Modules" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Motion; Get._EditorMotionCategory = LegsAnimator.EEditorMotionCategory.Extra; } );

                    menu.AddItem( new GUIContent( "Extra/Helpers" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Extra; Get._EditorExtraCategory = LegsAnimator.EEditorExtraCategory.Main; } );
                    menu.AddItem( new GUIContent( "Extra/Events" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Extra; Get._EditorExtraCategory = LegsAnimator.EEditorExtraCategory.Events; } );
                    menu.AddItem( new GUIContent( "Extra/Control" ), false, () => { Get._EditorCategory = LegsAnimator.EEditorCategory.Extra; Get._EditorExtraCategory = LegsAnimator.EEditorExtraCategory.Control; } );

                    menu.ShowAsContext();
                }


                selCol = selCol2;
                DrawCategoryButton( LegsAnimator.EEditorCategory.Motion, FGUI_Resources.TexMotionIcon, "Motion" );
                if( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion ) selRect = GUILayoutUtility.GetLastRect();

                selCol = selCol3;
                DrawCategoryButton( LegsAnimator.EEditorCategory.Extra, FGUI_Resources.Tex_Extension, "Extra" );
                if( Get._EditorCategory == LegsAnimator.EEditorCategory.Extra ) selRect = GUILayoutUtility.GetLastRect();

                if( Get._EditorCategory == LegsAnimator.EEditorCategory.Setup ) selCol = selCol1;
                else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion ) selCol = selCol2;
                else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Extra ) selCol = selCol3;


                EditorGUILayout.EndHorizontal();

                selRect.position += new Vector2( selRect.width / 2f - 6, 31 );
                selRect.width = 16; selRect.height = 6;
                //if ( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion) selRect.height += 6;
                Color cc = selCol; cc.a = 0.3f;
                GUI.color = cc;
                GUI.DrawTexture( selRect, Tex_Pixel );
                GUI.color = Color.white;

                GUILayout.Space( 9 );


                if( Get._EditorCategory == LegsAnimator.EEditorCategory.Setup ) GUI.color = new Color( 0f, 1f, 0f, 0.0f );
                else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion ) GUI.color = new Color( 0.2f, 0.2f, 1f, 0.11f );
                else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Extra ) GUI.color = new Color( 0.3f, .75f, 1f, 0.06f );


                if( Get._EditorCategory == LegsAnimator.EEditorCategory.Setup )
                {
                    EditorGUILayout.BeginVertical( StyleColorBG );
                    GUI.color = Color.white;

                    //GUI.color = selCol * 0.62f;
                    EditorGUILayout.BeginHorizontal(); // FGUI_Resources.HeaderBoxStyleH
                    GUI.color = Color.white;
                    DrawCategoryButton( LegsAnimator.EEditorSetupCategory.Main, FGUI_Resources.Tex_GearMain, "Main" );
                    DrawCategoryButton( LegsAnimator.EEditorSetupCategory.Physics, FGUI_Resources.Tex_Physics, "Detection" );
                    DrawCategoryButton( LegsAnimator.EEditorSetupCategory.IK, Tex_IK, "IK" );
                    DrawCategoryButton( LegsAnimator.EEditorSetupCategory.Optimizing, FGUI_Resources.TexSmallOptimizeIcon, "Optimizing", 32 );
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space( 5 );
                    FGUI_Inspector.DrawUILineCommon( 1 );

                    if( Get._EditorSetupCategory == LegsAnimator.EEditorSetupCategory.Main )
                        View_Setup();
                    else if( Get._EditorSetupCategory == LegsAnimator.EEditorSetupCategory.Physics )
                        View_Setup_Physics();
                    else if( Get._EditorSetupCategory == LegsAnimator.EEditorSetupCategory.IK )
                        View_Setup_IKSetup();
                    else if( Get._EditorSetupCategory == LegsAnimator.EEditorSetupCategory.Optimizing )
                        View_Setup_OptimSetup();
                }
                else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion )
                {

                    if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Main )
                        selCol = new Color( .5f, 1f, .65f, 1f );
                    else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Hips )
                        selCol = new Color( .5f, .7f, 1f, 1f );
                    else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Glue )
                        selCol = new Color( .7f, .8f, 1f, 1f );
                    else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Extra )
                        selCol = new Color( 0.5f, 1f, 1f, 1f );

                    Color bCol = selCol * 0.8f;
                    bCol.a = 0.06f;
                    GUI.color = bCol;
                    EditorGUILayout.BeginVertical( StyleColorBG );
                    GUI.color = Color.white;

                    //GUI.color = selCol * 0.8f;
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.white;

                    DrawCategoryButton( LegsAnimator.EEditorMotionCategory.Main, Tex_LegStep, "Main" );
                    //DrawCategoryButton(LegsAnimator.EEditorMotionCategory.Foot, Tex_FootStep, "Step");
                    DrawCategoryButton( LegsAnimator.EEditorMotionCategory.Hips, Tex_Hips, "Hips" );
                    DrawCategoryButton( LegsAnimator.EEditorMotionCategory.Glue, Tex_Glue, "Gluing" );
                    DrawCategoryButton( LegsAnimator.EEditorMotionCategory.Extra, FGUI_Resources.Tex_Module, "Modules" );

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space( 5 );
                    FGUI_Inspector.DrawUILineCommon( 1 );

                    if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Main )
                        View_Motion_Main();
                    else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Hips )
                        View_Motion_Hips();
                    //else if (Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Foot)
                    //    View_Motion_Foots();
                    else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Glue )
                        View_Motion_Glue();
                    else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Extra )
                        View_Motion_Modules();
                }
                else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Extra )
                {

                    EditorGUILayout.BeginVertical( StyleColorBG );
                    GUI.color = Color.white;


                    //GUI.color = selCol * 0.8f;
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.white;
                    DrawCategoryButton( LegsAnimator.EEditorExtraCategory.Main, FGUI_Resources.Tex_Sliders, "Helpers" );
                    DrawCategoryButton( LegsAnimator.EEditorExtraCategory.Events, Tex_EventIcon, "Events" );
                    DrawCategoryButton( LegsAnimator.EEditorExtraCategory.Control, Tex_AutoMotion, "Control" );
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space( 5 );
                    FGUI_Inspector.DrawUILineCommon( 1 );

                    if( Get._EditorExtraCategory == LegsAnimator.EEditorExtraCategory.Main )
                        View_Extra_Main();
                    else if( Get._EditorExtraCategory == LegsAnimator.EEditorExtraCategory.Events )
                        View_Extra_Events();
                    else if( Get._EditorExtraCategory == LegsAnimator.EEditorExtraCategory.Control )
                        View_Extra_Controll();

                    View_Extra_HeatmapControls();
                }

                if( Get._EditorCategory >= 0 ) EditorGUILayout.EndVertical();
            }

            if( Application.isPlaying ) DrawPerformance();
        }

        long _perf_totalT = 0;
        long _perf_lastMin = 0;
        long _perf_lastMax = 0;
        dynamic _perf_totalMS = 0;
        int _perf_totalSteps = 0;
        protected void DrawPerformance()
        {
            Get._perf_main.Editor_DisplayFoldoutButton( -9, -5 );
            if( Get._perf_main._foldout )
            {
                bool upd = Get._perf_preUpd.Editor_DisplayAlways( "Preparation:" );
                Get._perf_preLate.Editor_DisplayAlways( "Pre-Processing:" );
                Get._perf_main.Editor_DisplayAlways( "Main Algorithm:" );

                if( upd )
                {
                    _perf_totalT = 0;
                    _perf_totalT += Get._perf_preUpd.AverageTicks;
                    _perf_totalT += Get._perf_preLate.AverageTicks;
                    _perf_totalT += Get._perf_main.AverageTicks;
                    _perf_totalMS = 0;
                    _perf_totalMS += Get._perf_preUpd.AverageMS;
                    _perf_totalMS += Get._perf_preLate.AverageMS;
                    _perf_totalMS += Get._perf_main.AverageMS;

                    _perf_totalSteps += 1;
                    if( _perf_totalSteps > 6 )
                    {
                        if( _perf_totalT > _perf_lastMax ) _perf_lastMax = _perf_totalT;
                        if( _perf_totalT < _perf_lastMin ) _perf_lastMin = _perf_totalT;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField( "Total = " + _perf_totalT + " ticks  " + _perf_totalMS + "ms" );
                GUILayout.Space( 8 );
                if( _perf_lastMax != long.MinValue && _perf_lastMin != long.MaxValue ) EditorGUILayout.LabelField( "Min = " + _perf_lastMin + " ticks  :   Max = " + _perf_lastMax + " ticks" );
                EditorGUILayout.EndHorizontal();
            }
        }

        float editorScaleRef = 0.5f;
        void RefreshEditorScaleRef()
        {
            if( Get.Legs == null ) return;
            if( Get.Hips == null ) return;

            bool validated = false;
            if( Get.Legs.Count > 0 )
            {
                if( Get.Legs[0].BoneStart && Get.Legs[0].BoneMid && Get.Legs[0].BoneEnd ) validated = true;
            }

            editorScaleRef = Get.ScaleReference;
            if( !validated ) editorScaleRef = Get.HipsToGroundDistanceLocal() * 2f + 0.05f;
        }


        protected virtual void OnSceneGUI()
        {
            RefreshEditorScaleRef();

            if( Get._EditorCategory == LegsAnimator.EEditorCategory.Setup )
            {
                if( Get._EditorSetupCategory == LegsAnimator.EEditorSetupCategory.Main )
                {
                    SceneHelper_DrawBoneFocus();
                    if( _displayHipsHubs ) SceneHelper_DrawHipsHubs();
                    SceneHelper_DrawLegSelectorHelper();
                    SceneHelper_DrawDefinedBones();
                    SceneHelper_DrawDefinedBonesHipsLink( new Color( 0.3f, 0.9f, 0.75f, 0.3f ) );
                    SceneHelper_DrawScaleReference();
                }
                else if( Get._EditorSetupCategory == LegsAnimator.EEditorSetupCategory.Physics )
                {
                    SceneHelper_DrawDefinedBones( new Color( 0.2f, 0.6f, 0.3f, 0.3f ) );
                    SceneHelper_DrawRaycastingCastRange();
                    SceneHelper_DrawRaycastingStepDown();
                    SceneHelper_DrawRaycastingPreview( Color.magenta * 0.6f );
                }
                else // IK
                {
                    SceneHelper_DrawIKSetup( Color.yellow, _setupik_selected_leg );
                    SceneHelper_DrawDefinedBonesHipsLink();
                    SceneHelper_DrawFeetLength();

                    if( _setupik_selected_leg >= 0 )
                        if( _setupik_indivFoldout )
                        {
                            var leg = Get.Leg_GetLeg( _setupik_selected_leg );
                            if( leg != null ) leg._Editor_Glue_DrawControls();
                        }

                    if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Main )
                    {
                        for( int l = 0; l < Get.Legs.Count; l++ ) Get.Legs[l]._Editor_Raycasting_DrawControls();
                    }
                }
            }
            else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Motion )
            {
                SceneHelper_DrawIKSetup( new Color( 0.2f, 1f, 0.3f, 0.2f ), -2 );

                if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Main )
                {
                    for( int l = 0; l < Get.Legs.Count; l++ )
                    {
                        Get.Legs[l]._Editor_Align_DrawControls();

                        if( Get.LegsInitialized )
                        {
                            Handles.color = new Color( 1f, 1f, 0.0f, 0.6f );
                            Handles.DrawAAPolyLine( 3f, Get.Legs[l].AnkleIK.srcPosition, Get.Legs[l].ankleAlignedOnGroundHitWorldPos, Get.Legs[l].ankleAlignedOnGroundHitWorldPos + Vector3.forward * 0.1f );
                        }
                    }

                    float rad = Get.LegsInitialized ? Get._stepPointsOverlapRadius : Get.StepPointsOverlapRadius;

                    if( rad > 0f )
                    {
                        Handles.matrix = Get.BaseTransform.localToWorldMatrix;
                        Handles.color = new Color( 0.4f, 0.9f, 0.4f, 0.3f );
                        for( int l = 0; l < Get.Legs.Count; l++ )
                        {
                            var leg = Get.Legs[l];
                            if( leg.BoneEnd == null ) continue;
                            float radius = rad * leg.GlueThresholdMultiplier;
                            Handles.DrawSolidDisc( Get.transform.InverseTransformPoint( leg.BoneEnd.position ), Get.transform.up, radius * 0.35f );
                        }
                        Handles.matrix = Matrix4x4.identity;
                    }
                }
                else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Hips )
                {
                    SceneHelper_DrawRaycastingStepDown();

                    for( int l = 0; l < Get.Legs.Count; l++ )
                    {
                        Get.Legs[l]._Editor_Raycasting_DrawControls();
                        Get.Legs[l]._Editor_Hips_DrawControls();
                    }
                }
                else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Glue )
                {
                    if( _glueMainSet == 0 )
                    {
                        SceneHelper_DrawGlueFloorLevel();
                    }

                    for( int l = 0; l < Get.Legs.Count; l++ )
                    {
                        Get.Legs[l]._Editor_Glue_DrawControls();
                        Get.Legs[l]._Editor_Raycasting_DrawControls();
                        //Get.Legs[l]._Editor_Raycasting_DrawSwingReference();
                    }
                }
                else if( Get._EditorMotionCategory == LegsAnimator.EEditorMotionCategory.Extra )
                {
                    Get._Editor_ModulesOnSceneGUI();
                }

            }
            else if( Get._EditorCategory == LegsAnimator.EEditorCategory.Extra )
            {
                //if (_HeatmapDebugLeg > -2)
                //{
                //    Get.StepHeatmap_DebugOnSceneView(_HeatmapDebugLeg);
                //    return;
                //}

                SceneHelper_DrawIKSetup( new Color( 0.8f, 0.3f, 0.1f, 0.4f ), -2 );

                for( int l = 0; l < Get.Legs.Count; l++ )
                {
                    Get.Legs[l]._Editor_Raycasting_DrawControls();
                }

                if( Get._EditorExtraCategory == LegsAnimator.EEditorExtraCategory.Control || Get._EditorExtraCategory == LegsAnimator.EEditorExtraCategory.Main )
                {
                    SceneHelper_DrawExtraControll();
                }

            }
        }

        //public static int _HeatmapDebugLeg = -1;

    }

}