using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {


        //public float _Hips_LastHipsOffset { get; private set; } = 0f;
        //float _Hips_StepHeightAdjustOffset = 0f;
        //float _sd_Hips_StepHeightAdjustOffset = 0f;
        //void Hips_Calc_StepAdjustTo(float yOffset)
        //{
        //    //_Hips_StepHeightAdjustOffset = Mathf.MoveTowards(_Hips_StepHeightAdjustOffset,
        //    //     yOffset, Mathf.LerpUnclamped(1f, 22f, HipsHeightStepSpeed) * DeltaTime);

        //    //return;

        //    if (HipsHeightStepSpeed >= 1f)
        //    {
        //        _Hips_StepHeightAdjustOffset = yOffset;
        //        return;
        //    }

        //    float landingBoost = GetLandingBoost();

        //    _Hips_StepHeightAdjustOffset = Mathf.SmoothDamp(_Hips_StepHeightAdjustOffset,
        //        yOffset, ref _sd_Hips_StepHeightAdjustOffset,
        //        Mathf.LerpUnclamped(0.4f, 0.01f, landingBoost)
        //        , float.MaxValue, DeltaTime);
        //}

        public float GetLandingBoost()
        {
            float landingBoost = HipsHeightStepSpeed;
            if (IsGrounded && GroundedTime < 0.2f) landingBoost = Mathf.Max(HipsHeightStepSpeed, Mathf.LerpUnclamped(HipsHeightStepSpeed, 0.95f, 0.9f));
            return landingBoost;
        }

        float HipsBlendWeight { get { return _MainBlend * HipsAdjustingBlend * HipsHeightStepBlend; } }

        void Hips_Calc_BodyAdjust()
        {
            if (HipsHeightStepBlend <= 0f) return;

            float bhipsOffset = HipsSetup.CalculateBodyAdjust();
            Vector3 baseHipsOffset = Vector3.zero;

            if (bhipsOffset != 0f && float.IsNaN(bhipsOffset) == false)
            {
                baseHipsOffset = Up * (bhipsOffset);
                Hips.position += baseHipsOffset;
            }

            if (!_hipsHubs_using) return;
            if (HipsHubsBlend < 0.0001f) return;
            for (int h = 0; h < HipsHubs.Count; h++)
            {
                HipsHubs[h]._PreHipsAdjustPosition = HipsHubs[h].bone.position;

                float hipsOffset = HipsHubs[h].CalculateBodyAdjust();

                if (hipsOffset != 0f && float.IsNaN(hipsOffset) == false)
                {
                    Vector3 offset = Up * (hipsOffset);
                    offset -= baseHipsOffset;
                    HipsHubs[h].bone.position += offset;
                }
            }
        }

    }
}