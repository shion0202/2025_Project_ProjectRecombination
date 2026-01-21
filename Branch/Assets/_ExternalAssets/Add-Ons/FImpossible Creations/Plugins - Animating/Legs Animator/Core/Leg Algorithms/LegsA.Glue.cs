using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            [NonSerialized] public bool G_InstantReglue = false;
            public bool G_Attached { get; private set; }
            public bool G_DuringAttaching { get { return G_LegAnimation.duringLegAdjustMovement; } }
            public bool G_FadingIn { get { return G_LegAnimation.duringLegAdjustMovement; } }

            /// <summary> Main glue blend for activation/deactivation </summary>
            float _glueTargetBlend = 1f;
            float _gluingCulldown = 0f;

            protected bool G_JustLanded = false;

            /// <summary> Can be used by custom modules </summary>
            [NonSerialized] public float ExtraGluingBlend = 1f;

            Vector3 _GlueLastAttachPosition;
            Vector3 _GlueLastAttachPositionRootLocal;
            Quaternion _GlueLastAttachRotation;
            Vector3 _GluePosition;
            Quaternion _GlueRotation;

            /// <summary> Temporary variable to switch some optional calculations on and off </summary>
            //int qualityLOD = 0;

            #region Reference Swing

            /// <summary> Variable for reference swing calculations </summary>
            Vector3 _G_LastPreGlueSourceLocalIKPos;
            /// <summary> Variable for reference swing calculations </summary>
            Vector3 _G_PreGlueSourceLocalIKPos;
            /// <summary> Variable for reference swing calculations </summary>
            Vector3 _G_sd_RefSwing = Vector3.zero;
            public Vector3 _G_RefernceSwing { get; private set; }

            #endregion

            bool _G_WasDisabled = true;

            public enum GlueReposeRequest { None, Repose, ReposeIfFar }
            [NonSerialized] public GlueReposeRequest G_RequestRepose = GlueReposeRequest.None;

            /// <summary> !Without object scale multiplier! </summary>
            float G_GlueTesholdRange { get { return Owner.ScaleReferenceNoScale * GlueThresholdMultiplier * Owner.GlueRangeThreshold * 0.5f; } }


            void Gluing_Init()
            {
                G_AttachementHandler = new GlueAttachementHandler(this);
                Glue_Reset(true);
            }


            public void Glue_Reset(bool initializing)
            {
                G_Attached = false;

                if (initializing)
                {
                    _GlueLastAttachPosition = BoneEnd.position;
                    _GlueLastAttachRotation = BoneEnd.rotation;
                    _GluePosition = BoneEnd.position;
                    _GlueLastAttachPositionRootLocal = ToRootLocalSpace(BoneEnd.position);
                    _G_LastPreGlueSourceLocalIKPos = _GlueLastAttachPosition;
                    _G_PreGlueSourceLocalIKPos = _SourceIKPos;
                    A_PreIKPosForGluing = BoneEnd.position;
                    _G_LasGroundedPosLocal = _GlueLastAttachPositionRootLocal;
                }

                var attach = new GlueAttachement();
                attach.PosInAttachementLocal = _FinalIKPos;
                attach.RotInAttachementLocal = _FinalIKRot;
                G_Attachement = attach;

                _G_RefernceSwing = Vector3.zero;
                _G_WasDisabled = true;

                G_AttachementHandler.Reset(initializing);
            }

            bool _G_WasGrounded = true;
            Vector3 _G_LasGroundedPosLocal;
            Quaternion _G_LasGroundedRotLocal;

            void Gluing_Update()
            {
                #region Gluing blending switch, deactivation, reactivation etc.

                _glueTargetBlend = Owner.GlueBlend * ExtraGluingBlend * Owner.RagdolledDisablerBlend * Owner.NotSlidingBlend;

                if (Owner.GlueOnlyOnIdle) _glueTargetBlend *= 1f - Owner.IsMovingBlend;

                if (Owner.IsGrounded)
                {
                    if (Owner.GroundedTime < 0.25f)
                    {
                        G_JustLanded = true;
                        _glueTargetBlend *= 0.1f + Mathf.InverseLerp(0f, 0.25f, Owner.GroundedTime) * 0.9f;
                    }
                    else
                        G_JustLanded = false;
                }
                else
                {
                    G_JustLanded = false;
                    _glueTargetBlend *= Owner.IsGroundedBlend;
                }

                if (_glueTargetBlend < 0.0001f)
                {
                    _glueTargetBlend = 0f;
                    _G_WasDisabled = true;
                    return;
                }
                
                if (_G_WasDisabled)
                {
                    Glue_Reset(false);
                    _G_WasDisabled = false;
                }

                if (_gluingCulldown > 0f) _gluingCulldown -= Owner.DeltaTime;

                #endregion

                #region Is Ungrounding Handling

                if (!Owner.IsGrounded)
                {
                    if (_G_WasGrounded)
                    {
                        _G_WasGrounded = false;
                        _G_LasGroundedPosLocal = ToRootLocalSpace(_GluePosition);
                        _G_LasGroundedRotLocal = _GlueRotation;
                        G_AttachementHandler.legMoveAnimation.Reset();
                    }

                    _GluePosition = RootSpaceToWorld(_G_LasGroundedPosLocal);
                    _GlueRotation = _G_LasGroundedRotLocal;
                    return;
                }

                _G_WasGrounded = true;

                #endregion


                #region Reference Swing Calculate

                if (Owner._glueModeExecuted == EGlueMode.Moving && Owner.SwingHelper > 0f)
                {
                    Vector3 swingVelo = AnkleH.LastKeyframeRootPos - _G_LastPreGlueSourceLocalIKPos;
                    if (swingVelo.magnitude > Owner.ScaleReferenceNoScale * 0.001f)
                    {
                        _G_LastPreGlueSourceLocalIKPos = _G_PreGlueSourceLocalIKPos;
                    }

                    _G_PreGlueSourceLocalIKPos = AnkleH.LastKeyframeRootPos;
                    _G_RefernceSwing = Vector3.SmoothDamp(_G_RefernceSwing, swingVelo * 2f, ref _G_sd_RefSwing, 0.04f, 100000f, Owner.DeltaTime);
                }
                else
                {
                    _G_RefernceSwing = Vector3.zero;
                    _G_sd_RefSwing = Vector3.zero;
                }

                #endregion


                #region Attaching / Detaching check

                _Glue_AskingForDetach = false;


                if (G_Attached) // Checking glue transition during being attached
                {
                    bool attach = !Glue_CheckDetachement();
                    if (attach == false) attach = !Glue_CheckIdleDetachementConfirm();

                    if (attach == false)
                    {
                        G_Attached = attach; // Update the attached leg state
                        G_AttachementHandler.OnLegRequireRepose();

                        // Check for re-attach
                        attach = Glue_Conditions_Attach();
                    }
                    else
                    {
                        // If can't attach anyway, dont do it
                        if (!Glue_Conditions_Attach()) attach = false;
                    }

                    if (attach) G_AttachementHandler.TransitionToGlueAnimation();
                    else G_AttachementHandler.TransitionToDisableGlueAnimation();

                    //G_DuringAttaching = attach;
                }
                else // Checking glue transition target conditions when not yet fully attached
                {
                    bool attach = Glue_Conditions_Attach();

                    if (attach)
                    {
                        G_AttachementHandler.TransitionToGlueAnimation();
                    }
                    else
                    {
                        //G_AttachementHandler.TriggerDetach();
                        G_AttachementHandler.TransitionToDisableGlueAnimation();
                    }

                    //G_DuringAttaching = attach;
                }


                #endregion


                if (G_InstantReglue)
                {
                    G_AttachementHandler.SheduleInstantTransition();
                    G_InstantReglue = false;
                }

                G_AttachementHandler.UpdateTransitioning(G_DuringAttaching);
                Gluing_UpdateAttachement();
            }


            bool Glue_TriggerFinalAttach()
            {
                if (legGroundHit.transform || _UsingEmptyRaycast)
                {
                    G_Attached = true;
                    G_Attachement = new GlueAttachement(this, legGroundHit);
                    return true;
                }

                return false;
            }

            /// <summary> During being attached </summary>
            void Gluing_UpdateAttachement()
            {
                if (G_Attachement.NoTransform == false && G_Attachement.AttachedTo == null)
                {
                    // Reset attachement on attached to destroy
                    G_Attachement = new GlueAttachement();
                    G_AttachementHandler.OnLegRequireRepose();
                    G_Attached = false;
                }

                if (G_Attached == false) // Transition towards attachement point
                {
                    _GluePosition = G_AttachementHandler.GetGluePosition();

                    Gluing_DragStretchApply();

                    if (Owner.AnimateFeet)
                    {
                        if (Owner.LimitFeetYaw > 0f)
                            //_GlueRotation = Quaternion.LerpUnclamped(A_LastApppliedAlignRot, G_AttachementHandler.GetGlueRotation(), 0.5f);
                            _GlueRotation = G_AttachementHandler.GetGlueRotation();
                        else
                            _GlueRotation = A_LastApppliedAlignRot;
                    }

                }
                else // Adjusting foot position in attachement space
                {
                    _GlueLastAttachPosition = G_Attachement.GetRelevantAlignedHitPoint(this);
                    _GlueLastAttachPositionRootLocal = ToRootLocalSpace(_GlueLastAttachPosition);

                    Quaternion newAttachementRot = G_Attachement.GetRelevantAttachementRotation();

                    //if (qualityLOD == 0)
                    //{
                    //    float angle = Quaternion.Angle(_GlueLastAttachRotation, newAttachementRot);
                    //    if (angle > 1f)
                    //    {
                    //        newAttachementRot = Quaternion.Lerp(_GlueLastAttachRotation, newAttachementRot, Owner.DeltaTime * (4f + angle * 0.2f));
                    //    }
                    //}

                    _GlueLastAttachRotation = newAttachementRot;
                    _GluePosition = G_AttachementHandler.GetGluePosition();

                    Gluing_DragStretchApply();

                    if (Owner.AnimateFeet)
                    {
                        if (Owner.LimitFeetYaw > 0f)
                            _GlueRotation = G_AttachementHandler.GetGlueRotation();
                        //_GlueRotation = Quaternion.LerpUnclamped(A_LastApppliedAlignRot, G_AttachementHandler.GetGlueRotation(), 0.5f);
                        else
                        {
                            _GlueRotation = A_LastApppliedAlignRot;
                        }
                    }
                }

                G_AttachementHandler.PostUpdate();
            }

            /// <summary> World Space </summary>
            Vector3 G_GlueDragOffset = Vector3.zero;
            void Gluing_DragStretchApply()
            {
                if (Owner.AllowGlueDrag > 0f) // Shifting too much stretched glue point
                {
                    float stretchFactor = IKProcessor.GetStretchValue(_GluePosition - Owner._LastAppliedHipsStabilityOffset);

                    float baseStretchFact = Mathf.LerpUnclamped(1f, 0.825f, Owner.AllowGlueDrag);
                    float stretchHelper = baseStretchFact * Mathf.LerpUnclamped(1f, LegStretchLimit, Owner.AllowGlueDrag);
                    if (stretchHelper > baseStretchFact) stretchHelper = baseStretchFact;

                    Vector3 targetDragPos = _GluePosition;

                    if (stretchFactor > stretchHelper * 1.1f)
                    {
                        float diff = (stretchFactor - stretchHelper * 1.1f) * 2f * Mathf.Min(1f, Owner.AllowGlueDrag);

                        if (A_PreWasAligning)
                            targetDragPos = Vector3.Lerp(_GluePosition, ankleAlignedOnGroundHitWorldPos, diff);
                        else
                            targetDragPos = Vector3.Lerp(_GluePosition, A_PreIKPosForGluing, diff);
                    }

                    Vector3 offset = targetDragPos - _GluePosition;
                    G_GlueDragOffset = Vector3.Lerp(G_GlueDragOffset, offset, Owner.DeltaTime * 14f);

                    if( float.IsNaN( G_GlueDragOffset.x ) || float.IsNaN( G_GlueDragOffset.z ) ) G_GlueDragOffset = Vector3.zero;
                }
            }




            /// <summary> Apply Glue / Leg Adjust Animation to the IK controls </summary>
            void Gluing_ApplyCoords()
            {
                if (_glueTargetBlend < 0.0001f) // Gluing inactive
                {
                    return;
                }

                float applyBlend = _glueTargetBlend * G_AttachementHandler.glueAnimationBlend;
                
                if (applyBlend >= 1f)
                {
                    _FinalIKPos = _GluePosition + G_GlueDragOffset;

                    if (Owner.AnimateFeet)
                    {
                        _FinalIKRot = _GlueRotation;
                    }
                }
                else
                {
                    _FinalIKPos = Vector3.LerpUnclamped(A_PreIKPosForGluing, _GluePosition + G_GlueDragOffset, applyBlend);

                    if (Owner.AnimateFeet) //A_LastTargetAlignRot
                    {
                        _FinalIKRot = Quaternion.LerpUnclamped(_FinalIKRot, _GlueRotation, applyBlend);
                    }
                }

            }


        }

    }
}