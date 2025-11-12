using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {

        [Tooltip("When not assigned, component will use this transform as 'Base Transform', but if your movement controller core is located in different transform, assign it here to be fully synchronized.")]
        [SerializeField] private Transform baseTransform;
        [Tooltip("The anchor bone for all other limbs.\n! It needs to be parent of Leg Bones !")]
        public Transform Hips;

        /// <summary> Lossy scale at legs animator start </summary>
        Vector3 InitialBaseScale = Vector3.one;


        /// <summary> Lossy scale but adjusted with initial object scale </summary>
        public float DynamicYScale
        {
            get
            {
                return BaseTransform.lossyScale.y / InitialBaseScale.y;
            }
        }


        #region Scale Reference Stuff


        /// <summary> Helper distance (world scale) defining average half-length of the leg </summary>
        public float ScaleReference { get { return finalScaleReference * BaseTransform.lossyScale.x; } }
        public float ScaleReferenceNoScale { get { return finalScaleReference; } }
        /// <summary> No Scale! </summary>
        public float ScaleReferenceSqrt
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                    return Mathf.Sqrt(finalScaleReference);
                else
                    return finalScaleReferenceSqrt;
#else
                return finalScaleReferenceSqrt;
#endif
            }
        }

        public enum ELegsScaleReference { PelvisToGroundDistance, FirstLegLength, Custom, PelvisLegAverage }

        [Tooltip("Define helper value for the algorithm, to define raycasting distance - scale for the algorithms to animate model in the correct way.\n\nCheck scene gizmos to adjust this value.")]
        public ELegsScaleReference ScaleReferenceMode = ELegsScaleReference.PelvisLegAverage;

        /// <summary> Final scale reference is local space </summary>
        [SerializeField] private float finalScaleReference = 0.5f;
        /// <summary> Final scale reference is local space </summary>
        [SerializeField] private float finalScaleReferenceSqrt = 0.1f;
        /// <summary> Custom scale reference is local space </summary>
        [SerializeField] private float customScaleReferenceValue = 0.5f;


        #endregion


        [Tooltip("Do component init after few frames of the game (can be useful when waiting for some of the components to be generated, or to initialize component not during T-pose)")]
        public bool DelayedInitialization = false;

        [Tooltip( "Hard refresh bones on update: it's required when any of procedurally animated bones is not handled by keyframe animation.\nIf you're sure, your animations are always keyframe animated, you can disable this feature for small performance boost." )]
        public ECalibrateMode Calibrate = ECalibrateMode.Calibrate;
        public enum ECalibrateMode : int
        {
            [Tooltip( "No Extra overhead" )]
            None = 0,
            [Tooltip( "Resetting bones local rotations to the initialized state" )]
            Calibrate = 1,
            [Tooltip( "Resetting bones local rotations to the captured animator state (can fix trigger colliders detection when colliders added on legs)" )]
            FixedCalibrate = 2
        }

        [Tooltip("If your Unity Animator is using 'Animate Physics' update mode, you should enable this parameter.")]
        public bool AnimatePhysics = false;

        [Tooltip("If time.scale should or shouldn't affect legs animation")]
        public bool UnscaledDeltaTime = false;


        [Tooltip("Disable Legs Animator calculations when this renderer is not seen by any camera (including scene view camera!)")]
        public Renderer DisableIfInvisible = null;
        public List<Renderer> DisableIfInvisibleExtraRenderers = new List<Renderer>();

        [Tooltip("Smoothly fade out Legs Animator when far from the camera")]
        public float FadeOffAtDistance = 0f;

        /// <summary> If none then automatically gets main camera </summary>
        [NonSerialized] public Transform FadeOff_DistanceToCamera = null;
        public float FadeOff_lastCameraDistance { get; protected set; }


        /// <summary> World up reference for the algorithm </summary>
        public Vector3 Up { get { return _worldUpAxisVector; } set { _worldUpAxisVector = value; } }
        private Vector3 _worldUpAxisVector = Vector3.up;

        /// <summary>Time how long character stands on the ground, when value is below zero that's the time being ungrounded </summary>
        public float GroundedTime { get; private set; }
        /// <summary>Time how long character is during is moving = true, when value is below zero that's the time being stopped </summary>
        public float InAirTime { get { return -GroundedTime; } }
        public float MovingTime { get; private set; }
        public float StoppedTime { get { return -MovingTime; } }

        /// <summary>
        /// Do it when your model is changing scale.
        /// Updating scale reference and others.
        /// </summary>
        public void User_RefreshHelperVariablesOnParametersChange()
        {
            if (IsSetupValid() == false) return;

            #region Scale Reference Definition

            if (ScaleReferenceMode == ELegsScaleReference.PelvisToGroundDistance)
            {
                finalScaleReference = HipsToGroundDistanceLocal();
            }
            else if (ScaleReferenceMode == ELegsScaleReference.FirstLegLength)
            {
                finalScaleReference = Legs[0].LegLimbLength() / Mathf.Max(0.001f, BaseTransform.lossyScale.x);
            }
            else if (ScaleReferenceMode == ELegsScaleReference.Custom)
            {
                if (customScaleReferenceValue < 0f) customScaleReferenceValue = 0.2f;
                finalScaleReference = customScaleReferenceValue;
            }
            else if (ScaleReferenceMode == ELegsScaleReference.PelvisLegAverage)
            {
                finalScaleReference = HipsToGroundDistanceLocal();
                finalScaleReference = Mathf.LerpUnclamped(finalScaleReference, Legs[0].LegLimbLength() / Mathf.Max(0.001f, BaseTransform.lossyScale.x), 0.5f);
            }

            #endregion

        }


        /// <summary> Never returns null </summary>
        public Transform BaseTransform
        {
            #region Conditional return baseTransform (on build only baseTransform)
            get
            {
#if UNITY_EDITOR
                if (!LegsInitialized) if (baseTransform == null) return transform; else return baseTransform;
                if (Application.isPlaying) return baseTransform;
                if (baseTransform == null) return transform; else return baseTransform;
#else
                return baseTransform; 
#endif
            }
            #endregion
        }


        protected void Legs_PreCalibrate()
        {
            var leg = Legs[0];
            while (leg != null)
            {
                leg.PreCalibrate();
                leg = leg.NextLeg;
            }
        }
        void Legs_CheckAnimatorPose()
        {
            var leg = Legs[0];
            while (leg != null)
            {
                leg.CheckAnimatorPose();
                leg = leg.NextLeg;
            }
        }

        void Legs_BeginLateUpdate()
        {
            var leg = Legs[0];
            while (leg != null)
            {
                leg.BeginLateUpdate();
                leg = leg.NextLeg;
            }
        }

        void Legs_LateUpdate()
        {
            var leg = Legs[0];
            while (leg != null)
            {
                leg.LateUpdate();
                leg = leg.NextLeg;
            }
        }

        void Legs_LateUpdate_Apply()
        {
            var leg = Legs[0];
            while (leg != null)
            {
                leg.LateUpdate_Apply();
                leg = leg.NextLeg;
            }

            if ( UseCustomIK) CustomIK_ApplyIK();
        }

    }

}