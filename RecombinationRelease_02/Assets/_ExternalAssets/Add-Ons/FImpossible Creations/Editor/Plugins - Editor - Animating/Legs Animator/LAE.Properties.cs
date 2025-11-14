using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimatorEditor : Editor
    {
        [HideInInspector] public Object ModulesDirectory;
        [HideInInspector] public Object MotionPresetsDirectory;
        [HideInInspector] public Object ImpulsesDirectory;
        [HideInInspector] public Object DemosPackage;
        [HideInInspector] public Object UserManualFile;
        [HideInInspector] public Object AssemblyDefinitions;
        [HideInInspector] public Object AssemblyDefinitionsAll;


        public SerializedProperty sp_Debug_IsGrounded;
        public SerializedProperty sp_BaseTransform;
        public SerializedProperty sp_LegsAnimatorBlend;
        public SerializedProperty sp_Hips;
        public SerializedProperty sp_HipsChildSpineBone;
        public SerializedProperty sp_DelayedInitialization;
        public SerializedProperty sp_Legs;
        public SerializedProperty sp_ImpulsesPowerMultiplier;
        //public SerializedProperty sp_LegsMainSettings;
        public SerializedProperty sp_AnimateFoot;
        public SerializedProperty sp_ScRefMode;
        public SerializedProperty sp_customScaleReferenceValue;
        public SerializedProperty sp_GroundMask;
        public SerializedProperty sp_IKHint;
        public SerializedProperty sp_CastDistance;
        public SerializedProperty sp_MaxStepDown;
        public SerializedProperty sp_StabilityAlgorithm;
        public SerializedProperty sp_UseHipsRotation;
        public SerializedProperty sp_Event_OnStep;
        public SerializedProperty sp_ExtraPelvisOffset;
        public SerializedProperty sp_Mecanim;
        public SerializedProperty sp_HipsSetup;
        public SerializedProperty sp_BaseLegAnimating;
        public SerializedProperty sp_SmoothSuddenSteps;
        public SerializedProperty sp_HipsAdjustingBlend;
        public SerializedProperty sp_GlueBlend;
        public SerializedProperty sp_GlueMode;
        public SerializedProperty sp_MotionInfluence;
        public SerializedProperty sp_StepPointsOverlapRadius;

        public SerializedProperty sp_DisableIfInvisible;
        public SerializedProperty sp_DisableIfInvisibleArray;
        public SerializedProperty sp_FadeOffAtDistance;
        public SerializedProperty sp_SwingHelper;
        public SerializedProperty sp_AnimationFloorLevel;
        public SerializedProperty sp_ExtraHipsHubs;
        public SerializedProperty sp__StepHeatPenaltyCurve;
        public SerializedProperty sp_RagdolledParameter;

        protected virtual void OnEnable()
        {
            Get.Legs_RefreshLegsOwner();
            Get.DoBackCompatibilityChecks();

            sp_Debug_IsGrounded = serializedObject.FindProperty("Debug_IsGrounded");
            sp_BaseTransform = serializedObject.FindProperty("baseTransform");
            sp_LegsAnimatorBlend = serializedObject.FindProperty("LegsAnimatorBlend");

            sp_Hips = serializedObject.FindProperty("Hips");
            sp_HipsChildSpineBone = serializedObject.FindProperty("HipsChildSpineBone");
            sp_DelayedInitialization = serializedObject.FindProperty("DelayedInitialization");
            sp_Legs = serializedObject.FindProperty("Legs");
            sp_ImpulsesPowerMultiplier = serializedObject.FindProperty("ImpulsesPowerMultiplier");
            sp_AnimateFoot = serializedObject.FindProperty("AnimateFeet");
            //sp_LegsMainSettings = serializedObject.FindProperty("LegsMainSettings");
            sp_ScRefMode = serializedObject.FindProperty("ScaleReferenceMode");
            sp_customScaleReferenceValue = serializedObject.FindProperty("customScaleReferenceValue");
            sp_GroundMask = serializedObject.FindProperty("GroundMask");
            sp_IKHint = serializedObject.FindProperty("IKHintMode");
            sp_CastDistance = serializedObject.FindProperty("CastDistance");
            sp_MaxStepDown = serializedObject.FindProperty( "BodyStepDown" );
            sp_StabilityAlgorithm = serializedObject.FindProperty("StabilityAlgorithm");
            sp_UseHipsRotation = serializedObject.FindProperty("UseHipsRotation");
            sp_Event_OnStep = serializedObject.FindProperty("Event_OnStep");
            sp_ExtraPelvisOffset = serializedObject.FindProperty("ExtraPelvisOffset");
            sp_Mecanim = serializedObject.FindProperty("Mecanim");
            sp_HipsSetup = serializedObject.FindProperty("HipsSetup");
            sp_BaseLegAnimating = serializedObject.FindProperty("BaseLegAnimating");
            sp_SmoothSuddenSteps = serializedObject.FindProperty("SmoothSuddenSteps");
            sp_HipsAdjustingBlend = serializedObject.FindProperty("UseHips");
            //sp_HipsAdjustingBlend = serializedObject.FindProperty("HipsAdjustingBlend");
            sp_GlueBlend = serializedObject.FindProperty("UseGluing");
            //sp_GlueBlend = serializedObject.FindProperty("GlueBlend");
            sp_GlueMode = serializedObject.FindProperty("GlueMode");
            sp_MotionInfluence = serializedObject.FindProperty("MotionInfluence");
            sp_StepPointsOverlapRadius = serializedObject.FindProperty("StepPointsOverlapRadius");

            sp_DisableIfInvisible = serializedObject.FindProperty("DisableIfInvisible");
            sp_DisableIfInvisibleArray = serializedObject.FindProperty("DisableIfInvisibleExtraRenderers");
            sp_FadeOffAtDistance = serializedObject.FindProperty("FadeOffAtDistance");
            sp_SwingHelper = serializedObject.FindProperty("SwingHelper");
            sp_AnimationFloorLevel = serializedObject.FindProperty("AnimationFloorLevel");
            sp_ExtraHipsHubs = serializedObject.FindProperty("ExtraHipsHubs");
            sp__StepHeatPenaltyCurve = serializedObject.FindProperty("_StepHeatPenaltyCurve");
            sp_RagdolledParameter = serializedObject.FindProperty("RagdolledParameter");

            OnChange(false);
        }

        SerializedProperty GetLegSerializedProperty(int leg)
        {
            if (leg < 0) return null;
            if (leg >= Get.Legs.Count) return null;
            return sp_Legs.GetArrayElementAtIndex(leg);
        }



        protected virtual void OnChange(bool dirty = true)
        {
            if ( dirty) EditorUtility.SetDirty(Get);
            _perf_lastMin = long.MaxValue;
            _perf_lastMax = long.MinValue;
            _perf_totalSteps = 0;
        }

    }

}