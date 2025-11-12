using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public List<Leg> Legs = new List<Leg>();

        public enum ELegSide { Undefined, Left, Right }


        [System.Serializable]
        public partial class Leg
        {
            public LegsAnimator Owner;
            public int PlaymodeIndex { get; private set; }

            [FPD_Suffix(0f, 1f)]
            public float LegBlendWeight = 1f;

            #region Blend variables

            /// <summary> Multiplier for modules </summary>
            internal float InternalModuleBlendWeight = 1f;

            /// <summary> (precomputed on preCalibrate) LegBlendWeight * InternalModuleBlendWeight </summary>
            public float BlendWeight { get; private set; }

            /// <summary> (precomputed on preCalibrate) Inidividual leg blend + legs animator blend </summary>
            float finalBoneBlend = 1f;

            #endregion


            #region Leg Individual Settings

            [Tooltip("Make idle glue animation motion faster for this single leg")]
            public float LegMoveSpeedMultiplier = 1f;
            public float LegRaiseMultiplier = 1f;

            [Space(3)]
            public float GlueThresholdMultiplier = 1f;
            public Vector2 GluePointOffset = Vector2.zero;

            [Space(3)]
            [Range(0f, 1f)] public float LegStretchMultiplier = 1f;
            [Tooltip("Motion preset for the leg to be animated with different character than the other legs ('Idle Glue Motion' settings)")]
            public LegMotionSettingsPreset CustomLegAnimating;

            [Range(-40f, 40f)]
            public float FootPitchOffset = 0f;

            #endregion


            public Transform BoneStart;
            public Transform BoneMid;
            public Transform BoneEnd;


            public ELegSide Side = ELegSide.Undefined;
            public int OppositeLegIndex = -1;
            public ERaycastPrecision RaycastPrecision = ERaycastPrecision.Linecast;


            [Tooltip("(Experimental) If you want to animate in additional feet bone which in some cases can add nice animation feeling")]
            public bool UseFeet = false;
            public Transform BoneFeet;
            [Range(0f, 1f)]
            [Tooltip("Defining how quick heel should get up if leg gets stretched (change max stretching param under IK tab to be lower value that 1.1)")]
            public float FeetSensitivity = 0.5f;

            /// <summary> To avoid using for() loops but while() for better performance (Only Playmode) </summary>
            [field:NonSerialized] public Leg NextLeg { get; private set; }
            public void SetNextLeg(Leg next) { NextLeg = next; }
            public void RefreshPlaymodeIndex(int idx) { PlaymodeIndex = idx; }

            [field: NonSerialized] public HipsReference ParentHub { get; private set; }
            // Unity throws serialization depth limit warning when it's using {get; private set;} ¯\_(ツ)_/¯ 
            // Finally! [field:NonSerialized] seems to fix it!!!
            //public HipsReference ParentHub { get; private set; }

            bool hasOppositeleg = false;

            private LegStepAnimatingParameters targetLegAnimating;
            public LegStepAnimatingParameters LegAnimatingSettings { get { return targetLegAnimating; } }


            public float LegStretchLimit { get; private set; } = 1f;

            public void InitLegBasics(LegsAnimator creator, int index, Leg nextLeg)
            {
                if (creator != null) Owner = creator;
                PlaymodeIndex = index;
                SetNextLeg(nextLeg);
                LegStretchLimit = 1f;
                BlendWeight = 1f;
                InternalModuleBlendWeight = 1f;

                EnsureAxesNormalization();

                #region Initialize Bone Helpers

                _h_boneStart = new LegHelper(this, BoneStart);
                _h_boneMid = new LegHelper(this, BoneMid);
                _h_boneEnd = new LegHelper(this, BoneEnd);
                _h_boneStart.Child = _h_boneMid;
                _h_boneMid.Child = _h_boneEnd;

                #endregion

                Gluing_Init();
                Reset();

                Controll_Init();
                Raycasting_Init();
                Stability_Init();
                AlignStep_Init();

                RefreshHasOppositeLeg();

                targetLegAnimating = CustomLegAnimating ? CustomLegAnimating.Settings : creator.LegAnimatingSettings;
                ankleAlignedOnGroundHitWorldPos = _FinalIKPos; //

            }

            public void RefreshHasOppositeLeg()
            {
                hasOppositeleg = false;
                if (GetOppositeLeg() != null) hasOppositeleg = true;
            }

            public void Leg_UpdateParams()
            {
                targetLegAnimating = CustomLegAnimating ? CustomLegAnimating.Settings : Owner.LegAnimatingSettings;
                IK_UpdateParams();
            }

            internal void AssignParentHub(HipsReference hipsReference)
            {
                ParentHub = hipsReference;
            }

            internal void Reset()
            {
                _SourceIKPos = BoneEnd.position;
                _SourceIKRot = BoneEnd.rotation;
                _FinalIKPos = _SourceIKPos;
                _FinalIKRot = _SourceIKRot;
                _PreviousFinalIKPos = _FinalIKPos;
                _PreviousFinalIKRot = _FinalIKRot;

                legGroundHit = new RaycastHit();
                legGroundHit.point = _FinalIKPos;
                legGroundHit.normal = Owner.Up;

                _PreviousFinalIKPosForStability = _SourceIKPos;
                ankleAlignedOnGroundHitRotation = _SourceIKRot;
                A_LastApppliedAlignRot = _SourceIKRot;
                A_LastTargetAlignRot = _SourceIKRot;

                groundHitRootSpacePos = ToRootLocalSpace(_SourceIKPos);
                _SourceIKPosUnchangedY = groundHitRootSpacePos;

                RaycastHit ghostHit = new RaycastHit();
                ghostHit.point = _FinalIKPos;
                ghostHit.normal = Owner.Up;
                legGroundHit = ghostHit;

                Glue_Reset(true);
                //Gluing_Init();
                //Raycasting_Init();
                //AlignStep_Init();
                //PreCalibrate();
            }


            #region Update Executing

            public void PreCalibrate()
            {

                #region Editor ifdef - reset label
#if UNITY_EDITOR
                _Editor_Label = "";
#endif                
                #endregion

                BlendWeight = BlendWeight * InternalModuleBlendWeight;
                finalBoneBlend = BlendWeight * Owner._MainBlend;

                if (finalBoneBlend < 0.0001f)
                {
                    if (_G_WasDisabled == false)
                    {
                        G_Attached = false;
                        G_AttachementHandler.Reset(false);
                        G_Attachement = new GlueAttachement();
                        _G_WasDisabled = true;
                        legGroundHit = new RaycastHit();
                        RaycastHitted = false;
                    }

                    return;
                }

                if( Owner.Calibrate == ECalibrateMode.Calibrate )
                {
                    IKProcessor.PreCalibrate();
                }
                else if( Owner.Calibrate == ECalibrateMode.FixedCalibrate )
                {
                    if( !_wasFixedCalibrateAnimationCaptured ) IKProcessor.PreCalibrate();
                    else
                    {
                        BoneStart.localRotation = _AnimatorStartBoneLocRot;
                        BoneMid.localRotation = _AnimatorMidBoneLocRot;
                        BoneEnd.localRotation = _AnimatorEndBoneLocRot;
                    }
                }

                //G_CustomForceNOTDetach = false;
                //G_CustomForceDetach = false;
                //G_CustomForceNOTAttach = false;
            }

            public void CheckAnimatorPose()
            {
                _AnimatorStartBonePos = BoneStart.position;
                _AnimatorMidBonePos = BoneMid.position;
                _AnimatorEndBonePos = BoneEnd.position;

                if( Owner.Calibrate == ECalibrateMode.FixedCalibrate )
                {
                    _wasFixedCalibrateAnimationCaptured = true;
                    _AnimatorStartBoneLocRot = BoneStart.localRotation;
                    _AnimatorMidBoneLocRot = BoneMid.localRotation;
                    _AnimatorEndBoneLocRot = BoneEnd.localRotation;
                }
            }

            public void BeginLateUpdate()
            {
                if (finalBoneBlend < 0.0001f) return;

                //G_StepHeatmapForceDetach = false;
                //G_StepHeatmapForceNOTDetach = false;
                G_CustomForceAttach = false;
                G_CustomForceNOTDetach = false;
                G_CustomForceDetach = false;
                G_CustomForceNOTAttach = false;

                IK_PreUpdate();
                LegStretchLimit = Owner.LimitLegStretch * LegStretchMultiplier;
            }

            public void PreLateUpdate()
            {
                if (customOverwritingIKPos) return;
                if (_G_WasDisabled && finalBoneBlend < 0.0001f) return;
                Owner.Modules_LegBeforeRaycastingUpdate(this);
                Raycasting_PreLateUpdate();
                Controll_Calibrate();
            }

            public void LateUpdate()
            {
                if (finalBoneBlend < 0.0001f) return;
                if (customOverwritingIKPos) return;
                Owner.Modules_Leg_LateUpdate(this);

                AlignStep_CheckAlignStatePre();
                AlignStep_ValidateFootRotation();
                //if (Owner._usingStepHeatmap) Owner._stepHeatmap.UpdatePreGlue(PlaymodeIndex);
                Gluing_Update();
                Gluing_ApplyCoords();

                AlignStep_OnGroundAlign();
                AlignStep_SmoothSuddenSteps();
                AlignStep_LegElevation();
                AlignStep_Complete();

                Control_StepEventCalcs();

                ExtraProcessingApply();

            }

            public void LateUpdate_Apply()
            {
                IK_PostUpdate();
            }

            public void FixedUpdate()
            {

            }

            #endregion


            #region Calculation Helpers

            [Tooltip("Apply IK hint inversion, in case leg is bending in wrong direction.")]
            public bool InverseHint = false;

            /// <summary> Bone End's Local Space</summary>
            public Vector3 AnkleToHeel = Vector3.zero;
            /// <summary> Bone End's Local Space</summary>
            public Vector3 AnkleToFeetEnd = Vector3.zero;

            public Vector3 AnkleRight = Vector3.right;
            public Vector3 AnkleUp = Vector3.up;
            public Vector3 AnkleForward = Vector3.forward;

            [Range(0f, 1.001f)]
            public float FootMiddlePosition = 0.5f;

            [Space(5)]
            [FPD_Suffix(-45f, 45f, FPD_SuffixAttribute.SuffixMode.FromMinToMax, "°")] public float AnkleYawCorrection = 0f;

            #endregion


            #region Utilities

            /// <summary> Current frame leg limb world length in units </summary>
            public float LegLimbLength()
            {
                if (BoneStart == null || BoneMid == null || BoneEnd == null) return Owner.HipsToGroundDistance();
                float len = 0f;
                len += Vector3.Distance(BoneStart.position, BoneMid.position);
                len += Vector3.Distance(BoneEnd.position, BoneMid.position);
                return len;
            }

            public bool HasAllBonesSet()
            {
                if (BoneStart == null) return false;
                if (BoneMid == null) return false;
                if (BoneEnd == null) return false;
                return true;
            }

            public float R(float toRound, int digits = 2)
            {
                return (float)System.Math.Round(toRound, digits);
            }

            #endregion


            bool _StepSent = true;
            float _StepSentAt = -100f;
            float _RaiseSentAt = -100f;
            bool _OppositeLegStepped = true;
            float _ToConfirmStepEvent = 0f;

            void SendStepEvent(float factor = 1f, EStepType type = EStepType.IdleGluing)
            {
                if (_StepSent) return;
                //if (Time.unscaledTime - _StepSentAt < 0.05f) return;

                if (Owner.GroundedTime < 0.1f) type = EStepType.OnLanding;
                else if (Owner.IsMoving == false) if (Owner.StoppedTime < 0.15f) type = EStepType.OnStopping;
                Owner.Events_OnStep(this, factor, type);
                _StepSent = true;
                _StepSentAt = Time.unscaledTime;

                if (hasOppositeleg)
                {
                    _OppositeLegStepped = true;
                    GetOppositeLeg()._OppositeLegStepped = !Owner.IsMoving;
                }
            }

            void SendRaiseEvent( float distanceToNew = 1f)
            {
                if (Time.unscaledTime - _RaiseSentAt < 0.05f) return;
                _RaiseSentAt = Time.unscaledTime;

                EStepType type = EStepType.IdleGluing;
                if( Owner.IsMoving == false ) if( Owner.StoppedTime < 0.15f ) type = EStepType.OnStopping;
                Owner.Events_OnRaise( this, distanceToNew, type );
            }

            void StepEventRestore()
            {
                if (!Owner.UseEvents) return;
                if (!_StepSent) return;

                if (Time.unscaledTime - _StepSentAt < 0.1f) return;
                if (Owner.GroundedTime < 0.1f) return;
                //if (!Owner.StepEventOnLanding) if (Owner.IsGroundedBlend < 0.9f) return;

                if (Owner.UseGluing)
                {
                    if (G_AttachementHandler.glueAnimationBlend > 0.5f && G_GlueInternalTransition > 0.25f) return;

                    if (Owner._glueModeExecuted == EGlueMode.Idle)
                    {
                        if (G_DuringAttaching == false) return;

                        if (Owner.GlueMode == EGlueMode.Automatic)
                        {
                            if (Owner.IsMoving) return;
                            if (Owner.Helper_WasMoving) return;
                        }

                        if (Owner.SendOnStopping == false && Owner.StoppedTime < 0.155f) return;
                        if (G_AttachementHandler.lasGlueModeOnAttaching != EGlueMode.Idle) return;
                        if (G_AttachementHandler.legMoveDistanceFactor < 0.05f) return;
                    }
                    else
                    {
                        if (Owner.GlueMode == EGlueMode.Automatic) if (!Owner.IsMoving) return;
                        if (Owner.MovingTime < 0.06f) return;
                        if (A_PreWasAligning) return;
                        if (A_AligningHelperBlend > .5f - Owner.EventExecuteSooner) return;


                        if (hasOppositeleg)
                        {
                            if (GetOppositeLeg()._OppositeLegStepped == false) return;
                        }

                        float heightFactor = -ScaleRef * 0.2f + FloorLevel * Owner.BaseTransform.lossyScale.y + C_AnkleToHeelWorldHeight * 0.75f + A_LastAlignHeightCompareValue * (3f + Owner.EventExecuteSooner);

                        if (A_LastAlignHeightDiff < heightFactor)
                        {
                            return;
                        }
                    }
                }
                else // Not gluing - aligning based event triggering
                {
                    if (A_PreWasAligning) return;
                    if (A_AligningHelperBlend > 0.05f) { return; }

                    if (Owner.IsMovingBlend < 0.05f) { _StepSent = true; return; }
                    if (Owner.Helper_WasMoving == false) { _StepSent = true; return; }
                    if (Owner.IsMoving == false) { _StepSent = true; return; }
                }

                _StepSent = false;
            }

        }


        public void Legs_AddNewLeg()
        {
            Leg leg = new Leg();
            leg.Owner = this;
            Legs.Add(leg);
        }

        public void Legs_RefreshLegsOwner()
        {
            for (int i = 0; i < Legs.Count; i++)
            {
                Legs[i].Owner = this;
            }
        }

    }
}