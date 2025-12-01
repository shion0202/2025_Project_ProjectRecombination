using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class HipsReference
        {

            Vector3 _Hips_StabilityLocalAdjustement = Vector3.zero;
            Vector3 _Hips_sd_StabilAdjustm = Vector3.zero;
            public Vector3 _Get_Hips_StabilityLocalAdjustement { get { return _Hips_StabilityLocalAdjustement; } }

            /// <summary> Local Space Stability Adjust </summary>
            public Vector3 CalculateCenterOfMassStability( float stabilizingMultiplier )
            {

                if( Owner.StabilizeCenterOfMass > 0f )
                {
                    // Compute center of weight pose difference (local space)
                    Vector3 stabilityDiff = new Vector3( 0f, 0f, 0f );
                    float legsDiv = ChildLegs.Count;

                    if( Owner.StabilityAlgorithm == EStabilityMode.Biped_Deprecated )
                    {
                        for( int l = 0; l < ChildLegs.Count; l++ )
                        {
                            var leg = ChildLegs[l];
                            Vector3 footmiddleOff = leg.AnkleH.Bone.TransformVector( leg.AnkleToFeetEnd * 0.6f );
                            Vector3 legIKReferencePos = leg._PreviousFinalIKPosForStability;

                            Vector3 footLocalPos = Owner.ToRootLocalSpace( legIKReferencePos + footmiddleOff );
                            Vector3 initRefPose = leg.InitialPosInRootSpace;
                            initRefPose.y += _Hips_LastHipsOffset;

                            Vector3 stablePose;
                            if( Owner.AnimationIsStablePose >= 1f ) stablePose = leg.AnkleH.LastKeyframeRootPos;
                            else if( Owner.AnimationIsStablePose <= 0f ) stablePose = initRefPose;
                            else stablePose = Vector3.LerpUnclamped( initRefPose, leg.AnkleH.LastKeyframeRootPos, Owner.AnimationIsStablePose );


                            Vector3 target = footLocalPos - stablePose;
                            target.y *= 0.25f;
                            stabilityDiff += target * leg.BlendWeight * 0.5f * ( stabilizingMultiplier * Owner.StabilizeCenterOfMass );
                        }

                        stabilityDiff.y /= legsDiv;
                    }
                    else if( Owner.StabilityAlgorithm == EStabilityMode.Universal )
                    {
                        Vector3 pelvinInLocal = LastRootLocalPos;
                        //float lowestLegLocalPos = float.MaxValue;

                        for( int l = 0; l < ChildLegs.Count; l++ )
                        {
                            var leg = ChildLegs[l];

                            #region Reference Local Foot Position (keyframe or initial TPose) - stablePose

                            Vector3 initRefPose = leg.InitialPosInRootSpace;
                            initRefPose.y += _Hips_LastHipsOffset;

                            Vector3 stablePose;
                            if( Owner.AnimationIsStablePose >= 1f ) stablePose = leg.AnkleH.LastKeyframeRootPos;
                            else if( Owner.AnimationIsStablePose <= 0f ) stablePose = initRefPose;
                            else stablePose = Vector3.LerpUnclamped( initRefPose, leg.AnkleH.LastKeyframeRootPos, Owner.AnimationIsStablePose );

                            #endregion

                            Vector3 stableDiff = pelvinInLocal - stablePose;

                            Vector3 footLocalPos = Owner.ToRootLocalSpace( leg._PreviousFinalIKPosForStability );
                            //if (footLocalPos.y < lowestLegLocalPos) lowestLegLocalPos = footLocalPos.y;

                            Vector3 currDiff = pelvinInLocal - footLocalPos;

                            Vector3 target = stableDiff - currDiff;
                            target.y *= 0.25f;
                            stabilityDiff += ( target * leg.BlendWeight * ( stabilizingMultiplier * Owner.StabilizeCenterOfMass ) ) / legsDiv;
                        }

                    }

                    if( stabilityDiff.y > 0f ) // Prevent feet off-ground
                    {
                        stabilityDiff.y = 0f; //*= Mathf.InverseLerp(0, 0.1f, stabilityDiff.y);
                    }

                    // Apply calculated stability offset smoothing
                    if( Owner.StabilizingSpeed < 1f )
                    {
                        float targetDuration = 0f;
                        if( Owner.StabilizingSpeed < 1f ) targetDuration = 0.001f + ( 1f - Owner.StabilizingSpeed ) * 0.4f;
                        Owner.ValueTowards( ref _Hips_StabilityLocalAdjustement, stabilityDiff, ref _Hips_sd_StabilAdjustm, targetDuration );
                    }
                    else
                    {
                        _Hips_StabilityLocalAdjustement = stabilityDiff;
                    }
                }
                else
                {
                    _Hips_StabilityLocalAdjustement = Vector3.zero;
                }

                return _Hips_StabilityLocalAdjustement;
            }






            Vector3 _stretchPreventerOff = Vector3.zero;

            /// <summary> Stretcher offset in local space </summary>
            public Vector3 CalculateStretchPreventerOffset()
            {
                if( Owner.HipsStretchPreventer < 0.0001f ) return Vector3.zero;

                Vector3 stretchPreventerOffset = Vector3.zero;
                float stretched = 0f;

                Vector3 hubFloorPos = LastRootLocalPos;
                hubFloorPos.y = 0f;
                hubFloorPos = Owner.baseTransform.TransformPoint( hubFloorPos );

                for( int l = 0; l < ChildLegs.Count; l++ )
                {
                    var leg = ChildLegs[l];

                    float stretchFactor = leg.IKProcessor.GetStretchValue( leg._PreviousFinalIKPosForStability );

                    if( stretchFactor > Owner.LimitLegStretch * 0.975f )
                    {
                        stretched += 1f;
                        float diff = stretchFactor - ( Owner.LimitLegStretch * 0.975f );
                        Vector3 localOffset = hubFloorPos - leg._PreviousFinalIKPosForStability;
                        localOffset = Owner.ToRootLocalSpaceVec( localOffset );
                        if( localOffset.y > 0f ) localOffset.y = 0f;
                        localOffset.x *= -0.6f;
                        localOffset.z *= -0.6f;
                        stretchPreventerOffset += localOffset * Mathf.Clamp( diff * 3f, 0f, 0.5f );
                    }

                }

                if( Owner.StretchPreventerSpeed < 1f )
                {
                    float lerpSPD = Mathf.Lerp( 8f, 40f, Owner.StretchPreventerSpeed ) * Owner.DeltaTime;

                    if( stretched > 0f )
                        _stretchPreventerOff = Vector3.Lerp( _stretchPreventerOff, stretchPreventerOffset / stretched, lerpSPD );
                    else
                        _stretchPreventerOff = Vector3.Lerp( _stretchPreventerOff, Vector3.zero, lerpSPD * 0.7f );
                }
                else
                {
                    _stretchPreventerOff = stretchPreventerOffset;
                }

                return _stretchPreventerOff;
            }


            /// <summary> Push Local Space </summary>
            public Vector3 CalculateGlueMovePush()
            {
                Vector3 stabilityDiff = Vector3.zero;

                if( Owner.GlueBlend < 0.0001f ) return stabilityDiff;

                for( int l = 0; l < ChildLegs.Count; l++ )
                {
                    var leg = ChildLegs[l];

                    #region Attachement and alignment blending transitioning

                    if( ( leg.G_Attached || leg.G_DuringLegAdjustMovement ) )
                    {
                        if( leg.G_LastLegMoveDistanceFactor > leg.LegAnimatingSettings.DoStepAnimationOnDistanceFactor )
                            if( leg.G_GlueInternalTransition > 0f && leg.G_GlueInternalTransition < 1f )
                            {
                                if( leg.G_HandlerExecutingLegAnimationMode == EGlueMode.Idle )
                                {
                                    Vector3 footmiddleOff = leg.AnkleH.Bone.TransformVector( leg.AnkleToFeetEnd );
                                    Vector3 footLocalPos = Owner.ToRootLocalSpace( leg._PreviousFinalIKPosForStability + footmiddleOff );
                                    footLocalPos.z = -footLocalPos.z;

                                    float ev = Owner.BaseLegAnimating.PushHipsOnMoveCurve.Evaluate( leg.G_GlueInternalTransition );
                                    Vector3 legPush = -footLocalPos * ev * 1f;
                                    legPush.y -= ev * leg.G_LastLegMoveDistanceFactor * Owner.ScaleReferenceNoScale * 0.75f;

                                    Vector3 extraOffset;

                                    if( Owner.NormalizePush )
                                    {
                                        float normFactor = Mathf.Min( 1f, legPush.magnitude / ( Owner.ScaleReferenceNoScale * 0.33f ) );
                                        normFactor *= normFactor;
                                        extraOffset = legPush.normalized * Owner.ScaleReferenceNoScale * 0.33f * normFactor;
                                    }
                                    else
                                        extraOffset = legPush;

                                    extraOffset.y *= Owner.PushYBlend;

                                    stabilityDiff += extraOffset * leg.BlendWeight;

                                }
                            }
                    }

                    #endregion

                }

                return stabilityDiff;
            }




            /// <summary> Last applied height offset (with blending) </summary>
            public float _Hips_LastHipsOffset { get; private set; } = 0f;
            /// <summary> Not blended height offset </summary>
            public float _Hips_StepHeightAdjustOffset { get; private set; } = 0f;
            /// <summary> Extra offset to apply which is ignoring elastic muscle motion </summary>
            public Vector3 ExtraNonElasticOffset { get; internal set; }
            public Vector3 _PreHipsAdjustPosition { get; internal set; }

            float _sd_Hips_StepHeightAdjustOffset = 0f;
            void AnimateStepAdjustTo( float yOffset )
            {
                if( Owner.HipsHeightStepSpeed >= 1f )
                {
                    _Hips_StepHeightAdjustOffset = yOffset;
                    return;
                }

                float landingBoost = Owner.GetLandingBoost();

                // Leg height follow adjust hips
                if( Owner.HipsAdjustStyle == EHipsAdjustStyle.FollowLegHeight )
                    if( yOffset < _Hips_StepHeightAdjustOffset )
                    {
                        if( _h_lowestHitLeg != -1 )
                        {
                            Vector3 localPos = Owner.Legs[_h_lowestHitLeg]._PreviousFinalIKPos;
                            localPos = Owner.ToRootLocalSpace( localPos );
                            localPos.y -= Owner.ScaleReferenceNoScale * 0.325f;

                            if( localPos.y > yOffset )
                            {
                                yOffset = localPos.y;
                            }
                        }
                    }

                _Hips_StepHeightAdjustOffset = Mathf.SmoothDamp( _Hips_StepHeightAdjustOffset,
                    yOffset, ref _sd_Hips_StepHeightAdjustOffset,
                    Mathf.LerpUnclamped( 0.4f, 0.01f, landingBoost )
                    , 1000000f, Owner.DeltaTime );

                _h_lowestHitLeg = -1;
            }

            int _h_lowestHitLeg = -1;

            public float CalculateBodyAdjust()
            {
                _Hips_LastHipsOffset = 0f;

                if( Owner.HipsHeightStepBlend <= 0f ) return 0f;

                if( Owner.IsGrounded )
                {
                    Vector3 lowestHitLocal = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue );
                    Vector3 lowestHitLocalStepUp = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue );

                    // Finding lowest raycast hit in max body step down range
                    for( int l = 0; l < ChildLegs.Count; l++ )
                    {
                        var leg = ChildLegs[l];

                        if( leg.RaycastHitted == false ) continue;

                        Vector3 groundHit = leg.LastGroundHit.point;
                        groundHit = Owner.ToRootLocalSpace( groundHit );

                        if( groundHit.y <= 0f ) // Below Ground
                        {
                            if( -groundHit.y < Owner.BodyStepDown * Owner.ScaleReferenceNoScale )
                            {
                                if( groundHit.y < lowestHitLocal.y )
                                {
                                    lowestHitLocal = groundHit;
                                    _h_lowestHitLeg = l;
                                }
                            }
                        }
                        else // Above Ground
                        {
                            if( groundHit.y < Owner.MaxBodyStepUp * Owner.ScaleReferenceNoScale )
                            {
                                if( groundHit.y < lowestHitLocal.y ) lowestHitLocalStepUp = groundHit;
                            }
                        }

                    }

                    bool hipsAdjusted = false;
                    if( lowestHitLocal.x != float.MaxValue ) // Adjust hips down
                    {
                        if( Owner.BodyStepDown > 0f )
                            if( lowestHitLocal.y <= 0f )
                            {
                                AnimateStepAdjustTo( lowestHitLocal.y );
                                hipsAdjusted = true;
                            }
                    }

                    if( !hipsAdjusted ) // Adjust hips up
                    {
                        if( Owner.MaxBodyStepUp > 0f )
                            if( lowestHitLocalStepUp.x != float.MaxValue )
                            {
                                AnimateStepAdjustTo( lowestHitLocalStepUp.y );
                                hipsAdjusted = true;
                            }
                    }


                    if( !hipsAdjusted ) // Return to default hips pose
                    {
                        AnimateStepAdjustTo( 0f );
                    }
                }
                else
                {
                    AnimateStepAdjustTo( 0f );
                }

                float hipsWeight = Owner.HipsBlendWeight * Owner._MainBlend * Owner.IsGroundedBlend * Owner.RagdolledDisablerBlend;
                _Hips_LastHipsOffset = ( _Hips_StepHeightAdjustOffset * Owner.baseTransform.lossyScale.y ) * hipsWeight;
                return _Hips_LastHipsOffset;
            }



            Vector3 _reAdjustLocal = Vector3.zero;
            Vector3 _sd_readj = Vector3.zero;
            public Vector3 CalculateStretchReadjust()
            {
                Vector3 stretchReAdjust = Vector3.zero;

                for( int l = 0; l < ChildLegs.Count; l++ )
                {
                    var leg = ChildLegs[l];
                    Vector3 ikRefPos = leg._FinalIKPos - stretchReAdjust;
                    float legStretch = leg.IKProcessor.GetStretchValue( ikRefPos );

                    if( legStretch > Owner.LimitLegStretch )
                    {
                        Vector3 nonStretchedPos = leg.IKProcessor.GetNotStretchedPositionTowards( ikRefPos, Owner.LimitLegStretch );
                        Vector3 diff = ikRefPos - nonStretchedPos;
                        stretchReAdjust += diff;
                    }
                }

                stretchReAdjust = Owner.ToRootLocalSpaceVec( stretchReAdjust );
                _reAdjustLocal = Vector3.SmoothDamp( _reAdjustLocal, stretchReAdjust, ref _sd_readj, 0.1f, 10000000f, Owner.DeltaTime );
                return _reAdjustLocal;
            }





            Vector3 _pushSmoothed = Vector3.zero;
            Vector3 _sd_pushSmoothed = Vector3.zero;
            public Vector3 SmoothPushOffset( Vector3 pushLocalOffset, float pushDuration )
            {
                Owner.ValueTowards( ref _pushSmoothed, pushLocalOffset, ref _sd_pushSmoothed, pushDuration );
                return _pushSmoothed;
            }

            public Vector3 AnimateOffset( Vector3 hubOffset )
            {
                return hubOffset;
            }

        }
    }
}