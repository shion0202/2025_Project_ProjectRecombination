using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            public FimpIK_Limb IKProcessor { get; private set; }

            public void IK_Initialize(bool generateNew = true)
            {
                if (generateNew) IKProcessor = new FimpIK_Limb();

                if (BoneFeet) IKProcessor.SetLegWithFeet(BoneStart, BoneMid, BoneEnd, BoneFeet);
                else IKProcessor.SetBones(BoneStart, BoneMid, BoneEnd);

                IKProcessor.Init(Owner.BaseTransform);
                IKProcessor.UseEndBoneMapping = false;
                IKProcessor.IKWeight = 1f;
                IKProcessor.IKPositionWeight = 1f;
                IKProcessor.FootRotationWeight = 1f;
                IKProcessor.ManualHintPositionWeight = 0f;
                IKProcessor.FeetStretchSensitivity = 0.9f;
                IKProcessor.FeetFadeQuicker = 1.1f;
                IKProcessor.FeetStretchLimit = 0.8f;

                IKProcessor.HumanoidAnimator = Owner.Mecanim;
                IKProcessor.IsRight = (Side == ELegSide.Right);

                _FinalIKPos = IKProcessor.EndIKBone.transform.position;
                _PreviousFinalIKPos = _FinalIKPos;
                _PreviousFinalIKPosForStability = _FinalIKPos;
                _PreviousFinalIKPosRootLocal = ToRootLocalSpace( _FinalIKPos );

                IKProcessor.IKTargetPosition = _FinalIKPos;
                IKProcessor.IKTargetRotation = _FinalIKRot;
            }

            public void AssignCustomIKProcessor(FimpIK_Limb ik)
            {
                IKProcessor = ik;
                IK_Initialize(false);
            }

            /// <summary> If not using IK multiplicator it's simply _SourceIKPos </summary>
            Vector3 _SourceIKPosUnchangedY;
            Vector3 _SourceIKPos;
            public Vector3 _FinalIKPos;

            Quaternion _SourceIKRot;
            Quaternion _FinalIKRot;

            //bool customOverwriteIKPositions = false;
            bool customOverwritingIKPos = false;
            Vector3 customOverwritePos = Vector3.zero;
            public void OverrideTargetIKPosition(Vector3? targetIKPos)
            {
                if (targetIKPos == null)
                {
                    customOverwritingIKPos = false;
                }
                else
                {
                    customOverwritingIKPos = true;
                    customOverwritePos = targetIKPos.Value;
                }
            }

            bool customOverwritingIKRot = false;
            Quaternion customOverwriteRot = Quaternion.identity;
            public void OverrideTargetIKRotation(Quaternion? targetIKRot)
            {
                if (targetIKRot == null)
                {
                    if (customOverwritingIKRot == true) IKProcessor.FootRotationWeight = 1f;
                    customOverwritingIKRot = false;
                }
                else
                {
                    customOverwritingIKRot = true;
                    customOverwriteRot = targetIKRot.Value;
                }
            }

            public void OverrideFinalIKPos(Vector3 pos) { _FinalIKPos = pos; }
            public void OverrideFinalAndSourceIKPos(Vector3 pos) { _FinalIKPos = pos; _SourceIKPos = pos; }
            public Vector3 GetFinalIKPos() { return _FinalIKPos; }
            public Vector3 GetSourceIKPos() { return _SourceIKPos; }
            public Quaternion GetFinalIKRot() { return _FinalIKRot; }
            public Quaternion GetSourceIKRot() { return _SourceIKRot; }
            public void OverrideFinalIKRot(Quaternion rot) { _FinalIKRot = rot; }


            public Vector3 _PreviousFinalIKPos { get; private set; }
            public Vector3 _PreviousFinalIKPosRootLocal { get; private set; }
            public Vector3 _PreviousFinalIKPosForStability { get; private set; }
            public Quaternion _PreviousFinalIKRot { get; private set; }
            public Vector3 _AnimatorStartBonePos { get; private set; }
            public Vector3 _AnimatorMidBonePos { get; private set; }
            public Vector3 _AnimatorEndBonePos { get; private set; }

            bool _wasFixedCalibrateAnimationCaptured = false;
            public Quaternion _AnimatorStartBoneLocRot { get; private set; }
            public Quaternion _AnimatorMidBoneLocRot { get; private set; }
            public Quaternion _AnimatorEndBoneLocRot { get; private set; }

            bool _wasGrounded = true;
            Vector3 _ungroundLocalIKCache;

            /// <summary>
            /// Should be called after hips adjustements
            /// </summary>
            public void IK_PreUpdate()
            {
                IKProcessor.CallPreCalibrate = Owner.Calibrate == ECalibrateMode.Calibrate;

                #region Handling unground fade (return;)

                if (Owner.IsGrounded == false)
                {
                    if (_wasGrounded)
                    {
                        _ungroundLocalIKCache = ToRootLocalSpace(_PreviousFinalIKPos);
                        _wasGrounded = false;
                    }

                    _SourceIKPos = RootSpaceToWorld(_ungroundLocalIKCache);
                    _SourceIKPos = Vector3.Lerp(_SourceIKPos, IKProcessor.EndIKBone.transform.position, 1f - Owner.IsGroundedBlend);
                    _ungroundLocalIKCache = ToRootLocalSpace(_SourceIKPos);
                    _SourceIKRot = BoneEnd.rotation;// IKProcessor.EndIKBone.transform.rotation;
                    _SourceIKPosUnchangedY = _SourceIKPos;
                    _FinalIKPos = _SourceIKPos;
                    _FinalIKRot = _SourceIKRot;

                    return;
                }
                else
                {
                    _wasGrounded = true;
                }

                #endregion

                //IKProcessor.RefreshAnimatorCoords();
                //_SourceIKPos = _AnimtorEndBonePos + Owner.Up * Owner._Hips_LastHipsOffset;

                if (!_overwrittenSourceIKPos)
                    _SourceIKPos = IKProcessor.EndIKBone.transform.position;
                else
                    _overwrittenSourceIKPos = false;

                _SourceIKRot = BoneEnd.rotation;// IKProcessor.EndIKBone.transform.rotation;

                _SourceIKPosUnchangedY = _SourceIKPos;
                //if (ParentHub != Owner.HipsSetup) _SourceIKPosUnchangedY += Owner.Up * ParentHub._Hips_StepHeightAdjustOffset;

                _FinalIKPos = _SourceIKPos;
                _FinalIKRot = _SourceIKRot;
            }

            public void IK_PostUpdate()
            {
                if (customOverwritingIKPos) // Custom IK position follow implementation
                {
                    _FinalIKPos = customOverwritePos;

                    if (customOverwritingIKRot)
                    {
                        IKProcessor.FootRotationWeight = 1f;
                        _FinalIKRot = customOverwriteRot;
                    }
                    else
                    {
                        IKProcessor.FootRotationWeight = 0f;
                    }
                }
                else
                {
                    if (G_LegAnimation.LegAdjustementFootAngleOffset != 0f || FootPitchOffset != 0f)
                    {
                        _FinalIKRot = Quaternion.AngleAxis(G_LegAnimation.LegAdjustementFootAngleOffset + FootPitchOffset, _SourceIKRot * AnkleIK.right) * _FinalIKRot;
                    }
                }

                // NaN protection
                if ( float.IsNaN( _FinalIKPos.x) || float.IsNaN( _FinalIKPos.y ) || float.IsNaN( _FinalIKPos.z ) )
                {
                    Reset();
                    _FinalIKPos = RootSpaceToWorld( InitialPosInRootSpace );
                    Gluing_Init();
                }

                _PreviousFinalIKPosForStability = _FinalIKPos;

                IKProcessor.IKTargetPosition = _FinalIKPos;
                IKProcessor.IKTargetRotation = _FinalIKRot;

                //if ( ApplyIKTo ) ApplyIKTo.position = _FinalIKPos;

                if (IKProcessor.IKWeight > 0f)
                {
                    if (!Owner.UseCustomIK) if (LegStretchLimit < 1.1f) IKProcessor.ApplyMaxStretchingPreprocessing(LegStretchLimit, 3f);

                    ExtraIKPostProcessingApply();

                    if (!Owner.UseCustomIK) IKProcessor.Update();
                }

                _PreviousFinalIKPos = IKProcessor.IKTargetPosition;
                _PreviousFinalIKPosRootLocal = ToRootLocalSpace( _PreviousFinalIKPos );

                if (Owner.AnimateFeet) _PreviousFinalIKRot = IKProcessor.IKTargetRotation;

                //UnityEngine.Debug.DrawRay(IKProcessor.IKTargetPosition, IKProcessor.IKTargetRotation * Vector3.forward, Color.green, 0.01f);
            }

            public void IK_UpdateParamsBase()
            {
                IKProcessor.IKWeight = Owner._MainBlend * LegBlendWeight * InternalModuleBlendWeight;
                BlendWeight = IKProcessor.IKWeight;
                IKProcessor.InverseHint = InverseHint;
            }

            public void IK_UpdateParams()
            {
                IK_UpdateParamsBase();
                IKProcessor.AutoHintMode = Owner.IKHintMode;
                //IK_UseIKMultiplicator = Owner.IKOffsetsMultiply != Vector3.one;

                IKProcessor.FeetStretchSensitivity = 0.7f + 0.6f * FeetSensitivity;
                IKProcessor.FeetFadeQuicker = 0.95f + 0.35f * FeetSensitivity;
                IKProcessor.FeetStretchLimit = 0.8f + 0.2f * FeetSensitivity;

                IKProcessor.disableFeet = !UseFeet;
            }

            public void RandomizeIndividualSettings(float from, float to)
            {
                GlueThresholdMultiplier = UnityEngine.Random.Range(Mathf.Lerp(from, to, 0.4f), to);
                LegMoveSpeedMultiplier = UnityEngine.Random.Range(from, to);
                LegRaiseMultiplier = UnityEngine.Random.Range(from, to);
            }

            bool _overwrittenSourceIKPos = false;
            public void OverrideAnimatorAnklePosition(Vector3 targetPos)
            {
                _overwrittenSourceIKPos = true;
                _AnimatorEndBonePos = targetPos + (Owner._LastAppliedHipsFinalPosition - ParentHub.LastKeyframePosition);
                _SourceIKPos = _AnimatorEndBonePos;
            }

        }
    }
}