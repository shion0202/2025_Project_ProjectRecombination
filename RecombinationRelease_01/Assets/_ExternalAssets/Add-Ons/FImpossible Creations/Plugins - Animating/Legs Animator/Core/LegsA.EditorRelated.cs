using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator : UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {

        #region Hierarchy Icon

        public virtual string EditorIconPath { get { if (PlayerPrefs.GetInt("AnimsH", 1) == 0) return ""; else return "Legs Animator/SPR_LegsCrop"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }

        #endregion


        #region Performance Measuring

        void MeasurePerformanceUpdate(bool start)
        {
#if UNITY_EDITOR
            if (start) _perf_preUpd.Start(gameObject); else _perf_preUpd.Finish();
#endif
        }

        void MeasurePerformancePreLateUpdate(bool start)
        {
#if UNITY_EDITOR
            if (start) _perf_preLate.Start(gameObject); else _perf_preLate.Finish();
#endif
        }
        void MeasurePerformanceMain(bool start)
        {
#if UNITY_EDITOR
            if (start) _perf_main.Start(gameObject); else _perf_main.Finish();
#endif
        }

        #endregion


        #region Helpers

        public static LegsAnimator _Editor_LastSelectedLA = null;

        #endregion


#if UNITY_EDITOR


        #region Performance Measuring

        public FDebug_PerformanceTest _perf_preUpd = new FDebug_PerformanceTest();
        public FDebug_PerformanceTest _perf_preLate = new FDebug_PerformanceTest();
        public FDebug_PerformanceTest _perf_main = new FDebug_PerformanceTest();

        #endregion


        public bool _Editor_Foldout_Setup_LegsList = true;
        public int _Editor_EnsureCount = 0;
        public bool _Editor_OnValidateTrigger = false;
        public bool _EditorAllowAutoUpdateFeetParams = true;

        public enum EEditorCategory { Setup, Motion, Extra }
        public EEditorCategory _EditorCategory = EEditorCategory.Setup;

        public enum EEditorSetupCategory { Main, Physics, IK, Optimizing }
        public EEditorSetupCategory _EditorSetupCategory = EEditorSetupCategory.Main;
        public enum EEditorMotionCategory { Main, Hips, Glue, Extra }
        public EEditorMotionCategory _EditorMotionCategory = EEditorMotionCategory.Main;
        public enum EEditorExtraCategory { Main, Events, Control }
        public EEditorExtraCategory _EditorExtraCategory = EEditorExtraCategory.Main;


        public Transform _Editor_BaseTransform
        {
            get { return baseTransform; }
            set { baseTransform = value; }
        }

        bool _editor_disabledGizmo = false;
        private void OnValidate()
        {
            if (Application.isPlaying == false)
            {
                User_RefreshHelperVariablesOnParametersChange();
            }
            else // Is Playing == true
            {
                if (LegsInitialized == false) return;
                User_UpdateParametersAfterManualChange();
                Controll_DefineHashes();
            }

            _Editor_OnValidateTrigger = true;
            if( !_editor_disabledGizmo ) { FSceneIcons.SetGizmoIconEnabled(this, false); _editor_disabledGizmo = true; }
        }



#endif

    }

}