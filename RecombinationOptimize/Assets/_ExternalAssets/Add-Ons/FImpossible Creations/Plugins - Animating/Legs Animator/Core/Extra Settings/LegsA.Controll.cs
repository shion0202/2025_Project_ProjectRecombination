using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        [Tooltip("Optional reference to unity's Animator. Legs Animator can use animator's variables to read state of your character movement, like IsGrounded or IsMoving + other extra helpers for custom modules and special calculations.")]
        public Animator Mecanim;

        [Tooltip("Animator parameter to read value for auto-define grounded state of the character (you can use LegAnimator.User_SetIsGrounded() through code instead)")]
        public string GroundedParameter = "";
        [Tooltip("Animator parameter (bool or float - Bool recommended for quicker Not-Moving reaction) to read value for auto-define movement state of the character (you can use LegAnimator.User_SetIsMoving() through code instead)")]
        public string MovingParameter = "";


        private int _hash_Grounded = -1;
        private int _hash_Moving = -1;
        private bool _hash_MovingIsFloat = false;
        [Range(0f,0.5f)]
        [HideInInspector] public float User_IsMovingMecanim_NotMovingFloat_Threshold = 0.1f;
        private int _hash_Sliding = -1;

        [Tooltip("Optional Rigidbody which is used for few helper calculations. If rigidbody is assigned, then rigidbody velocity will drive 'Desired Move Direction' value (! only if .IsMoving is true !), unless you use 'User_SetDesiredMovementDirection'")]
        public Rigidbody Rigidbody;
        [Tooltip("Use rigidboy velocity state to automatically drive Leg Animator's helper 'IsMoving' flag")]
        public bool UseRigidbodyVelocityForIsMoving = false;
        //[Tooltip("Use rigidboy velocity to automatically drive Leg Animator's helper 'Desired Direction' value")]
        //public bool UseRigidbodyVelocityForDesiredVelocity = false;
        [Tooltip("Use leg raycasts to automatically define Leg Animator's helper 'IsGrounded' flag")]
        public bool UseRaycastsForIsGrounded = false;


        [Tooltip("Animator parameter to read value for auto-define sliding state of the character - auto fading off gluing (you can use LegAnimator.User_SetIsSliding() through code instead)")]
        public string SlidingParameter = "";

        [Tooltip("Optional bone for modules if needed")]
        public Transform SpineBone;
        [Tooltip("Optional bone for modules if needed")]
        public Transform ChestBone;

        /// <summary> (world space move direction)\n(When rigidbody is assigned, rigidbody velocity is used here)\n Control value which helps animation based gluing detection </summary>
        public Vector3 DesiredMovementDirection { get; private set; }
        public bool usingCustomDesiredMovementDirection { get; private set; }

        CalibrateTransform _spineBoneCalibrate;
        CalibrateTransform _ChestBoneCalibrate;


        #region Ragdolled State Switch Implementation


        [Tooltip("Animator parameter to read value for auto-define calculations state of the character. The ragdolled parameter is disabling legs, and other algorithms which can conflict with physical animations. (you can use LegAnimator.User_SetIsRagdolled() through code instead)")]
        public string RagdolledParameter = "";
        private int _hash_Ragdolled = -1;
        public bool IsRagdolled { get { return _ragdolled; } }
        bool _ragdolled = false;

        /// <summary> Negative for Non-Ragdolled elapsed time </summary>
        public float RagdolledTime { get; private set; }
        [NonSerialized] public float MinNonRagdolledForBlendOut = 0f;

        public void User_SetIsRagdolled(bool isRagdolled)
        {
            if (isRagdolled != _ragdolled)
            {

                if (_ragdolled)
                {
                    OnLegsReactivate();
                }

                _ragdolled = isRagdolled;
            }
        }

        #endregion



        public void User_SetDesiredMovementDirection(Vector3 worldDirection)
        {
            usingCustomDesiredMovementDirection = true;
            DesiredMovementDirection = worldDirection;
        }

        public void User_SetDesiredMovementDirection(Vector3 worldDirection, bool removeYspeed)
        {
            if (removeYspeed)
            {
                worldDirection = ToRootLocalSpaceVec(worldDirection);
                worldDirection.y = 0f;
                worldDirection = RootToWorldSpaceVec(worldDirection);
            }

            User_SetDesiredMovementDirection(worldDirection);
        }

        public void User_SetDesiredMovementDirectionOff()
        {
            usingCustomDesiredMovementDirection = false;
        }

        /// <summary>
        /// Requires assigned 'Mecanim' parameter to calculate mecanim properties hashes
        /// </summary>
        public virtual void Controll_DefineHashes()
        {
            if (Mecanim == null) return;

            if( string.IsNullOrWhiteSpace( GroundedParameter ) == false ) _hash_Grounded = Animator.StringToHash( GroundedParameter ); else _hash_Grounded = -1;

            if( string.IsNullOrWhiteSpace( MovingParameter ) == false )
            {
                _hash_Moving = Animator.StringToHash( MovingParameter );

                if( Mecanim.runtimeAnimatorController != null )
                {
                    for( int i = 0; i < Mecanim.parameterCount; i++ )
                        if( Mecanim.GetParameter( i ).nameHash == _hash_Moving )
                            if( Mecanim.GetParameter( i ).type == AnimatorControllerParameterType.Float )
                            {
                                _hash_MovingIsFloat = true;
                                break;
                            }
                }
            }
            else _hash_Moving = -1;

            if( string.IsNullOrWhiteSpace( SlidingParameter ) == false ) _hash_Sliding = Animator.StringToHash( SlidingParameter ); else _hash_Sliding = -1;
            if( string.IsNullOrWhiteSpace( RagdolledParameter ) == false ) _hash_Ragdolled = Animator.StringToHash( RagdolledParameter ); else _hash_Ragdolled = -1;
        }

        public bool Helper_WasMoving { get; private set; }
        protected virtual void Controll_Update()
        {

            #region Ragdolled Blending Switch Support

            if (_hash_Ragdolled != -1)
            {
                User_SetIsRagdolled(Mecanim.GetBool(_hash_Ragdolled));
            }

            if (IsRagdolled)
            {
                if (RagdolledTime < 0f) RagdolledTime = 0f; RagdolledTime += DeltaTime;
                RagdolledDisablerBlend = Mathf.MoveTowards(RagdolledDisablerBlend, 0f, DeltaTime * 6f);
                UpdateBeingRagdolled();
            }
            else
            {
                if (RagdolledTime > 0f) RagdolledTime = 0f; RagdolledTime -= DeltaTime;

                bool blendTo1 = false;
                if (MinNonRagdolledForBlendOut > 0f) { if (-RagdolledTime > MinNonRagdolledForBlendOut) blendTo1 = true; } else blendTo1 = true;

                if (blendTo1)
                {
                    float was = RagdolledDisablerBlend;
                    RagdolledDisablerBlend = Mathf.MoveTowards(RagdolledDisablerBlend, 1f, DeltaTime * 4f);
                    if (was != RagdolledDisablerBlend) UpdateBeingRagdolled();
                }
                else
                {
                    RagdolledDisablerBlend = Mathf.MoveTowards(RagdolledDisablerBlend, 0f, DeltaTime * 6f);
                }
            }

            #endregion



            if (IsMoving || IsMovingBlend > 0.5f) Helper_WasMoving = true; else Helper_WasMoving = false;

            if (_hash_Grounded != -1) User_SetIsGrounded(Mecanim.GetBool(_hash_Grounded));

            if (_hash_Moving != -1)
            {
                if (_hash_MovingIsFloat)
                    User_SetIsMoving(Mecanim.GetFloat(_hash_Moving) > ScaleReference * User_IsMovingMecanim_NotMovingFloat_Threshold);
                else
                    User_SetIsMoving(Mecanim.GetBool(_hash_Moving));
            }

            if (_hash_Sliding != -1) User_SetIsSliding(Mecanim.GetBool(_hash_Sliding));

            if (GlueMode == EGlueMode.Automatic)
            {
                if (GroundedTime < 0.1f /*|| IsMovingBlend > 0.5f*/) _glueModeExecuted = EGlueMode.Moving;
                else
                {
                    if (IsMoving) _glueModeExecuted = EGlueMode.Moving;
                    else _glueModeExecuted = EGlueMode.Idle;
                }
            }
            else
            {
                _glueModeExecuted = GlueMode;
            }

            if (IsGrounded && GroundedTime < 0.2f) _glueModeExecuted = EGlueMode.Moving;

            //if (IsGrounded) GroundedTime += DeltaTime; else GroundedTime = -0.000001f;
            if (IsGrounded) { if (GroundedTime < 0f) GroundedTime = 0f; GroundedTime += DeltaTime; } else { if (GroundedTime > 0f) GroundedTime = 0f; GroundedTime -= DeltaTime; }
            if (IsMoving) { if (MovingTime < 0f) MovingTime = 0f; MovingTime += DeltaTime; } else { if (MovingTime > 0f) MovingTime = 0f; MovingTime -= DeltaTime; }

            if (GluingFloorLevelUseOnMoving)
                _glueingFloorLevel = Mathf.LerpUnclamped(GluingFloorLevel, GluingFloorLevelOnMoving, IsMovingBlend);
            else
                _glueingFloorLevel = GluingFloorLevel;

            if (UseStepPointsOverlapRadiusOnMoving)
                _stepPointsOverlapRadius = Mathf.LerpUnclamped(StepPointsOverlapRadius, StepPointsOverlapRadiusOnMoving, IsMovingBlend);
            else
                _stepPointsOverlapRadius = StepPointsOverlapRadius;
        }


        [Space(5)]
        [Tooltip("Calculating leg swing velocity in order to prevent gluing foot when swinging forward during movement forward (during forward swing, foot sometimes is touching ground which can result in gluing foot too soon, especially with ground level increased)\nWhen this value is high, foot will detect gluing less oftem.")]
        [Range(0f, 1f)]
        public float SwingHelper = 0.0f;
        [Tooltip("Local height value for the glue algorithm. You can try adjusting it's value during character movement and idling, to detect glue more effectively.")]
        public float GluingFloorLevel = 0.05f;

        public bool GluingFloorLevelUseOnMoving = false;
        public float GluingFloorLevelOnMoving = 0.0f;

        float _glueingFloorLevel = 0f;
        [Space(5)]
        [Tooltip("If you want to push out legs out of each other if their IK points are overlapping in one placement")]
        public float StepPointsOverlapRadius = 0.0f;
        public float _stepPointsOverlapRadius { get; private set; }
        public bool UseStepPointsOverlapRadiusOnMoving = false;
        public float StepPointsOverlapRadiusOnMoving = 0.0f;

        //[Tooltip("Local height value for the algorithms when character is in movement mode")]
        //public float FloorLevelOnMoving = 0.0125f;

        //[Space(5)]
        //public float AnimationFloorLevel = 0.05f;

        public MotionInfluenceProcessor MotionInfluence = new MotionInfluenceProcessor();

        /// <summary> Velocity and motion influence update </summary>
        void BaseObject_MotionUpdate()
        {
            MotionInfluence.Update();
        }

        void MotionInfluence_Init()
        {
            MotionInfluence.Init(BaseTransform);
        }

        protected virtual void Control_OnLand()
        {
            //if (ImpulseOnLanding != 0f)
            //{
            //    User_AddImpulse(new ImpulseExecutor(new Vector3(0f, -ImpulseOnLanding, 0f), 0.225f, .65f));
            //}
            if (StepEventOnLanding)
            {
                float lowestLegH = float.MaxValue;
                Leg lowestL = null;
                for (int i = 0; i < Legs.Count; i++)
                {
                    float localH = ToRootLocalSpace(Legs[i]._PreviousFinalIKPos).y;
                    if (localH < lowestLegH)
                    {
                        lowestLegH = localH;
                        lowestL = Legs[i];
                        Legs[i].StepEventSentInCustomWay();
                    }
                }

                if (lowestL != null)
                {
                    Events_OnStep(lowestL, 1f, EStepType.OnLanding);
                }
            }
        }

        protected virtual void Control_OnLooseGround()
        {
            //if (ImpulseOnInAirChange != 0f) User_AddImpulse(new ImpulseExecutor(new Vector3(0f, ImpulseOnInAirChange, 0f), 0.25f, 0.0f));
            // Modules
        }

        protected virtual void Control_OnStopMoving()
        {
            //if (ImpulseOnStop != 0f)
            //{
            //    User_AddImpulse(new ImpulseExecutor(new Vector3(0f, -ImpulseOnStop * 0.75f, ImpulseOnStop), 0.225f, 0.65f));
            //}
            // Modules
        }

        protected virtual void Control_OnStartMoving()
        {
            //if (ImpulseOnInAirChange != 0f) User_AddImpulse(new ImpulseExecutor(new Vector3(0f, ImpulseOnInAirChange, 0f), 0.25f, 0.0f));
            // Modules
        }

        private void RefreshTargetMovementDirectionHelper()
        {
            if (!usingCustomDesiredMovementDirection)
            {
                if (IsMoving == false)
                {
                    DesiredMovementDirection = Vector3.zero;
                }
                else
                {
                    if (Rigidbody)
                    {
                        if (Rigidbody.velocity.magnitude < ScaleReference * 0.1f)
                            DesiredMovementDirection = Vector3.zero;
                        else
                            DesiredMovementDirection = Rigidbody.velocity.normalized;
                    }
                }
            }
        }

        /// <summary>
        /// IK parameters needs to be upadted during ragdolled state for proper blend off
        /// </summary>
        private void UpdateBeingRagdolled()
        {
            for (int l = 0; l < Legs.Count; l++)
            {
                var leg = Legs[l];
                leg.Leg_UpdateParams();
            }
        }

        struct CalibrateTransform
        {
            public Transform Transform;
            private Quaternion initLocalRot;
            public CalibrateTransform(Transform t) { Transform = t; initLocalRot = t.localRotation; }
            public void Calibrate() { Transform.localRotation = initLocalRot; }
        }

    }

}