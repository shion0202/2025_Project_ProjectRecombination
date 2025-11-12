using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            public bool A_PreWasAligning { get; private set; }
            public bool A_PreWasAligningNearGround { get; private set; }
            public bool A_WasAligning { get; private set; }
            public float A_AligningHelperBlend { get; private set; }
            public float A_LastAlignHeightDiff { get; private set; }
            public float A_LastAlignHeightCompareValue { get; private set; }

            Vector3 A_PreviousRelevantAnklePos;
            Vector3 A_LastApppliedAlignPos;
            /// <summary> Changed only on A_PreWasAligning </summary>
            Vector3 A_LastApppliedAlignPosLocal;
            Vector3 A_PreIKPosForGluing;

            Quaternion A_LastApppliedAlignRot;
            Quaternion A_LastTargetAlignRot;

            void AlignStep_Init()
            {
                A_PreWasAligning = false;
                A_PreWasAligningNearGround = false;
                A_WasAligning = false;
                A_AligningHelperBlend = 0f;
                A_LastTargetAlignRot = BoneEnd.rotation;
                A_LastApppliedAlignRot = BoneEnd.rotation;
                A_PreIKPosForGluing = _FinalIKPos;
            }

            void AlignStep_CheckAlignStatePre()
            {
                A_PreIKPosForGluing = _FinalIKPos;
                if (_noRaycast_skipFeetCalcs) return;

                bool align = false;
                A_PreWasAligningNearGround = false;

                if (RaycastHitted)
                {
                    float hipsHelp = ParentHub._Hips_StepHeightAdjustOffset;

                    // Cathing ground below when standing on the big slope down
                    if (hipsHelp < 0f) hipsHelp *= -0.03f; else hipsHelp = 0f;

                    A_LastAlignHeightDiff = C_Local_MidFootPosVsGroundHit.y;
                    A_LastAlignHeightCompareValue = ScaleRef * (0.002f + Owner.AnimationFloorLevel) + hipsHelp;

                    if (Owner.FootAlignRapidity > 0.9999f)
                    {
                        if (A_LastAlignHeightDiff <= A_LastAlignHeightCompareValue)
                        {
                            align = true;
                            A_PreWasAligningNearGround = true;
                        }
                    }
                    else
                    {
                        if (A_LastAlignHeightDiff <= A_LastAlignHeightCompareValue + ScaleRef * (0.04f + (1f - Owner.FootAlignRapidity) * 0.04f))
                        {
                            A_PreWasAligningNearGround = true; // Align foot sooner, before position align to rotate on step slope before hitting ground
                            if (A_LastAlignHeightDiff <= A_LastAlignHeightCompareValue) align = true;
                        }
                    }

                    //A_PreWasAligningDeeper = A_LastAlignHeightDiff <= ScaleRef * (0.002f /*+ 0.1f * Owner.FootDeeperRange*/) + hipsHelp;
                }
                else
                {
                    A_LastAlignHeightDiff = 100f;
                    //A_PreWasAligningDeeper = false;
                }

                A_PreWasAligning = align;


                if (align)
                {
                    //Vector3 hAlign = ToRootLocalSpace(_FinalIKPos);
                    //hAlign.y = ankleAlignedOnGroundHitRootLocal.y;
                    //Vector3 newPos = RootSpaceToWorld(hAlign);
                    Vector3 newPos = ankleAlignedOnGroundHitWorldPos;

                    if (A_AligningHelperBlend > 0.99f)
                        _FinalIKPos = newPos;
                    else
                        _FinalIKPos = Vector3.Lerp(_FinalIKPos, newPos, A_AligningHelperBlend * 8f);

                    A_PreIKPosForGluing = newPos;
                }
                else
                {
                    if (A_AligningHelperBlend > 0.01f)
                        _FinalIKPos = Vector3.Lerp(_FinalIKPos, RootSpaceToWorld(A_LastApppliedAlignPosLocal), A_AligningHelperBlend);
                }

                //A_PreIKPosForGluing = G_AttachementHandler.legMoveAnimation.lastAppliedGluePosition;
            }

            void AlignStep_ValidateFootRotation()
            {
                if (!Owner.AnimateFeet) return;
                if (_noRaycast_skipFeetCalcs) return;

                if (A_PreWasAligningNearGround)
                {
                    float blend = Owner.FootRotationBlend * A_AligningHelperBlend;

                    if (A_LastElevateH < 0.02f)
                    {
                        // Apply target foot rotation to aligned on raycast hit rotation
                        if (blend >= 1f)
                            A_LastTargetAlignRot = ankleAlignedOnGroundHitRotation;
                        else
                            A_LastTargetAlignRot = Quaternion.LerpUnclamped(_FinalIKRot, ankleAlignedOnGroundHitRotation, blend);
                    }
                    else // When elevating foot, we blending out rotation a bit
                    {
                        float factor = A_LastElevateH / (ScaleRef * 0.15f);
                        if (A_LastElevateH > 1f) A_LastElevateH = 1f;
                        A_LastTargetAlignRot = Quaternion.LerpUnclamped(ankleAlignedOnGroundHitRotation, _FinalIKRot, factor * blend);
                    }
                }
                else // If not aligning, refreshing target rotation
                {
                    if (A_AligningHelperBlend < 0.001f)
                        A_LastTargetAlignRot = _FinalIKRot;
                    else
                        A_LastTargetAlignRot = Quaternion.Lerp(_FinalIKRot, A_LastTargetAlignRot, A_AligningHelperBlend);
                }

                // Rotate towards target foot rotation with smooth lerp motion
                if (Owner.FootAlignRapidity >= 1f)
                    A_LastApppliedAlignRot = A_LastTargetAlignRot;
                else
                    A_LastApppliedAlignRot = Quaternion.Lerp(A_LastApppliedAlignRot, A_LastTargetAlignRot, DeltaTime * (8f + Owner.FootAlignRapidity * 26f));

                _FinalIKRot = A_LastApppliedAlignRot;
            }


            bool A_WasFullAlign = false;
            float A_aligningBlendByGluing = 1f;

            /// <summary> Define A_DefaultStepIKPosition </summary>
            void AlignStep_OnGroundAlign()
            {

                if (_noRaycast_skipFeetCalcs)
                {
                    A_WasAligning = A_PreWasAligning;

                    if (A_PreWasAligning)
                    {
                        if (A_AligningHelperBlend < 0.05f) A_AligningHelperBlend = 0.05f;
                        A_AligningHelperBlend = Mathf.MoveTowards(A_AligningHelperBlend, 1f, Owner.DeltaTime * 8f);

                        if (!A_WasFullAlign)
                            if (A_AligningHelperBlend >= 1f - Owner.EventExecuteSooner)
                            {
                                A_WasFullAlign = true;
                                if (Owner.UseGluing == false) SendStepEvent();
                            }
                    }
                    else
                    {
                        if (A_AligningHelperBlend > 0.5f) A_AligningHelperBlend = 0.5f;
                        A_AligningHelperBlend = Mathf.MoveTowards(A_AligningHelperBlend, 0f, Owner.DeltaTime * 14f);
                    }

                    if (A_AligningHelperBlend < 0.65f) A_WasFullAlign = false;

                    return;
                }

                A_aligningBlendByGluing = 1f;
                if (Owner.UseGluing)
                {
                    A_aligningBlendByGluing = 1f - (_glueTargetBlend * G_GlueAnimationBlend);
                }

                if (A_PreWasAligning)
                {
                    if (A_WasAligning) A_PreviousRelevantAnklePos = previousAnkleAlignedOnGroundHitWorldPos;

                    float blend = A_aligningBlendByGluing * A_AligningHelperBlend;

                    if (blend >= 1f)
                        _FinalIKPos = ankleAlignedOnGroundHitWorldPos; // Seamless Foot Position on the ground
                    else
                        _FinalIKPos = Vector3.LerpUnclamped(_FinalIKPos, ankleAlignedOnGroundHitWorldPos, blend);

                    if (A_AligningHelperBlend < 0.05f) A_AligningHelperBlend = 0.05f;
                    A_AligningHelperBlend = Mathf.MoveTowards(A_AligningHelperBlend, 1f, Owner.DeltaTime * 8f);

                    if (!A_WasFullAlign)
                        if (A_AligningHelperBlend >= 1f - Owner.EventExecuteSooner)
                        {
                            A_WasFullAlign = true;
                            if (Owner.UseGluing == false) SendStepEvent();
                        }

                    A_LastApppliedAlignPosLocal = ToRootLocalSpace(_FinalIKPos);
                }
                else
                {
                    A_PreviousRelevantAnklePos = _SourceIKPosUnchangedY;

                    if (A_AligningHelperBlend > 0.75f) A_AligningHelperBlend = 0.75f;
                    A_AligningHelperBlend = Mathf.MoveTowards(A_AligningHelperBlend, 0f, Owner.DeltaTime * 18f);
                }

                if (A_AligningHelperBlend < 0.6f) A_WasFullAlign = false;

                A_LastApppliedAlignPos = _FinalIKPos;

                A_WasAligning = A_PreWasAligning;
            }



            Vector3 A_LastElevation;
            float A_LastElevateH = 0f;
            float _sd_A_Elev = 0f;

            /// <summary> Foots Y Offset apply </summary>
            void AlignStep_LegElevation()
            {
                if (Owner.LegElevateBlend < 0.001f) return;

                if (_noRaycast_skipFeetCalcs)
                {
                    A_LastElevation = Vector3.zero;
                    return;
                }

                float scaleRef = ScaleRef;

                // Using leg elevate on ground overlapping
                float heightInGroundAlign = (groundHitRootSpacePos.y - A_LastSuddenSmoothYOffset) - ParentHub._Hips_StepHeightAdjustOffset;
                float flr = Owner.AnimationFloorLevel * scaleRef;
                float heightInAnim = C_Local_FootElevateInAnimation;


                if (heightInAnim > flr && heightInGroundAlign > (0.001f * scaleRef + flr) + 0.1f)
                {
                    heightInAnim -= flr;

                    float animationRelevantElevation = heightInAnim; // Elevate leg on top of ground hit accordingly with animation

                    // But make it controlled to avoid unneccesary elevating and limit it
                    // distanceBetweenGroundAndAnim -> above zero = foot above ground in safe position
                    float distanceBetweenGroundAndAnim = heightInAnim - heightInGroundAlign;
                    float elevateApplyTreshold = scaleRef * 0.015f;

                    float groundVSfootSafeDistance = scaleRef * 0.35f;

                    // Fade out elevation in safe height distance of foot VS ground hit
                    float fadeOutFactor = distanceBetweenGroundAndAnim / groundVSfootSafeDistance;
                    // Foot close ground -> fadeOutFactor = 0
                    fadeOutFactor = Mathf.Clamp01(fadeOutFactor);

                    // needs to be bigger than ground touching distance
                    if (distanceBetweenGroundAndAnim > elevateApplyTreshold)
                    {
                        //fadeOutFactor *= fadeOutFactor; // Exponential damping
                        animationRelevantElevation *= (1f - fadeOutFactor);

                        if (A_AligningFor < 0) A_AligningFor = DeltaTime;

                        if (A_AligningFor < 0.3f)
                            A_AligningFor += DeltaTime;
                        else
                            A_AligningFor = 0.3f;
                    }
                    else
                    {
                        if (A_AligningFor > 0f)
                            A_AligningFor -= DeltaTime;
                        else
                            A_AligningFor = 0f;
                    }

                    float targetLegElevateHeightOffset = animationRelevantElevation;

                    float raiseLimit = scaleRef * Mathf.LerpUnclamped(0.1f, 0.9f, Owner.LegElevateHeightLimit);
                    if (targetLegElevateHeightOffset > raiseLimit) targetLegElevateHeightOffset = raiseLimit;


                    //A_LastElevateH = Mathf.SmoothDamp(A_LastElevateH, targetLegElevateHeightOffset, ref _sd_A_Elev, 1f, float.MaxValue, DeltaTime);

                    //float baseDur = A_WasSmoothing ? 0.055f : 0.025f;
                    //float dur = 0.25f * (Mathf.LerpUnclamped(.25f, 1f, fadeOutFactor)) + baseDur;
                    //if (A_AligningFor > 0.1f) dur *= 0.05f;

                    //A_LastElevateH = Mathf.SmoothDamp(A_LastElevateH, targetLegElevateHeightOffset, ref _sd_A_Elev, dur, float.MaxValue, DeltaTime);

                    // Height limit apply
                    if (groundHitRootSpacePos.y > 0)
                    {
                        float limitFactor = ScaleRef * 0.2f;
                        if (limitFactor > 0f)
                        {
                            float limitRatio = groundHitRootSpacePos.y / limitFactor;
                            if (limitRatio > 0.8f)
                            {
                                targetLegElevateHeightOffset = Mathf.LerpUnclamped(targetLegElevateHeightOffset, 0f, Mathf.InverseLerp(0.8f, 1.1f, limitRatio));
                            }
                        }
                    }


                    float elevDiff = targetLegElevateHeightOffset - A_LastElevateH;
                    elevDiff = Mathf.Abs(elevDiff);

                    if (elevDiff > scaleRef * Adj_A_ElevateSpeedupMargin)
                    {
                        A_LastElevateH = Mathf.Lerp(A_LastElevateH, targetLegElevateHeightOffset, DeltaTime * Adj_A_ElevateLerpSpeedAfter);
                    }
                    else
                    {
                        A_LastElevateH = Mathf.Lerp(A_LastElevateH, targetLegElevateHeightOffset, DeltaTime * Adj_A_ElevateLerpSpeedStart);
                    }

                    if (A_LastElevateH < 0f) A_LastElevateH = 0f;
                    //UnityEngine.Debug.Log("height in anim = " + heightInAnim + "   :   ground align h " + heightInGroundAlign + "   :   ElevateH = " + A_LastElevateH);
                }
                else
                {
                    A_LastElevateH = Mathf.SmoothDamp(A_LastElevateH, 0f, ref _sd_A_Elev, 0.02f, 100000f, DeltaTime);
                }

                A_LastElevation = RootSpaceToWorldVec(new Vector3(0f, A_LastElevateH * Owner.LegElevateBlend * A_aligningBlendByGluing, 0f));
                //_Editor_Label += "  Elev:" + System.Math.Round(A_LastElevateH, 2) + " inAnim = " + System.Math.Round(C_Local_FootElevateInAnimation,2);

                _FinalIKPos += A_LastElevation;
            }

            /// <summary> Can be freely adjusted through custom module (default 8) </summary>
            [NonSerialized] public float Adj_A_ElevateLerpSpeedStart = 8f;
            /// <summary> Can be freely adjusted through custom module (default 5) </summary>
            [NonSerialized] public float Adj_A_ElevateLerpSpeedAfter = 5f;
            /// <summary> Can be freely adjusted through custom module (default 0.014) </summary>
            [NonSerialized] public float Adj_A_ElevateSpeedupMargin = 0.014f;

            float A_AligningFor = 0f;
            Vector3 A_LastAlignRootSpacePos;
            /// <summary> Assigned when A_WasSmoothing is true </summary>
            Vector3 A_LastSmoothTargetedPosLocal;
            float A_LastSuddenSmoothYOffset = 0f;
            float A_SuddenSmoothing = 0f;
            float A_lastSuddenSmoothingDiff = 0f;
            bool A_WasSmoothing = false;
            //float smoothingTime = -1f;
            bool A_WasAligningFrameBack = false;

            /// <summary> Smoothing sudden steps Y </summary>
            void AlignStep_SmoothSuddenSteps()
            {
                if (Owner.SmoothSuddenSteps < 0.0001f) return;

                if (_noRaycast_skipFeetCalcs || G_Attached)
                {
                    A_WasAligningFrameBack = A_WasAligning;
                    A_WasSmoothing = false;
                    return;
                }

                float scaleRef = ScaleRef;

                if (A_WasAligning || A_WasAligningFrameBack)
                {
                    if (!A_WasAligning) A_PreviousRelevantAnklePos = previousAnkleAlignedOnGroundHitWorldPos;

                    Vector3 currentLocAlign = ToRootLocalSpace(ankleAlignedOnGroundHitWorldPos);
                    Vector3 preAlignRootY;

                    // If already smoothing -> check diff between last focused ik pos
                    // If not smoothing -> check diff between last relevant ik pos
                    if (A_WasSmoothing)
                    {
                        preAlignRootY = ToRootLocalSpace(previousAnkleAlignedOnGroundHitWorldPos);
                    }
                    else
                    {
                        preAlignRootY = ToRootLocalSpace(A_PreviousRelevantAnklePos);
                    }

                    float yDiff = preAlignRootY.y - currentLocAlign.y;
                    yDiff = Mathf.Abs(yDiff);

                    //_Editor_Label = "yDiff: " + R(yDiff, 4);

                    float smoothingTreshold = scaleRef * (0.006f); // Treshold to start next checks

                    if ((raycastSlopeAngle < 17f || raycastSlopeAngle > 80f) || Owner.RaycastShape == ERaycastMode.Spherecast) // To avoid smoothing all the time running on slopes (needs better solution)
                        if (yDiff > smoothingTreshold)
                        {
                            float diffFactor = yDiff / (scaleRef * 0.275f);
                            if (diffFactor > 1f) diffFactor = 1f;
                            //_Editor_Label = "   diffFactor: " + R(diffFactor, 4);

                            // Treshold dictated by smooth sudden steps parameter
                            if (diffFactor > Mathf.LerpUnclamped(0.25f, 0.1f, Owner.SmoothSuddenSteps))
                            {
                                float smoothBooster = Mathf.LerpUnclamped(0.3f, 0.1f, Owner.SmoothSuddenSteps);

                                if (A_lastSuddenSmoothingDiff == 0f || A_SuddenSmoothing < diffFactor)
                                {
                                    A_lastSuddenSmoothingDiff = yDiff;
                                    A_LastAlignRootSpacePos = ToRootLocalSpace(previousAnkleAlignedOnGroundHitWorldPos); //_PreviousFinalIKPos
                                    smoothBooster *= 0.7f;
                                }
                                else
                                {
                                    if (Owner.SmoothSuddenSteps < 0.5f)
                                    {
                                        float reAdjust = Mathf.LerpUnclamped(0.5f, 0.0f, Owner.SmoothSuddenSteps);
                                        A_lastSuddenSmoothingDiff = Mathf.LerpUnclamped(A_lastSuddenSmoothingDiff, yDiff, reAdjust);
                                    }
                                }

                                A_SuddenSmoothing += Mathf.Clamp01(A_lastSuddenSmoothingDiff / (scaleRef * smoothBooster));

                                float maxSm = 0.85f + Owner.SmoothSuddenSteps * 0.165f;
                                if (A_SuddenSmoothing > maxSm) A_SuddenSmoothing = maxSm;
                            }
                        }

                }

                //_Editor_Label += "  Smooth: " + R(A_SuddenSmoothing, 4);

                if (A_SuddenSmoothing > 0f)
                {
                    Vector3 currIKLoc = ToRootLocalSpace(_FinalIKPos);

                    //if (A_GlueAskForSmooth)
                    //{
                    //    if (A_PreWasAligning) currIKLoc = ankleAlignedOnGroundHitRootLocal;
                    //    A_GlueAskForSmooth = false;
                    //}

                    A_LastSuddenSmoothYOffset = currIKLoc.y;

                    A_SuddenSmoothing -= Owner.DeltaTime * Mathf.LerpUnclamped(60f, 7.5f, Owner.SmoothSuddenSteps);

                    // Shift IK local Y pos before sudden change   ->   towards current local Y  (currIKLoc.Y)
                    currIKLoc.y = Mathf.Lerp(currIKLoc.y, A_LastAlignRootSpacePos.y, A_SuddenSmoothing);

                    // When foot is pushed down to compensate the value is < 0 -> leg is moving up
                    // with value > 0 leg is moving down (y is compensating it up)
                    A_LastSuddenSmoothYOffset = currIKLoc.y - A_LastSuddenSmoothYOffset;
                    //_Editor_Label += "  SMY: " + A_LastSuddenSmoothYOffset;

                    A_LastSmoothTargetedPosLocal = currIKLoc;
                    A_SmoothedIKPos = RootSpaceToWorld(currIKLoc);
                    _FinalIKPos = A_SmoothedIKPos;

                    if (A_SuddenSmoothing < 0f) A_SuddenSmoothing = 0f;

                    A_WasSmoothing = true;
                }
                else
                {
                    A_LastSuddenSmoothYOffset = 0f;
                    A_WasSmoothing = false;
                }
            }

            /// <summary> Changed only on A_WasSmoothing </summary>
            Vector3 A_SmoothedIKPos;

            void AlignStep_Complete()
            {
                A_WasAligningFrameBack = A_WasAligning;
            }

        }

    }
}