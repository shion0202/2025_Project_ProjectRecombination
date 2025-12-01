using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        [Tooltip("Use hips step adjustements and the stability algorithms")]
        public bool UseHips = true;

        //[Tooltip("Overall blend value for the hips step adjustements and for the stability algorithms")]
        //[FPD_Suffix(0f, 1f)] public float HipsAdjustingBlend = 1f;

        public float HipsAdjustingBlend
        {
            get { return UseHips ? (1f) : 0f; }
        }

        [Tooltip("Whole body lift effect blend")]
        [FPD_Suffix(0f, 1f)] public float HipsHeightStepBlend = 1f;
        [Tooltip("How fast body should adjust up/down")]
        [Range(0f, 1f)] public float HipsHeightStepSpeed = 0.7f;
        public enum EHipsAdjustStyle
        {
            SmoothDamp, FollowLegHeight
        }

        public EHipsAdjustStyle HipsAdjustStyle = EHipsAdjustStyle.SmoothDamp;

        [Tooltip("Adjusting hips to keep body balance pose")]
        [FPD_Suffix(0f, 1f)] public float StabilizeCenterOfMass = 0.45f;
        [Tooltip("Blend stability pose reference from: initial pose to: current animator pose")]
        [Range(0f, 1f)] public float AnimationIsStablePose = 0.75f;
        [Tooltip("How fast body should adjust to the stability pose / to stretch preventer pose")]
        [Range(0f, 1f)] public float StabilizingSpeed = 0.375f;

        [Tooltip("Simulating body behaviour when doing leg steps")]
        [Range(0f, 1f)] public float PushHipsOnLegMove = 0.1f;

        [Tooltip("If your setup contains more than 2 legs it can be helpful to prevent overlapping pushes of multiple legs")]
        public bool NormalizePush = false;
        [Tooltip("Related with 'Push Hips On Leg Move' parameter above. How rapidly the pelvis push effect should be animated.")]
        [Range(0f, 1f)] public float PushReactionSpeed = 0.3f;
        [Tooltip("If Push in Y axis seems to be too strong, you can calm it down with this parameter")]
        [Range(0f, 2f)] public float PushYBlend = 1f;

        [Space(3)]
        [Tooltip("Auto adjust hips to prevent leg stretching poses")]
        [Range(0f, 1f)] public float HipsStretchPreventer = 0.15f;
        public float StretchPreventerSpeed = 0.8f;

        [Space(7)]
        [Tooltip("Some of the stabilizing features may be not wanted when your character is running, you can blend them automatically to desired amount with this slider (you need to implement IsGrounded/IsMoving controls to give Legs Animator information about your character movement state)")]
        [FPD_Suffix(0f, 1f)] public float StabilizeOnIsMoving = 0.5f;



        //[Tooltip("Rotate hips accordingly to the stability offsets")]
        //[FPD_Suffix(-1f, 1f)] public float UseHipsRotation = 0.0f;
        //[Tooltip("Helper spine bone to restore it after hips rotations")]
        //public Transform HipsChildSpineBone = null;
        //[Tooltip("How much spine pose should be restored after hips rotation")]
        //[FPD_Suffix(0f, 1f)] public float CompensateChildBone = 0.5f;

        public void Hips_PreLateUpdate()
        {
            if (!_updateHipsAdjustements) return;
            HipsSetup.Calibrate();
            HipsHubs_CaptureAnimation();
        }

        public void Hips_LateUpdate()
        {
            if (!_updateHipsAdjustements) return;

            Hips_Calc_BodyAdjust();
        }

        public void Hips_PostLateUpdate()
        {
            if (!_updateHipsAdjustements) return;

            Hips_Calc_Stabilize();
            Hips_Calc_UpdateImpulses();

            Hips_Calc_ApplyImpulsesInherit();
            Hips_Calc_Elasticity();
            Hips_Calc_Apply();
            Hips_Calc_ApplyImpulses();

            Hips_ApplyTransformations();
        }



        protected virtual void Hips_ApplyTransformations()
        {
            if (float.IsNaN(_LastAppliedHipsFinalPosition.x) || float.IsNaN(_LastAppliedHipsFinalPosition.y) || float.IsNaN(_LastAppliedHipsFinalPosition.z))
            {
                // Reset hips if some unexepcted NaN exception occurs
                _LastAppliedHipsFinalPosition = RootToWorldSpace(HipsSetup.InitHipsPositionRootSpace);

                if (float.IsNaN(_LastAppliedHipsFinalPosition.x) || float.IsNaN(_LastAppliedHipsFinalPosition.y) || float.IsNaN(_LastAppliedHipsFinalPosition.z))
                    // If there is still NaN, there is something wrong in the init setup, so let's just hard reset it
                    _LastAppliedHipsFinalPosition = Vector3.zero;
            }

            if (_Hips_Modules_ExtraRotOffset != Vector3.zero)
            {
                float blend = _MainBlend * ImpulsesPowerMultiplier;
                Vector3 angles = _Hips_Modules_ExtraRotOffset;

                Quaternion hipsRotationOffset = Quaternion.identity;
                if (angles.z != 0) hipsRotationOffset *= Quaternion.AngleAxis(angles.z * blend, BaseTransform.right);
                if (angles.x != 0) hipsRotationOffset *= Quaternion.AngleAxis(angles.x * blend, BaseTransform.forward);
                if (angles.y != 0) hipsRotationOffset *= Quaternion.AngleAxis(angles.y * blend, BaseTransform.up);

                _LastAppliedHipsFinalRotationOffset = hipsRotationOffset;
                Quaternion newHipsRot = hipsRotationOffset * Hips.rotation;

                Hips.SetPositionAndRotation(_LastAppliedHipsFinalPosition, newHipsRot);
            }
            else
            {
                Hips.position = _LastAppliedHipsFinalPosition;
            }

            _LastAppliedHipsFinalOffset = _LastAppliedHipsFinalPosition - HipsSetup.LastKeyframePosition;

            HipsHubs_ApplyTransformations();
            Hips_Finalize();
        }

        protected virtual void Hips_Finalize()
        {
            _LastHipsRotationOffsetOutsideInfo = Quaternion.identity;
        }

        Vector3 _LastAppliedHipsStabilityOffset = Vector3.zero;
        internal Vector3 _LastAppliedHipsFinalPosition = Vector3.zero;
        internal Vector3 _LastAppliedHipsFinalOffset = Vector3.zero;
        internal Quaternion _LastAppliedHipsFinalRotationOffset = Quaternion.identity;
        internal Quaternion _LastHipsRotationOffsetOutsideInfo = Quaternion.identity;
        public Vector3 _Hips_Modules_ExtraWOffset = Vector3.zero;
        public Vector3 _Hips_Modules_ExtraRotOffset = Vector3.zero;

        void Hips_Calc_Apply()
        {
            _LastAppliedHipsFinalOffset = Vector3.zero;
            _LastAppliedHipsFinalRotationOffset = Quaternion.identity;
            _LastAppliedHipsFinalPosition = Hips.position;

            _LastAppliedHipsStabilityOffset = _Hips_FinalStabilityOffset * _MainBlendPlusGrounded * HipsAdjustingBlend;
            _LastAppliedHipsFinalPosition += _LastAppliedHipsStabilityOffset;
            _LastAppliedHipsFinalPosition += _Hips_Modules_ExtraWOffset;
            _Hips_Modules_ExtraWOffset = Vector3.zero;
        }

    }
}