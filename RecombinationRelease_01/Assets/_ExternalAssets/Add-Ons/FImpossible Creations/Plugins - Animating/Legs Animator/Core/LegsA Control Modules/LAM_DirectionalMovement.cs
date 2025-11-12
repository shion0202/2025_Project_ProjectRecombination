using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using FIMSpace.FEditor;
#endif
using UnityEngine;
using static FIMSpace.FProceduralAnimation.LegsAnimator;
using System.Linq;

namespace FIMSpace.FProceduralAnimation
{
    /// <summary>
    /// Examples class for customized controll over the Legs Animator IK Redirecting features
    /// </summary>
    [CreateAssetMenu(fileName = "LAM_DirectionalMovement", menuName = "FImpossible Creations/Legs Animator/Module - 360 Movement Animation", order = 2)]
    public class LAM_DirectionalMovement : LegsAnimatorControlModuleBase
    {

        #region Module Instance Variables

        LegsAnimatorCustomModuleHelper _useHelper = null;

        Variable HipsRedirVar { get { return _useHelper.RequestVariable("Redirect Hips", 0.8f); } }
        Variable _play_HipsRedir = null;

        Variable FeetRedirVar { get { return _useHelper.RequestVariable("Redirect Feet", 0.8f); } }
        Variable _play_FeetRedir = null;

        Variable KneesRedirVar { get { return _useHelper.RequestVariable("Redirect Knees", 0.4f); } }
        Variable _play_KneesRedir = null;

        Variable TrDurationVar { get { return _useHelper.RequestVariable("Transitions Duration", 0.25f); } }
        Variable _play_TrDur = null;

        Variable LimitRaiseVar { get { return _useHelper.RequestVariable("Limit Leg Raise", 0.1f); } }
        Variable _play_LimitRaise = null;

        Variable FixFeetVar { get { return _useHelper.RequestVariable("Fix Backward Feet", 1f); } }
        Variable _play_FixFeet = null;

        Variable AdjustStretchVar { get { return _useHelper.RequestVariable("Adjust Stretched", 0.2f); } }
        Variable _play_AdjustStretch = null;

        Variable RestoreSpineVar { get { return _useHelper.RequestVariable("Restore Spine", 0.5f); } }
        Variable _play_RestoreSpine = null;

        Variable ExtraSmootherVar { get { return _useHelper.RequestVariable("Extra Smoother", 0f); } }
        Variable _play_Smoother = null;

        Variable ReAdjVar { get { return _useHelper.RequestVariable("Re-adjust with hips offset", false); } }
        Variable _play_reAdj = null;

        //Variable UseRigidVar { get { return _useHelper.RequestVariable("Use Rigidbody For Dir", false); } }
        //Variable _play_UseRigid = null;
        Variable FadeOffInAirVar { get { return _useHelper.RequestVariable("Disable When Jumping", false); } }
        Variable _play_offInAir = null;

        Variable XDirAnimVarVar { get { return _useHelper.RequestVariable("Animator World X Dir", ""); } }
        int _hash_xDir = -1;
        Variable ZDirAnimVarVar { get { return _useHelper.RequestVariable("Animator World Z Dir", ""); } }
        int _hash_zDir = -1;

        #endregion


        #region Calculation Variables

        Vector3 _calc_WorldDir = Vector3.zero;
        Vector3 _calc_LocalDir = Vector3.zero;
        Quaternion _calc_LocalRotDir = Quaternion.identity;

        float _localTargetAngle = 0f;
        float _wrappedAngle = 0f;
        float _smoothedWrappedAngle = 0f;

        float _calc_smoothedTargetAngle = 0f;
        float _calc_angleDiffFactor = 0f;

        float _calc_toNegativeXProgress = 0f;
        internal float _calc_backAngleOff = 0f; // for later

        float _calc_sideFactorL = 0f;
        float _calc_sideFactorR = 0f;
        internal float _calc_sideFactor = 0f; // for later

        float _calc_deltaSpeed = 0f;
        float _calc_deltaSpeedSlow = 0f;

        float _var_raiseLimit = 0f;
        float _var_fixFeet = 0f;

        Vector3 _calc_hipsPositionOffsets = Vector3.zero;
        Vector3 _calc_hipsRotationOffsets = Vector3.zero;

        Vector3 _calc_hipsStretchOffset = Vector3.zero;
        Vector3 _sd_hipsStretchOff = Vector3.zero;

        Vector3 _calc_ikOff = Vector3.zero;


        List<LegRedirectHelper> legRedirectHelpers = null;

        [NonSerialized] public Transform SpineBone = null;

        #endregion


        /// <summary> Use it for custom multiply axis values for hips redirecting </summary>
        [NonSerialized] public Vector3 User_MultiplyHipsOffsets = Vector3.one;

        [System.Serializable]
        public class AnglesSetup
        {
            public Vector3 AnglesOn0 = new Vector3(0f, 0f, 0f);
            [Tooltip(" Hips rotations on reaching 45 angle movement")]
            public Vector3 AnglesOn45 = new Vector3(-10f, 14f, -5f);
            [Tooltip(" Hips rotations on reaching 90 angle movement")]
            public Vector3 AnglesOn90 = new Vector3(-7f, 40f, -3f);
            [Tooltip(" Hips rotations on reaching 135 angle movement")]
            public Vector3 AnglesOn135 = new Vector3(-8f, -25f, -4f);
            [Tooltip(" Hips rotations on reaching 180 angle movement")]
            public Vector3 AnglesOn180 = new Vector3(-20f, 0f, 0f);

            [Space(8)]
            public Vector3 HipsOffsetOn0 = new Vector3(0f, 0f, 0f);
            [Tooltip(" Hips position offset on reaching 45 angle movement")]
            public Vector3 HipsOffsetOn45 = new Vector3(-0.05f, 0f, -0.05f);
            [Tooltip(" Hips position offset on reaching 90 angle movement")]
            public Vector3 HipsOffsetOn90 = new Vector3(-0.1f, 0f, 0.05f);
            [Tooltip(" Hips position offset on reaching 135 angle movement")]
            public Vector3 HipsOffsetOn135 = new Vector3(-0.1f, 0f, 0.1f);
            [Tooltip(" Hips position offset on reaching 180 angle movement")]
            public Vector3 HipsOffsetOn180 = new Vector3(0f, 0.05f, 0.2f);

            [Space(8)]
            public Vector3 IKsOffsetOn0 = new Vector3(0f, 0f, 0f);
            [Tooltip(" Foot IK position offset on reaching 45 angle movement (x on left leg goes negative)")]
            public Vector3 IKsOffsetOn45 = new Vector3(0f, 0f, -0.04f);
            [Tooltip(" Foot IK position offset on reaching 90 angle movement (x on left leg goes negative)")]
            public Vector3 IKsOffsetOn90 = new Vector3(0f, 0f, -0.08f);
            [Tooltip(" Foot IK position offset on reaching 135 angle movement (x on left leg goes negative)")]
            public Vector3 IKsOffsetOn135 = new Vector3(0f, 0f, 0.08f);
            [Tooltip(" Foot IK position offset on reaching 180 angle movement (x on left leg goes negative)")]
            public Vector3 IKsOffsetOn180 = new Vector3(0f, 0f, 0f);
        }

        [FPD_Header("Angles setup to drive procedural animation")]
        public AnglesSetup Animation360Angles;

        /// <summary> Rotating hips on stretch adjustements, default is 30 </summary>
        [NonSerialized] public float User_StretchRotatorAnglePower = 30f;

        [NonSerialized] public float User_StretchPositionMultiplier = 1f;

        float _mainBlend = 1f;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _useHelper = helper;

            // Get variable references for quicker computing in playtime
            _play_HipsRedir = HipsRedirVar;
            _play_TrDur = TrDurationVar;
            _play_LimitRaise = LimitRaiseVar;
            _play_FeetRedir = FeetRedirVar;
            _play_KneesRedir = KneesRedirVar;
            _play_FixFeet = FixFeetVar;
            _play_AdjustStretch = AdjustStretchVar;
            _play_RestoreSpine = RestoreSpineVar;
            _play_Smoother = ExtraSmootherVar;
            //_play_UseRigid = UseRigidVar;
            _play_offInAir = FadeOffInAirVar;
            _play_reAdj = ReAdjVar;
            _wasUpdated = false;

            // Prepare leg redirecting helpers
            legRedirectHelpers = new List<LegRedirectHelper>();
            for (int i = 0; i < LA.Legs.Count; i++)
            {
                LegRedirectHelper lHelp = new LegRedirectHelper(this, LA.Legs[i]);
                legRedirectHelpers.Add(lHelp);
            }

            for (int i = 0; i < LA.Legs.Count; i++) // After generating helpers, assign Opposite leg helpers
            {
                if (LA.Legs[i].OppositeLegIndex < 0) continue;
                LegRedirectHelper lHelp = legRedirectHelpers[i];
                lHelp.oppositeHelper = legRedirectHelpers[LA.Legs[i].OppositeLegIndex];
            }


            if (SpineBone == null)
            {
                if (LA.Hips.childCount > 0)
                {
                    if (LA.Hips.childCount == 1) { SpineBone = LA.Hips.GetChild(0); }
                    else
                    {
                        for (int c = 0; c < LA.Hips.childCount; c++)
                        {
                            if (LA.Hips.GetChild(c).name.ToLower().Contains("spin")) { SpineBone = LA.Hips.GetChild(c); break; }
                        }

                        if (SpineBone == null) SpineBone = LA.Hips.GetChild(0);
                    }
                }
            }


            if (LA.Mecanim)
            {
                var xdirV = XDirAnimVarVar;
                if (!string.IsNullOrWhiteSpace(xdirV.GetString()))
                {
                    _hash_xDir = Animator.StringToHash(xdirV.GetString());
                    var zdirV = ZDirAnimVarVar;
                    _hash_zDir = Animator.StringToHash(zdirV.GetString());
                }
            }

        }

        bool _wasUpdated;
        public override void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _mainBlend = LA._MainBlend * ModuleBlend;
            if (_play_offInAir.GetBool()) _mainBlend *= LA.IsGroundedBlend;

            // Calculate base orientation for redirecting and handle smooth transitioning
            float trDurMul = _play_TrDur.GetFloat();

            if (_mainBlend < 0.001f) return;

            // There calculate transitioning values and other main calculations

            #region Define desired movement direction


            if (useOverridingDirection)
            {
                if (overrideDirectionFadeSpeed < 0.0001f) overrideDirectionBlend = 1f;
                else overrideDirectionBlend = Mathf.MoveTowards(overrideDirectionBlend, 1f, Owner.DeltaTime * overrideDirectionFadeSpeed);
            }
            else
            {
                if (overrideDirectionFadeSpeed < 0.0001f) overrideDirectionBlend = 0f;
                else overrideDirectionBlend = Mathf.MoveTowards(overrideDirectionBlend, 0f, Owner.DeltaTime * overrideDirectionFadeSpeed);
            }

            //bool usesRigid = false;
            //if (_play_UseRigid.GetBool())
            //{
            //    if (LA.Rigidbody != null)
            //    {
            //        usesRigid = true;
            //        _calc_WorldDir = LA.Rigidbody.velocity.normalized;
            //    }
            //}

            Vector3 _src_worldDir;
            if (_hash_zDir != -1)
            {
                _src_worldDir = new Vector3(LA.Mecanim.GetFloat(_hash_xDir), 0f, LA.Mecanim.GetFloat(_hash_zDir)).normalized;
            }
            else // Default use variable
            {
                _src_worldDir = LA.DesiredMovementDirection;
                _src_worldDir.y = 0f;
                if (_src_worldDir.magnitude < 0.1f) _src_worldDir = Vector3.zero;
            }

            _calc_WorldDir = _src_worldDir;

            if (overrideDirectionBlend > 0.0001f)
            {
                if (overrideDirectionBlend >= 1f)
                    _calc_WorldDir = overridingDirection;
                else
                    _calc_WorldDir = Vector3.Slerp(_calc_WorldDir, overridingDirection, overrideDirectionBlend);
            }

            #endregion

            _calc_LocalDir = LA.ToRootLocalSpaceVec(_calc_WorldDir);

            _var_raiseLimit = _play_LimitRaise.GetFloat();
            _var_fixFeet = _play_FixFeet.GetFloat();

            if (_calc_LocalDir.sqrMagnitude < 0.00001f) _localTargetAngle = 0f; // Reset Angle protection

            _localTargetAngle = FEngineering.GetAngleRad(_calc_LocalDir.x, _calc_LocalDir.z);

            float deltaMul;
            if (trDurMul <= 0f) deltaMul = 1000f;
            else deltaMul = 3f * Mathf.Lerp(5f, .5f, trDurMul / 0.6f);

            _calc_deltaSpeed = LA.DeltaTime * deltaMul;
            _calc_deltaSpeedSlow = LA.DeltaTime * (deltaMul * 0.6f);

            _calc_smoothedTargetAngle = Mathf.LerpAngle(_calc_smoothedTargetAngle, _localTargetAngle, _calc_deltaSpeedSlow);
            _calc_angleDiffFactor = Mathf.InverseLerp(0.0001f, 0.25f, Mathf.Abs((_localTargetAngle - _calc_smoothedTargetAngle) / Mathf.PI));
            _localTargetAngle *= Mathf.Rad2Deg;

            // +- 180 angle conversion
            _wrappedAngle = FormatAngleToPM180(_localTargetAngle);

            #region Smooth wrapped angle and keep correctness

            _smoothedWrappedAngle = Mathf.LerpAngle(_smoothedWrappedAngle, _wrappedAngle, _calc_deltaSpeed * 1.25f);
            _smoothedWrappedAngle = FormatAngleToPM180(_smoothedWrappedAngle);
            if (_smoothedWrappedAngle < -179.9f && _wrappedAngle > 0f) _smoothedWrappedAngle = -_smoothedWrappedAngle;
            if (_smoothedWrappedAngle > 179.9f && _wrappedAngle < 0f) _smoothedWrappedAngle = -_smoothedWrappedAngle;

            #endregion

            #region Side factors calculate

            float targetSFactor;
            float sideCalcAngle = _wrappedAngle;

            if (_wrappedAngle < 90f) targetSFactor = Mathf.InverseLerp(0f, 90f, sideCalcAngle);
            else targetSFactor = Mathf.InverseLerp(180f, 90f, sideCalcAngle);
            _calc_sideFactorR = Mathf.Lerp(_calc_sideFactorR, targetSFactor, _calc_deltaSpeed * 2f);

            if (_wrappedAngle > -90f) targetSFactor = Mathf.InverseLerp(0f, -90f, sideCalcAngle);
            else targetSFactor = Mathf.InverseLerp(-180f, -90f, sideCalcAngle);
            _calc_sideFactorL = Mathf.Lerp(_calc_sideFactorL, targetSFactor, _calc_deltaSpeed * 2f);

            if (_wrappedAngle < 0f) _calc_sideFactor = _calc_sideFactorL;
            else _calc_sideFactor = _calc_sideFactorR;

            #endregion

            #region Negative Progress (backwards movement)

            _calc_toNegativeXProgress = 0f;

            float negProgAngle = _wrappedAngle;
            if (negProgAngle < -90f)
                _calc_toNegativeXProgress = Mathf.InverseLerp(-90f, -135f, negProgAngle);
            else if (negProgAngle > 90f)
                _calc_toNegativeXProgress = Mathf.InverseLerp(90f, 135f, negProgAngle);

            #endregion


            // Provide orientation reference
            _calc_LocalRotDir = Quaternion.Euler(0f, _localTargetAngle, 0f);
            _wasUpdated = true;
        }


        public override void OnAfterAnimatorCaptureUpdate(LegsAnimatorCustomModuleHelper helper)
        {
            if (!_wasUpdated) return;
            float smoother = _play_Smoother.GetFloat() + 1;

            for (int l = 0; l < LA.Legs.Count; l++)
            {
                var leg = LA.Legs[l];

                // Apply target ik mods for each leg IK

                // Read base IK data
                Vector3 finalIKPos = leg._AnimatorEndBonePos;
                Vector3 local = LA.ToRootLocalSpace(finalIKPos);

                LegRedirectHelper lHelper = legRedirectHelpers[leg.PlaymodeIndex];
                /*Vector3 finalLocal = */
                lHelper.ComputeIKOffset(local, smoother); // Compute position with use of leg helper

                // Apply
                Vector3 targetPos = lHelper.LastComputedWorldSpaceLegPos;
                if (_mainBlend < 1f) targetPos = Vector3.LerpUnclamped(finalIKPos, targetPos, _mainBlend); // support blending

                Vector3 ikOff = _calc_ikOff;
                if (LA.Legs[l].Side == ELegSide.Left) ikOff.x = -ikOff.x;
                else if (LA.Legs[l].Side == ELegSide.Right) ikOff.z = -ikOff.z;

                ikOff = LA.RootToWorldSpaceVec(ikOff);
                targetPos += ikOff;

                // Override reference animator ankle position with computed one
                // so legs animator will treat new position as animator pose
                leg.OverrideAnimatorAnklePosition(targetPos);

                //leg.OverrideFinalIKPos(targetPos);
                //leg.OverrideControlPositionsWithCurrentIKState();
                //leg.OverrideSourceIKPos();
            }

        }


        #region Override Move Direction Switch Support

        Vector3 overridingDirection = Vector3.zero;
        bool useOverridingDirection = false;
        [NonSerialized] public float overrideDirectionFadeSpeed = 6f;
        float overrideDirectionBlend = 0f;
        /// <summary> If you want to force module to apply different legs direction than automatically calculated directions (useful when using root motion velocity animations) </summary>
        public void OverrideMoveDirection(Vector3? direction)
        {
            if (direction == null) useOverridingDirection = false;
            else
            {
                useOverridingDirection = true;
                overridingDirection = direction.Value;
            }
        }

        #endregion


        float _calc_lStretch = 0f;
        float _calc_rStretch = 0f;
        public override void OnLateUpdatePreApply(LegsAnimatorCustomModuleHelper helper)
        {
            if (!_wasUpdated) return;
            // Hips Adjustements Compute

            // Prepare factors
            float redirect = _play_HipsRedir.GetFloat();
            float footsRedir = _play_FeetRedir.GetFloat();
            float kneesRedir = _play_KneesRedir.GetFloat();
            float hipsStretchAdj = _play_AdjustStretch.GetFloat();
            float angleABS = Mathf.Abs(_wrappedAngle);
            float strafeBlendIn = Mathf.InverseLerp(0f, 45f, angleABS);

            // Analyze legs state for redirector animation

            #region Legs stretching check and adjustements


            float angle = _wrappedAngle;
            float angleFootOffset = angle; // Support run back 360 as -90 to 90

            if (footsRedir > 0f) // Remapping angle for foots rotation
            {
                if (angle < -90f)
                {
                    if (angle > -135f) angleFootOffset = Mathf.Lerp(-90f, 40f, Mathf.InverseLerp(-90f, -135f, angle));
                    else angleFootOffset = Mathf.Lerp(40f, 0f, Mathf.InverseLerp(-135f, -180f, angle));
                }
                else if (angle > 90f)
                {
                    if (angle < 135) angleFootOffset = Mathf.Lerp(90f, -40f, Mathf.InverseLerp(90f, 135f, angle));
                    else angleFootOffset = Mathf.Lerp(-40f, 0, Mathf.InverseLerp(135f, 180f, angle));
                }
            }

            Vector3 extraHipsOffset = Vector3.zero;
            float lStretch = 0f;
            float rStretch = 0f;

            float backTo135 = 0f;
            if (angleABS > 135f) backTo135 = Mathf.InverseLerp(180f, 135f, angleABS);
            else backTo135 = Mathf.InverseLerp(90f, 135f, angleABS);
            backTo135 = Mathf.Lerp(1f, -0.5f, backTo135);

            for (int i = 0; i < LA.Legs.Count; i++)
            {
                var leg = LA.Legs[i];

                // Offset Y foots rotation to match movement direction
                if (footsRedir > 0f)
                {
                    float footYRot = angleFootOffset;
                    float redir = 1f - footsRedir; redir = redir * redir * redir;
                    footYRot *= 1f - redir;

                    Quaternion rot = Quaternion.AngleAxis(footYRot * 0.8f * _mainBlend, LA.BaseTransform.up);
                    rot = legRedirectHelpers[i].FootRedirectSmoother(rot); // Smooth rotation offset

                    leg.OverrideFinalIKRot(rot * leg.GetFinalIKRot());
                }


                // Knees redirection extra rotation
                if (kneesRedir > 0f)
                {
                    if (leg.Side == ELegSide.Left)
                    {
                        leg.IKProcessor.StartBoneRotationOffset = Quaternion.Euler(0f, -_calc_sideFactorR * Mathf.Min(35f, 50f * kneesRedir) * backTo135, 0f);
                    }
                    else if (leg.Side == ELegSide.Right)
                    {
                        leg.IKProcessor.StartBoneRotationOffset = Quaternion.Euler(0f, -_calc_sideFactorL * Mathf.Min(35f, 50f * kneesRedir) * backTo135, 0f);
                    }
                }
                else leg.IKProcessor.StartBoneRotationOffset = Quaternion.identity;


                // Leg stretch detecting to move hips towards non-stretched pose
                if (hipsStretchAdj > 0.01f)
                {
                    float stretch = leg.IKProcessor.GetStretchValue(legRedirectHelpers[i].LastComputedWorldSpaceLegPos);

                    if (stretch > 0.9f)
                    {
                        float blendIn = Mathf.InverseLerp(0.9f, 1.125f, stretch);
                        if (leg.Side == ELegSide.Left) lStretch += blendIn;
                        else rStretch += blendIn;

                        Vector3 diff = leg._PreviousFinalIKPos - LA.BaseTransform.position;

                        diff = LA.ToRootLocalSpaceVec(diff);
                        diff.y *= -0.8f;
                        diff = LA.RootToWorldSpaceVec(diff);

                        extraHipsOffset += diff * (blendIn * 1f);
                        //if (leg.Side == ELegSide.Left) stretchLR -= stretchDiff;
                        //else if (leg.Side == ELegSide.Right) stretchLR += stretchDiff;
                    }
                }

            }

            #endregion


            #region Compute angle 0 - 45 - 90 - 135 - 180 parts factors

            Vector3 targetHipsRots = Animation360Angles.AnglesOn0;
            Vector3 targetHipsPosOff = Animation360Angles.HipsOffsetOn0;
            Vector3 targetIKPosOff = Animation360Angles.IKsOffsetOn0;

            if (angleABS > 0f)
            {
                if (angleABS < 90f) // To 45
                {
                    float sideFactor = InverseLerpDoubleSide(0f, 45f, angleABS, 90f);
                    LerpIt(ref targetHipsRots, Animation360Angles.AnglesOn45, sideFactor);
                    LerpIt(ref targetHipsPosOff, Animation360Angles.HipsOffsetOn45, sideFactor);
                    LerpIt(ref targetIKPosOff, Animation360Angles.IKsOffsetOn45, sideFactor);
                }

                if (angleABS > 45f && angleABS < 135f) // To 90
                {
                    float sideFactor = InverseLerpDoubleSide(45f, 90f, angleABS, 135f);
                    LerpIt(ref targetHipsRots, Animation360Angles.AnglesOn90, sideFactor);
                    LerpIt(ref targetHipsPosOff, Animation360Angles.HipsOffsetOn90, sideFactor);
                    LerpIt(ref targetIKPosOff, Animation360Angles.IKsOffsetOn90, sideFactor);
                }

                if (angleABS > 90f) // To 135
                {
                    float sideFactor = InverseLerpDoubleSide(90f, 135f, angleABS, 180f);
                    LerpIt(ref targetHipsRots, Animation360Angles.AnglesOn135, sideFactor);
                    LerpIt(ref targetHipsPosOff, Animation360Angles.HipsOffsetOn135, sideFactor);
                    LerpIt(ref targetIKPosOff, Animation360Angles.IKsOffsetOn135, sideFactor);
                }

                if (angleABS > 135f) // To 180
                {
                    float sideFactor = Mathf.InverseLerp(135f, 180f, angleABS);
                    LerpIt(ref targetHipsRots, Animation360Angles.AnglesOn180, sideFactor);
                    LerpIt(ref targetHipsPosOff, Animation360Angles.HipsOffsetOn180, sideFactor);
                    LerpIt(ref targetIKPosOff, Animation360Angles.IKsOffsetOn180, sideFactor);
                }

                if (_wrappedAngle < 0f)
                {
                    targetHipsRots.y = -targetHipsRots.y;
                    targetHipsRots.z = -targetHipsRots.z;
                    targetHipsPosOff.x = -targetHipsPosOff.x;
                    targetIKPosOff.z = -targetIKPosOff.z;
                }

                targetHipsRots *= redirect;
                targetHipsPosOff *= 0.7f * redirect;
                targetHipsRots = Vector3.Scale(targetHipsRots, User_MultiplyHipsOffsets);
            }

            targetIKPosOff *= redirect * _mainBlend * LA.ScaleReference;
            _calc_ikOff = targetIKPosOff;

            float hipsRotStretchMul = 0.25f + hipsStretchAdj * 0.75f;

            _calc_lStretch = Mathf.Lerp(_calc_lStretch, lStretch, _calc_deltaSpeed);
            _calc_rStretch = Mathf.Lerp(_calc_rStretch, rStretch, _calc_deltaSpeed);

            targetHipsRots.y -= _calc_lStretch * User_StretchRotatorAnglePower * redirect * hipsRotStretchMul;
            targetHipsRots.y += _calc_rStretch * User_StretchRotatorAnglePower * redirect * hipsRotStretchMul;

            _calc_hipsRotationOffsets.x = Mathf.LerpAngle(_calc_hipsRotationOffsets.x, targetHipsRots.x, _calc_deltaSpeed);
            _calc_hipsRotationOffsets.y = Mathf.LerpAngle(_calc_hipsRotationOffsets.y, targetHipsRots.y, _calc_deltaSpeed);
            _calc_hipsRotationOffsets.z = Mathf.LerpAngle(_calc_hipsRotationOffsets.z, targetHipsRots.z, _calc_deltaSpeed);

            _calc_hipsPositionOffsets = Vector3.Lerp(_calc_hipsPositionOffsets, targetHipsPosOff, _calc_deltaSpeed);
            targetHipsPosOff *= User_StretchPositionMultiplier;

            #endregion

            Quaternion preSpineRot = Quaternion.identity;
            float restoreSpn = _play_RestoreSpine.GetFloat();
            if (SpineBone != null) preSpineRot = SpineBone.rotation; else restoreSpn = 0f;

            // Hips Rotation Adjust Apply
            Quaternion hipsRotTo = Quaternion.AngleAxis(_calc_hipsRotationOffsets.y * _mainBlend, LA.BaseTransform.up);
            hipsRotTo *= Quaternion.AngleAxis(_calc_hipsRotationOffsets.z * _mainBlend, LA.BaseTransform.forward);
            hipsRotTo *= Quaternion.AngleAxis(_calc_hipsRotationOffsets.x * _mainBlend, LA.BaseTransform.right);
            LA.Hips.rotation = hipsRotTo * LA.Hips.rotation;

            if (restoreSpn > 0f) SpineBone.rotation = Quaternion.Slerp(SpineBone.rotation, preSpineRot, Mathf.Lerp(1f, restoreSpn, _mainBlend));

            _calc_hipsStretchOffset = Vector3.SmoothDamp(_calc_hipsStretchOffset, strafeBlendIn * extraHipsOffset, ref _sd_hipsStretchOff, 0.2f + 0.3f * _play_TrDur.GetFloat(), 100000f, LA.DeltaTime);

            // Hips Position Adjust Apply
            Vector3 finalHipsOffset = LA.RootToWorldSpaceVec(_calc_hipsPositionOffsets * 0.5f * LA.ScaleReference) * _mainBlend;
            LA._Hips_Modules_ExtraWOffset += finalHipsOffset + (_calc_hipsStretchOffset * hipsStretchAdj * _mainBlend);

            if (_play_reAdj.GetBool())
                for (int l = 0; l < LA.Legs.Count; l++) // Re-adjust feet ik position to offsetted hips
                {
                    //Vector3 ikOff = targetIKPosOff;
                    //if (LA.Legs[l].Side == ELegSide.Left) ikOff.x = -ikOff.x;
                    //ikOff = LA.RootToWorldSpaceVec(ikOff);
                    LA.Legs[l].OverrideFinalIKPos(LA.Legs[l].GetFinalIKPos() - finalHipsOffset);
                }
        }

        public override void OnPostLateUpdate(LegsAnimatorCustomModuleHelper helper)
        {
            if (_mainBlend < 0.001f) return;
            if (!_wasUpdated) return;

            // Foots back run rotation fix
            if (_var_fixFeet > 0f)
            {
                for (int i = 0; i < LA.Legs.Count; i++)
                {
                    var leg = LA.Legs[i];
                    Quaternion ikRot = leg.IKProcessor.EndIKBone.transform.rotation;
                    Quaternion newRot = leg.IKProcessor.EndIKBone.transform.parent.rotation * leg.IKProcessor.EndIKBone.InitialLocalRotation;

                    newRot = Quaternion.LerpUnclamped(ikRot, newRot, (1f - leg.A_AligningHelperBlend) * (_var_fixFeet) * LA.IsMovingBlend * _calc_toNegativeXProgress);
                    leg.IKProcessor.EndIKBone.transform.rotation = newRot;
                }
            }
        }

        // Class for smooth calculating IK redirecting for a single leg
        class LegRedirectHelper
        {

            #region Base Variables

            LAM_DirectionalMovement parent;
            LegsAnimator.Leg leg;
            internal LegRedirectHelper oppositeHelper = null;

            public LegRedirectHelper(LAM_DirectionalMovement parent, LegsAnimator.Leg leg)
            {
                this.parent = parent;
                this.leg = leg;

                LastComputedWorldSpaceLegPos = leg.BoneEnd.position;
                computedPosLocal = leg.Owner.ToRootLocalSpace(LastComputedWorldSpaceLegPos);
            }

            LegsAnimator LA { get { return parent.LA; } }

            public Vector3 LastComputedWorldSpaceLegPos { get; private set; }

            #endregion


            #region Main Calculations for a single leg

            Vector3 computedPosLocal = Vector3.zero;

            public Vector3 ComputeIKOffset(Vector3 localPos, float smoother = 1f)
            {
                float trDurMul = parent._play_TrDur.GetFloat();
                Vector3 targetLPos = parent._calc_LocalRotDir * localPos;

                float diff = Vector3.Magnitude(targetLPos - computedPosLocal);
                float diffNormalized = diff / leg.Owner.ScaleReferenceNoScale;

                float diffDelayer = 0f;
                if (diff > 0.2f)
                {
                    diffDelayer = Mathf.InverseLerp(0.2f, 1f, diff);
                    diffDelayer *= 0.1f;

                    if (trDurMul < 0.1f) diffDelayer *= trDurMul / 0.1f;
                }

                // Negative animation support
                if (parent._calc_toNegativeXProgress > 0f)
                {
                    Vector3 negativeLocalPos = localPos;
                    negativeLocalPos.x *= -1f;
                    Vector3 targetLPosNegative = parent._calc_LocalRotDir * negativeLocalPos;
                    targetLPos = Vector3.Lerp(targetLPos, targetLPosNegative, parent._calc_toNegativeXProgress);
                }

                float diffMargin = 0f;
                if (smoother >= 3f)
                {
                }
                else if (smoother > 0f)
                {
                    if (diffNormalized < 0.1f / smoother)
                        computedPosLocal = targetLPos;
                    else
                        diffMargin = Mathf.InverseLerp(1.5f * smoother, 0.1f / smoother, diffNormalized) * 6f;

                    diffMargin = Mathf.Max(0f, diffMargin);
                }
                else
                {
                    computedPosLocal = targetLPos;
                }

                computedPosLocal = Vector3.Lerp(computedPosLocal, targetLPos, LA.DeltaTime * (Mathf.Lerp(20f, 4f, (parent._calc_angleDiffFactor * trDurMul + diffDelayer) * 1.5f) + diffMargin));
                computedPosLocal.y = Mathf.Lerp(computedPosLocal.y, targetLPos.y, 0.5f);

                // Leg raise limiting on strafes and on backwards
                if (parent._var_raiseLimit > 0f)
                {
                    float floorY = leg.C_AnimatedAnkleFlatHeight;
                    float toFloorProgr = 0f;

                    if (leg.Side == ELegSide.Left) toFloorProgr = Mathf.Lerp(0f, 0.5f, parent._calc_sideFactorR);
                    else if (leg.Side == ELegSide.Right) toFloorProgr = Mathf.Lerp(0f, 0.5f, parent._calc_sideFactorL);

                    if (parent._calc_toNegativeXProgress > 0f)
                    {
                        toFloorProgr = Mathf.Lerp(toFloorProgr, 1f, parent._calc_toNegativeXProgress);
                    }

                    computedPosLocal.y = Mathf.Lerp(computedPosLocal.y, floorY, toFloorProgr * parent._var_raiseLimit);
                }

                LastComputedWorldSpaceLegPos = LA.RootToWorldSpace(computedPosLocal);

                return computedPosLocal;
            }

            Quaternion _footRedirCache = Quaternion.identity;
            internal Quaternion FootRedirectSmoother(Quaternion target)
            {
                _footRedirCache = Quaternion.Lerp(_footRedirCache, target, parent._calc_deltaSpeedSlow);
                return _footRedirCache;
            }

            #endregion

        }



        #region Utilities

        public static float FormatAngleToPM180(float angle)
        {
            float wrappedAngle = angle % 360f;
            if (wrappedAngle > 180) wrappedAngle -= 360;
            if (wrappedAngle < -180) wrappedAngle += 360;
            return wrappedAngle;
        }


        static float InverseLerpDoubleSide(float from, float to, float t, float toRange)
        {
            if (t > to)
                return Mathf.InverseLerp(toRange, to, t);
            else
                return Mathf.InverseLerp(from, to, t);
        }


        static void LerpIt(ref Vector3 val, Vector3 to, float t)
        {
            val = Vector3.LerpUnclamped(val, to, t);
        }

        #endregion


        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_OnSceneGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if (!Initialized) return;
            //UnityEditor.Handles.Label(LA.Hips.position, "extra = " + extraOff);
            //UnityEditor.Handles.Label(LA.Hips.position, "Local Dir = " + _calc_LocalRotDir.eulerAngles + "\nAngle: " + _localTargetAngle + "\nLocal: " + _calc_LocalDir + "\nWorld: " + _calc_WorldDir);
        }

        [HideInInspector] public bool InfoDisplay = true;

        public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            _useHelper = helper;

            if (InfoDisplay)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(FGUI_Resources.GUIC_Info, FGUI_Resources.ButtonStyle, GUILayout.Width(26))) InfoDisplay = !InfoDisplay; GUILayout.Space(4);
                EditorGUILayout.HelpBox("Reading Legs Animator's .MovementDirection variable to drive this module.By default it will use rigidbody velocity to do it, so you don't need to code anything.\nBut if you need to drive direction manually, Use legsAnim.User_SetDesiredMovementDirection... or use unity Animator variables (assign animator under Extra/Control and check bottom of this module GUI)", UnityEditor.MessageType.None);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            if (!InfoDisplay)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(FGUI_Resources.GUIC_Info, EditorStyles.label, GUILayout.Width(22))) InfoDisplay = !InfoDisplay;
            }

            EditorGUILayout.LabelField("Redirecting Legs IKs to generate 360 degrees movement\nautomatically, with use of single walk/run animation", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(24));

            if (!InfoDisplay) EditorGUILayout.EndHorizontal();


            GUILayout.Space(4);

            LegsAnimator.Variable hipsRedirectV = HipsRedirVar;
            if (!hipsRedirectV.TooltipAssigned) hipsRedirectV.AssignTooltip("Overall blend for hips rotation / position adjustement on different movement angles.");
            hipsRedirectV.SetMinMaxSlider(0f, 1f);
            hipsRedirectV.Editor_DisplayVariableGUI();

            LegsAnimator.Variable footRedirectV = FeetRedirVar;
            if (!footRedirectV.TooltipAssigned) footRedirectV.AssignTooltip("Rotating feet towards desired movement direction to match it.");
            footRedirectV.SetMinMaxSlider(0f, 1f);
            footRedirectV.Editor_DisplayVariableGUI();

            LegsAnimator.Variable kneesRedirV = KneesRedirVar;
            if (kneesRedirV.TooltipAssigned) kneesRedirV.AssignTooltip("Adjusting IK knees bend direction to match movement direction.");
            kneesRedirV.SetMinMaxSlider(0f, 1f);
            kneesRedirV.Editor_DisplayVariableGUI();

            GUILayout.Space(4);
            LegsAnimator.Variable limitLegRaiseV = LimitRaiseVar;
            if (!limitLegRaiseV.TooltipAssigned) limitLegRaiseV.AssignTooltip("Limiting how hight legs should be raised. It can be helpful when some running animations tends to raise legs too high on running backwards.");
            limitLegRaiseV.SetMinMaxSlider(0f, 0.3f);
            limitLegRaiseV.Editor_DisplayVariableGUI();

            FGUI_Inspector.DrawUILineCommon();

            LegsAnimator.Variable durationV = TrDurationVar;
            if (!durationV.TooltipAssigned) durationV.AssignTooltip("How rapidly the procedural adjustements should be executed.");
            durationV.SetMinMaxSlider(0f, 0.6f);
            durationV.Editor_DisplayVariableGUI();


            //LegsAnimator.Variable testVar = helper.RequestVariable("Debug", Vector3.zero);
            //testVar.VariableType = Variable.EVariableType.Vector3;
            //testVar.Editor_DisplayVariableGUI();

            //LegsAnimator.Variable testVar2 = helper.RequestVariable("Debug2", Vector3.zero);
            //testVar2.VariableType = Variable.EVariableType.Vector3;
            //testVar2.Editor_DisplayVariableGUI();

            GUILayout.Space(4);
            LegsAnimator.Variable fixFeetV = FixFeetVar;
            if (!fixFeetV.TooltipAssigned) fixFeetV.AssignTooltip("Fixing feet rotation which happens during running backwards.");
            fixFeetV.SetMinMaxSlider(0f, 1f);
            fixFeetV.Editor_DisplayVariableGUI();

            LegsAnimator.Variable adjStrV = AdjustStretchVar;
            if (!adjStrV.TooltipAssigned) adjStrV.AssignTooltip("Adjusting hips position and rotation when legs redirecting causes too big offset of feet from the hips.");
            adjStrV.SetMinMaxSlider(0f, 1f);
            adjStrV.Editor_DisplayVariableGUI();

            LegsAnimator.Variable restoreSpn = RestoreSpineVar;
            if (!restoreSpn.TooltipAssigned) restoreSpn.AssignTooltip("Restoring spine rotation which is rotated by the hips rotation adjuster, to face head forward instead of desired direction.");
            restoreSpn.SetMinMaxSlider(0f, 1f);
            restoreSpn.Editor_DisplayVariableGUI();

            LegsAnimator.Variable smoothV = ExtraSmootherVar;
            if (!smoothV.TooltipAssigned) smoothV.AssignTooltip("Applying extra smoothing to the leg motion.");
            smoothV.SetMinMaxSlider(-1f, 2f);
            smoothV.Editor_DisplayVariableGUI();

            LegsAnimator.Variable reAdjIK = ReAdjVar;
            if (!reAdjIK.TooltipAssigned) reAdjIK.AssignTooltip("Re-adjusting resulting feet ik positions with the hips offset. Can improve hips offset feeling but can cause minimalistic feet jitters during movement gluing.");
            reAdjIK.Editor_DisplayVariableGUI();

            GUILayout.Space(4);

            var la = helper.Parent;
            if (!la) return;

            //bool usingRigid = false;
            //if (la.Rigidbody != null)
            //{
            //    LegsAnimator.Variable rigidV = UseRigidVar;
            //    if (rigidV.Tooltip == "") rigidV.AssignTooltip("Using attached rigidbody velocity to define current desired movement direction for the module's algorithm. (no need for coding!)";
            //    rigidV.Editor_DisplayVariableGUI();
            //    usingRigid = rigidV.GetBool();
            //}

            FadeOffInAirVar.Editor_DisplayVariableGUI();

            //if (!usingRigid)

            bool usingAnimParams = false;

            if (la.Mecanim != null)
            {
                if (Application.isPlaying) GUI.enabled = false;
                //EditorGUILayout.HelpBox("You can use Animator's variable to drive the module movement direction", MessageType.None);

                LegsAnimator.Variable xDirV = XDirAnimVarVar;
                if (!xDirV.TooltipAssigned) xDirV.AssignTooltip("(Optional) Using unity animator's variable to define X world direction for this module's algorithm. (no need for Legs Animator module access through code)");

                if (string.IsNullOrEmpty(xDirV.GetString()))
                    GUI.color = new Color(1f, 1f, 1f, 0.4f);

                xDirV.Editor_DisplayVariableGUI();

                if (xDirV.GetString() != "")
                {
                    usingAnimParams = true;
                    LegsAnimator.Variable zDirV = ZDirAnimVarVar;
                    if (!zDirV.TooltipAssigned) zDirV.AssignTooltip("(Optional) Using unity animator's variable to define Z world direction for this module's algorithm.");
                    zDirV.Editor_DisplayVariableGUI();
                }

                GUI.color = Color.white;
            }

            if (!usingAnimParams)
            {
                if (la.Rigidbody)
                    EditorGUILayout.HelpBox("Module will use rigidbody velocity to drive legs direction", UnityEditor.MessageType.None);
                else
                    EditorGUILayout.HelpBox("You can assign 'Rigidbody' under Extra/Control to drive legs direction automatically! Or use legsAnimator.User_SetDesiredMovementDirection...", UnityEditor.MessageType.None);
            }

            LAM_DirectionalMovement dirMovPlaymode = helper.PlaymodeModule as LAM_DirectionalMovement;
            if (dirMovPlaymode == null) return;
            if (dirMovPlaymode._wasUpdated == false) return;

            EditorGUILayout.LabelField("Dir: " + dirMovPlaymode._localTargetAngle, EditorStyles.centeredGreyMiniLabel);

            //if (Application.isPlaying) return;

            //FGUI_Inspector.DrawUILineCommon(4);

            //if (GUILayout.Button("Go to module file for specific animation settings!"))
            //{
            //    Selection.activeObject = helper.ModuleReference;
            //}

            //EditorGUILayout.HelpBox("Some settings needs to be kept inside module preset file - not in legs animator module displayer.\nIf you need specific redirecting settings for different characters, you will need to create few files of Redirect Module!", MessageType.None);
        }


#endif
        #endregion



        #region Inspector Editor Class Ineritance
#if UNITY_EDITOR

        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor(typeof(LAM_DirectionalMovement))]
        public class LAM_DirectionalMovementEditor : LegsAnimatorControlModuleBaseEditor
        {
        }

#endif
        #endregion


    }
}