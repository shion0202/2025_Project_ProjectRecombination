using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        [Tooltip("Algorithm selector which controls how leg is bent - knee hint.")]
        public FimpIK_Limb.FIK_HintMode IKHintMode = FimpIK_Limb.FIK_HintMode.Default;

        [Tooltip("Dragging Leg if stretched too much, for humanoids this vlaue should be high (around 0.9 - 1.1) for spider or similar creatures it should be lower.\nUsing feet bones can be really helpful to enchance the leg stretch length range!")]
        [Range(0.4f, 1.1f)] public float LimitLegStretch = 0.99f;

        [Tooltip("Pushing feet up/down if required for model correction.")]
        public float FeetYOffset = 0.0f;

        [Tooltip("Adjust the visual size of feet in order to make foot aligning calculations more precise.")]
        [Range(-1f, 1f)] public float FeetLengthAdjust = 0f;

        void IK_Initialize()
        {
            for (int i = 0; i < Legs.Count; i++)
            {
                Legs[i].IK_Initialize();
            }

            if (UseCustomIK) CustomIK_Initialize();
        }

        void IK_TriggerGlueReinitialize()
        {
            for (int i = 0; i < Legs.Count; i++)
            {
                Legs[i].Glue_Reset(true);
            }
        }

        void IK_TriggerGlueInstantTransition()
        {
            _wasInstantTriggered = true;

            for (int i = 0; i < Legs.Count; i++)
            {
                Legs[i].G_InstantReglue = true;
            }
        }

        public void IK_TriggerReglue(bool onlyIfFar = true)
        {
            var leg = Legs[0];
            while (leg != null)
            {
                leg.G_RequestRepose = onlyIfFar ? Leg.GlueReposeRequest.ReposeIfFar : Leg.GlueReposeRequest.Repose;
                leg = leg.NextLeg;
            }
        }

    }
}