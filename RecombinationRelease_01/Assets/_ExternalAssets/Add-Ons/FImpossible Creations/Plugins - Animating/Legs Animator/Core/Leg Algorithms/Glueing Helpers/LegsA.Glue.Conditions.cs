using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            /// <summary> Check if there is raycast ground hit and if the hit is in gluing foot range </summary>
            public bool G_AttachPossible
            {
                get { return RaycastHitted && C_Local_MidFootPosVsGroundHit.y < BelowFootRange * Owner.AllowGlueBelowFoot + FloorLevel; }
            }

            public bool _Glue_AskingForDetach { get; private set; }
            public bool Glue_CheckDetachement()
            {
                bool detach = Glue_Conditions_Detach();
                if (!detach) detach = Glue_Conditions_DetachForced(); // If detaching is forced, detach anyway
                _Glue_AskingForDetach = detach;
                return detach;
            }

            /// <summary> Confirm that leg can be detached right now.
            /// It can be restricted by idle glue mode with opposite leg during transition etc. </summary>
            public bool Glue_CheckIdleDetachementConfirm()
            {
                if (Owner._glueModeExecuted != EGlueMode.Idle) return true;

                if (hasOppositeleg)
                {
                    Leg oppositeLeg = GetOppositeLeg();
                    if (Glue_CheckOppositeLegMovementRestriction(oppositeLeg))
                    {
                        return false; // Prevent detaching when other leg is adjusting
                    }
                }

                return true;
            }

            /// <summary> Basically ScaleReference but smaller </summary>
            public float BelowFootRange
            {
                get
                {
                    return ScaleRef * _C_DynamicYScale * 0.2f;
                }
            }

            bool Glue_Conditions_Attach()
            {
                if (Owner.IsGrounded == false) { /*_Editor_Label += "!IsGrounded!";*/ return false; }
                if (_glueTargetBlend < 0.0001f) { /*_Editor_Label += "!TargetBlend!";*/ return false; }
                if (!RaycastHitted) {  /*_Editor_Label += "!RaycastHitted!";*/ return false; }
                if (G_CustomForceNOTDetach /*|| G_StepHeatmapForceNOTDetach*/) { return true; }
                if (_gluingCulldown > 0f) { return false; }
                if (G_CustomForceAttach) { return true; }

                //if (Owner._usingStepHeatmap == false)
                if (Owner.DontGlueAttachIfTooNearOppositeLeg > 0f)
                    if (hasOppositeleg)
                    {
                        Vector3 lastFinalLocal = ToRootLocalSpace(_PreviousFinalIKPos);
                        var oppositeLeg = GetOppositeLeg();
                        Vector3 oppositeLastFinalLocal = ToRootLocalSpace(oppositeLeg._PreviousFinalIKPos);
                        float preventOnLowerThan = Owner.DontGlueAttachIfTooNearOppositeLeg * Owner.ScaleReference;

                        if (Vector2.Distance(new Vector2(lastFinalLocal.x, lastFinalLocal.z), new Vector2(oppositeLastFinalLocal.x, oppositeLastFinalLocal.z)) < preventOnLowerThan)
                            return false;
                    }

                // Foot height in animation on grounded level
                //if (IsFootGroundedInAnimation == false) { /*_Editor_Label += "!GroundedInAnimation!";*/ return false; }

                if (G_HandlerExecutingLegAnimationMode == EGlueMode.Moving)
                {
                    #region Foot Y Diff Condition

                    bool condition_YDiff = false;
                    float yDiff = C_Local_MidFootPosVsGroundHit.y;

                    if (yDiff > FloorLevel) // hit below foot - foot above ground
                    {
                        if (yDiff < BelowFootRange * Owner.AllowGlueBelowFoot + FloorLevel) // Still in range for gluing
                        {
                            condition_YDiff = true;
                        }
                        else
                        {
                            //UnityEngine.Debug.Log("floor too low - detaching");
                        }
                    }
                    else // Hit above foot 
                        condition_YDiff = true;

                    #endregion

                    if (!condition_YDiff) { /*_Editor_Label += "!YDiff!";*/ return false; }
                }

                if (G_CustomForceNOTAttach) return false;

                #region Desired leg animation swinging direction

                if (Owner._glueModeExecuted == EGlueMode.Moving)
                {
                    if (Owner.SwingHelper > 0f)
                        if (Owner.DesiredMovementDirection != Vector3.zero)
                        {
                            Vector3 desiredLocal = ToRootLocalSpaceDir(Owner.DesiredMovementDirection);
                            Vector3 legSwingLocal = _G_RefernceSwing;
                            float swingDot = Vector3.Dot(desiredLocal.normalized, legSwingLocal.normalized);

                            if (swingDot > 1f - Owner.SwingHelper) return false; // Dont allow attach when swinging foot in the same direction as desired direction
                        }
                }

                #endregion

                return true;
            }


            /// <summary> Returnting true when leg glue transition is not yet compleated (within progress range) </summary>
            bool Glue_CheckOppositeLegMovementRestriction(Leg oppositeLeg)
            {
                if (RaycastHitted == false) return false;
                if (!Owner.IsGrounded) return false;

                //if (!A_WasAligning) return false; // Raycast treshold check instead, see below line:
                if (C_Local_MidFootPosVsGroundHit.y > BelowFootRange) return false;
                if (G_CustomForceNOTDetach /*|| G_StepHeatmapForceNOTDetach*/) return true; // Prevent detach

                if (oppositeLeg.RaycastHitted == false) return false;
                if (oppositeLeg.C_Local_MidFootPosVsGroundHit.y > oppositeLeg.BelowFootRange) return false;
                //if (oppositeLeg.G_AttachementHandler.StartingTransition) return true; // Prevent detach

                //if (!Owner._usingStepHeatmap)
                float transitionProgr = oppositeLeg.G_GlueInternalTransition;
                if (/*transitionProgr > 0.01f && */transitionProgr < LegAnimatingSettings.AllowDetachBefore)
                {
                    return true; // Prevent detach
                }

                return false; // Allow detach
            }


            /// <summary> Returns null if no opposite leg detected </summary>
            Leg GetOppositeLeg()
            {
                if (OppositeLegIndex < 0) return null;
                if (OppositeLegIndex >= Owner.Legs.Count) return null;
                return Owner.Legs[OppositeLegIndex];
            }

            void Gluing_SetCulldown(float minDuration = 0.01f)
            {
                _gluingCulldown = Mathf.Max(_gluingCulldown, minDuration + (0.02f - Owner.GlueFadeOutSpeed * 0.03f));
            }

            public bool Glue_Conditions_Detach()
            {
                bool detach = false;
                if (G_CustomForceNOTDetach /*|| G_StepHeatmapForceNOTDetach*/) { return detach; } // Prevent detach

                // Prevent detach when leg adjustement is being executed
                if (G_AttachementHandler.legMoveAnimation.duringLegAdjustMovement) { return false; }

                // If attaching conditions met, don't detach to prevent re-attaching
                if (Glue_Conditions_Attach() == false)
                {
                    detach = true;
                }

                if (Owner.AnimateFeet) if (lastFootForwardAngleDiffABS > Owner.UnglueOn)
                    {
                        if (!G_JustLanded)
                        {
                            if (Owner._glueModeExecuted != EGlueMode.Idle) Gluing_SetCulldown();

                            //if (G_AttachementHandler.lastAttachingGlueMode != EGlueMode.Idle) Gluing_SetCulldown();
                            detach = true;
                        }
                    }

                if (!detach)
                {
                    //Vector2 flatCurrentPos = new Vector2();
                    //flatCurrentPos.x = ankleAlignedOnGroundHitRootLocal.x;
                    //flatCurrentPos.y = ankleAlignedOnGroundHitRootLocal.z;
                    //float distanceToAttachement = Vector2.Distance(flatCurrentPos, new Vector2(_GlueLastAttachPositionRootLocal.x, _GlueLastAttachPositionRootLocal.z));
                    if (!G_JustLanded)
                    {
                        Vector3 _off = Vector3.zero;
                        if (GluePointOffset != Vector2.zero) _off = -GetGluePointOffset();

                        float distanceToAttachement = Vector3.Distance(ankleAlignedOnGroundHitRootLocal + _off, _GlueLastAttachPositionRootLocal);

                        if (distanceToAttachement > G_GlueTesholdRange) // Foot too far from glue position
                        {
                            if (Owner._glueModeExecuted != EGlueMode.Idle) Gluing_SetCulldown();

                            detach = true;
                            //if (G_AttachementHandler.lastAttachingGlueMode != EGlueMode.Idle) Gluing_SetCulldown();
                        }
                    }
                }

                return detach;
            }

            public Vector3 GetGluePointOffset()
            {
                float scaleOff = Owner.ScaleReferenceNoScale * Owner.GlueRangeThreshold;
                return Owner.RootToWorldSpaceVec(new Vector3(GluePointOffset.x * scaleOff, 0, GluePointOffset.y * scaleOff));
            }

            //public bool G_StepHeatmapForceDetach = false;
            //public bool G_StepHeatmapForceNOTDetach = false;

            /// <summary> Resetted each frame </summary>
            public bool G_CustomForceAttach = false;
            /// <summary> Resetted each frame </summary>
            public bool G_CustomForceNOTDetach = false;
            /// <summary> Resetted each frame </summary>
            public bool G_CustomForceDetach = false;
            /// <summary> Resetted each frame </summary>
            public bool G_CustomForceNOTAttach = false;

            bool Glue_Conditions_DetachForced()
            {
                if (G_CustomForceDetach) return true;
                //if (G_StepHeatmapForceDetach) return true;

                #region Request Repose

                if (G_RequestRepose != GlueReposeRequest.None)
                {
                    if (G_RequestRepose == GlueReposeRequest.ReposeIfFar)
                    {
                        G_RequestRepose = GlueReposeRequest.None;

                        if (G_Attached)
                        {
                            if (Vector3.Distance(_GluePosition, ankleAlignedOnGroundHitWorldPos) > ScaleRef * 0.1f) return true;
                        }
                    }
                    else
                    {
                        G_RequestRepose = GlueReposeRequest.None;
                        return true;
                    }
                }

                #endregion

                return false;
            }


        }

    }
}