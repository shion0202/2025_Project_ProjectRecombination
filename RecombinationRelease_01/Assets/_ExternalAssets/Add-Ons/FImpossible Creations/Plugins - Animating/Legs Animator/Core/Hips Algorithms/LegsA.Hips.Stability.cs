using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public enum EStabilityMode
        {
            Biped_Deprecated, Universal
        }

        public EStabilityMode StabilityAlgorithm = EStabilityMode.Universal;

        Vector3 _Hips_StabilityLocalOffset = Vector3.zero;
        FMuscle_Eulers _Hips_RotationMuscle;
        public Vector3 _Get_Hips_StabilityLocalOffset { get { return _Hips_StabilityLocalOffset; } }
        Vector3 _Hips_FinalStabilityOffset = Vector3.zero;

        //Vector3 _Hips_StabilityLocalAdjustement = Vector3.zero;
        public Vector3 _Get_Hips_StabilityLocalAdjustement { get { return HipsSetup._Get_Hips_StabilityLocalAdjustement; } }
        //public Vector3 _Get_Hips_StabilityLocalAdjustement { get { return _Hips_StabilityLocalAdjustement; } }
        //Vector3 _Hips_sd_StabilAdjustm = Vector3.zero;

        //Vector3 _Hips_PushLocalOffset = Vector3.zero;
        //Vector3 _Hips_sd_PushOffset = Vector3.zero;


        void Initialize_Stability()
        {
            _Hips_RotationMuscle = new FMuscle_Eulers();
            _Hips_RotationMuscle.Initialize(Vector3.zero);
        }

        void Hips_Calc_PreRefreshVariables()
        {
            _Hips_StabilityLocalOffset = Vector3.zero; // Reset before adjustement calculations
            _Hips_Modules_ExtraRotOffset = Vector3.zero; // Reset before adjustement calculations
        }

        void Hips_Calc_Stabilize()
        {
            float baseBlend = _MainBlendPlusGrounded;
            float stabilizingMultiplier = Mathf.LerpUnclamped(1f, StabilizeOnIsMoving, IsMovingBlend);

            HipsSetup.CalculateCenterOfMassStability(stabilizingMultiplier);

            // Push hips on leg move hips motion smoothing
            float pushingDuration = 0f;
            if (PushReactionSpeed < 1f) pushingDuration = Mathf.LerpUnclamped(0.125f, 0.025f, PushReactionSpeed);

            // Hips motion basing on idle glue leg step animation
            Vector3 legMovePush = HipsSetup.CalculateGlueMovePush();
            //ValueTowards(ref _Hips_PushLocalOffset, legMovePush, ref _Hips_sd_PushOffset, pushingDuration);
            Vector3 legPushLocalOffset = HipsSetup.SmoothPushOffset(legMovePush, pushingDuration);

            // Finalize
            if (HipsStretchPreventer > 0f)
            {
                Vector3 stretchOffset = HipsSetup.CalculateStretchPreventerOffset();
                _Hips_StabilityLocalOffset += (_MainBlendPlusGrounded * stretchOffset * HipsStretchPreventer * stabilizingMultiplier);
            }

            // Push effect - base object translation based
            if (MotionInfluence.AdvancedInfluence || MotionInfluence.AxisMotionInfluence.x > 0f)
                _Hips_StabilityLocalOffset += MotionInfluence.CalculateInversedInfluence();

            // Apply stability and push offsets
            _Hips_StabilityLocalOffset += _Get_Hips_StabilityLocalAdjustement;
            _Hips_StabilityLocalOffset += legPushLocalOffset * PushHipsOnLegMove;

            // Custom local offsets apply
            if (ExtraPelvisOffset != Vector3.zero) if (IsGroundedBlend > 0f)
                    Hips.position += RootToWorldSpaceVec(ExtraPelvisOffset * baseBlend);

        }





        void ValueTowards(ref Vector3 value, Vector3 target, ref Vector3 sd, float duration)
        {
            ValueTowards(ref value, target, ref sd, duration, DeltaTime);
        }


        void ValueTowards(ref Vector3 value, Vector3 target, ref Vector3 sd, float duration, float delta)
        {
            if (duration < 1f)
            {
                value = Vector3.SmoothDamp(value,
                    target, ref sd, duration, 10000000f, delta);
            }
            else
            {
                value = target;
            }
        }


    }
}