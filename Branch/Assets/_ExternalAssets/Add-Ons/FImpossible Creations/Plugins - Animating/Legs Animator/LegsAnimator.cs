using System.Collections;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    [AddComponentMenu("FImpossible Creations/Legs Animator")]
    [HelpURL("https://assetstore.unity.com/packages/tools/animation/legs-animator-154245")]
    [DefaultExecutionOrder(-7)]
    public partial class LegsAnimator : MonoBehaviour
    {
        // All Legs Animator code is placed inside the partial classes \\

        /// <summary> Once per frame calculated component's blend value for the algorithms </summary>
        public float _MainBlend { get; protected set; }

        float _lastMainBlend = 1f;
        /// <summary> For extra update calls liek IK refresh </summary>
        protected bool _lastMainBlendChanged { get; private set; }

        public float _MainBlendNoRagdolling { get; protected set; }
        public float _MainBlendPlusGrounded { get; protected set; }

        [Tooltip("Total blend of the plugin effects. When zero it disables most of the calculations (but not all)")]
        [FPD_Suffix(0f, 1f)] public float LegsAnimatorBlend = 1f;

        /// <summary> Used for fading off legs animator on culling camera distance </summary>
        protected float cullingBlend = 1f;

        public float GetCurrentCullingBlend()
        { return cullingBlend; }

        /// <summary> Can be used by inheriting for custom blending amount </summary>
        protected float protectedBlend = 1f;

        /// <summary> If not initialized, it's false, it can be used for optimization </summary>
        protected bool AllowUpdate = false;

        protected bool _started = false;

        private void Start()
        {
            _started = true;
            if (baseTransform == null) baseTransform = transform;
            AllowUpdate = false;

            if (DelayedInitialization == false) Initialize();
            StartCoroutine(IEStart());
        }

        #region Start Coroutine

        private IEnumerator IEStart()
        {
            yield return null;
            yield return null;

            if (Rigidbody == null)
            {
                Rigidbody = BaseTransform.GetComponent<Rigidbody>();
                if (!Rigidbody) Rigidbody = BaseTransform.GetComponentInChildren<Rigidbody>();
                if (!Rigidbody) Rigidbody = BaseTransform.GetComponentInParent<Rigidbody>();
            }

            Initialize();
        }

        private void OnEnable()
        {

            #region Recompile support (Thanks to zORg_alex on our Discord!)

#if UNITY_EDITOR
#if UNITY_2021_3_OR_NEWER
            if (Application.isPlaying) UnityEditor.AssemblyReloadEvents.afterAssemblyReload += ReInitialize;
#endif
#endif
            #endregion

            // Prevent start-disable not activated component error
            if (_started)
            {
                if (!LegsInitialized) { Initialize(); }
                else _wasInstantTriggered = false;
            }
        }

        private void ReInitialize()
        {
            LegsInitialized = false;
            DisposeModules();
            Start();
        }


        #endregion Start Coroutine

        private void CheckActivation()
        {
            // Distance Culling Fading
            if (FadeOffAtDistance < 0.01f) cullingBlend = 1f;
            else
            {
                bool inRange = true;
                if (FadeOff_DistanceToCamera == null) if (Camera.main != null) FadeOff_DistanceToCamera = Camera.main.transform;

                if (FadeOff_DistanceToCamera != null)
                {
                    FadeOff_lastCameraDistance = Vector3.Distance(BaseTransform.position, FadeOff_DistanceToCamera.position);
                    if (FadeOff_lastCameraDistance > FadeOffAtDistance) inRange = false;
                }

                if (inRange)
                {
                    if (cullingBlend < 1f)
                    {
                        cullingBlend = Mathf.Min(1f, Mathf.Lerp(cullingBlend, 1.05f, Time.unscaledDeltaTime * 6f));
                        cullingBlend = Mathf.MoveTowards(cullingBlend, 1f, Time.unscaledDeltaTime);
                    }
                }
                else cullingBlend = Mathf.MoveTowards(cullingBlend, 0f, Time.unscaledDeltaTime * 1.5f);
            }

            // Camera Visibility culling
            if (DisableIfInvisible != null)
            {
                bool allInvisible = true;

                if (DisableIfInvisible.isVisible)
                {
                    allInvisible = false;

                    for (int i = 0; i < DisableIfInvisibleExtraRenderers.Count; i++)
                    {
                        if (DisableIfInvisibleExtraRenderers[i].isVisible) { allInvisible = false; break; }
                    }
                }

                if (allInvisible)
                {
                    legsWasDisabled = true;
                    return;
                }
            }

            #region Backquote input debug disable plugin

#if UNITY_EDITOR
#if ENABLE_LEGACY_INPUT_MANAGER
            // Just for editor debug help
            if (Input.GetKey(KeyCode.BackQuote))
            {
                legsWasDisabled = true;
                return;
            }
#endif
#endif

            #endregion Backquote input debug disable plugin

            // Reactivating procedures
            if (legsWasDisabled)
            {
                if (_MainBlendNoRagdolling > 0f)
                    if (LegsInitialized)
                    {
                        OnLegsReactivate();
                        legsWasDisabled = false;
                    }
            }
        }

        private void Update()
        {
            if (LegsInitialized == false) return;

            CheckActivation();

            float dt = UnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
            PrepareValues(dt);

            if (!AllowUpdate) return;
            if (AnimatePhysics) return;
            if (legsWasDisabled) return;

            UpdateStack(dt);
        }

        private bool _fixedUpdated = false;

        private void FixedUpdate()
        {
            if (!AllowUpdate) return;
            if (legsWasDisabled) return;

            float dt = UnscaledDeltaTime ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime;
            PrepareValues(dt);
            ExtraFixedUpdate();

            if (AnimatePhysics == false) return;

            UpdateStack(dt);
            _fixedUpdated = true;
        }

        /// <summary> Called even when animate physics is disabled </summary>
        protected virtual void ExtraFixedUpdate()
        {
            if (!UseRigidbodyVelocityForIsMoving) return;
            if (!Rigidbody) return;

            Vector3 localVelo = ToRootLocalSpaceVec(Rigidbody.velocity);
            localVelo.y = 0f;
            bool moving = (localVelo.magnitude) > ScaleReferenceNoScale * 0.02f;
            User_SetIsMoving(moving);
        }

        private void LateUpdate()
        {
            if (!AllowUpdate) return;
            if (legsWasDisabled) return;

            #region Animate Physics support

            if (AnimatePhysics)
            {
                if (!_fixedUpdated) return;
                else
                {
                    _fixedUpdated = false;
                }
            }

            #endregion Animate Physics support

            PreLateUpdateStack();
            LateUpdateStack();
        }

        /// <summary> Prepare target blend value for the component's algorithms </summary>
        protected virtual void PrepareValues(float delta)
        {
            _MainBlendNoRagdolling = LegsAnimatorBlend * cullingBlend * protectedBlend;
            _MainBlend = _MainBlendNoRagdolling * RagdolledDisablerBlend;
            _MainBlendPlusGrounded = _MainBlend * IsGroundedBlend;
            if (Mecanim != null) AnimatePhysics = Mecanim.updateMode == AnimatorUpdateMode.AnimatePhysics;

            if (_lastMainBlend != _MainBlend)
            {
                _lastMainBlendChanged = true;
                for (int l = 0; l < Legs.Count; l++) Legs[l].IK_UpdateParamsBase();
            }
            else { _lastMainBlendChanged = false; }

            _lastMainBlend = _MainBlend;
        }

        protected virtual void UpdateStack(float delta)
        {
            #region Editor Debug Performance Measure Start

            MeasurePerformanceUpdate(true);

            #endregion Editor Debug Performance Measure Start


            DeltaTime = delta;
            Scale = baseTransform.lossyScale.y;

            if (_MainBlend > 0f)
            {
                if (_wasInstantTriggered == false) IK_TriggerGlueInstantTransition();

                RefreshTargetMovementDirectionHelper();
                //RefreshMatrices();

                Controll_Update();
                UpdateGroundedBlend();
                UpdateMovingBlend();
                UpdateSlidingBlend();

                Hips_PreCalibrate();
                Legs_PreCalibrate();

                ExtraControls_Update();

                Modules_Update();
            }
            else
            {
                Controll_Update();
                Legs_PreCalibrate();
                legsWasDisabled = true;
            }

            #region Editor Debug Performance Measure End

            MeasurePerformanceUpdate(false);

            #endregion Editor Debug Performance Measure End
        }

        protected virtual void PreLateUpdateStack()
        {
            #region Editor Debug Performance Measure Start

            MeasurePerformancePreLateUpdate(true);

            #endregion Editor Debug Performance Measure Start

            RefreshMatrices();

            Legs_CheckAnimatorPose();
            Modules_AfterAnimatorCaptureUpdate();
            BaseObject_MotionUpdate();

            Hips_PreLateUpdate();
            Hips_LateUpdate();

            Legs_BeginLateUpdate();
            //StepHeatmap_Update();

            #region Editor Debug Performance Measure End

            MeasurePerformancePreLateUpdate(false);

            #endregion Editor Debug Performance Measure End
        }

        protected virtual void LateUpdateStack()
        {
            #region Editor Debug Performance Measure Start

            MeasurePerformanceMain(true);

            #endregion Editor Debug Performance Measure Start

            if (_MainBlend > 0.001f) Legs_MidLateUpdateAndRaycasting();

            Modules_PreLateUpdate(); //
            Legs_LateUpdate();
            Hips_PostLateUpdate();

            Modules_LateUpdatePreApply(); //
            Legs_LateUpdate_Apply();
            Modules_PostLateUpdate(); //

            #region Editor Debug Performance Measure End

            MeasurePerformanceMain(false);

            #endregion Editor Debug Performance Measure End
        }

        public virtual void User_UpdateParametersAfterManualChange()
        {
            #region Editor ifdef

#if UNITY_EDITOR
            if (Application.isPlaying == false) return;
#endif

            #endregion Editor ifdef

            if (AllowUpdate == false) return;

            #region IK Rotate Support Part I

            //_ExecutionIKOffsetsRotate = IKOffsetsRotate;

            //if (IKOffsetsRotate != 0f)
            //{
            //    if (usingIKRotate == false) usingIKRotate = true;
            //}

            #endregion IK Rotate Support Part I

            Modules_UpdateAfterManualChanges();

            #region IK Rotate Support Part II

            //if (usingIKRotate)
            //{
            //    //if (useCustomIKRotatorVector == false)
            //    IK_UseIKRotatorQuat = Quaternion.Euler(0f, _ExecutionIKOffsetsRotate, 0f);
            //}

            #endregion IK Rotate Support Part II

            for (int l = 0; l < Legs.Count; l++)
            {
                var leg = Legs[l];
                leg.Leg_UpdateParams();
            }

            if (Event_OnStep.GetPersistentEventCount() > 0 || StepInfoReceiver)
            {
                UseEvents = true;
            }
            else
            {
                UseEvents = false;
            }
        }

        /// <summary> Use if you're adding legs animator runtime </summary>
        public void OnAddedReset()
        {
            MotionInfluence = new MotionInfluenceProcessor();
            MotionInfluence.AxisMotionInfluence.x = 0f;

            BaseLegAnimating = new LegStepAnimatingParameters();
            LegAnimatingSettings.RefreshDefaultCurves();

            CustomModules = new System.Collections.Generic.List<LegsAnimatorCustomModuleHelper>();
        }

        #region Editor Code (void Reset())

#if UNITY_EDITOR

        [HideInInspector] public LegsAnimatorControlModuleBase _Editor_DefaultModuleOnStart;
        [HideInInspector] public LegsAnimatorControlModuleBase _Editor_LegHelperModule;

        protected virtual void Reset()
        {
            OnAddedReset();

            var helper = new LegsAnimatorCustomModuleHelper(this);
            helper.ModuleReference = _Editor_DefaultModuleOnStart;
            if (helper.ModuleReference == null) return;
            CustomModules.Add(helper);
        }

#endif

        #endregion Editor Code (void Reset())
    }
}