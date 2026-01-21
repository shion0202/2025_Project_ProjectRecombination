using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        [Tooltip("Smoothing leg align motion when sudden uneven terrain step occurs")]
        [Range(0f, 1f)] public float SmoothSuddenSteps = 0.85f;

        [Space(3)]
        [Tooltip("Making leg rise a bit over ground when character leg overlaps collision (it's mostly visible on steep slopes)")]
        [Range(0f, 2f)] public float LegElevateBlend = 0.7f;
        [Range(0f, 1f)] public float LegElevateHeightLimit = 0.6f;

        [Space(6)]
        [Tooltip("Overall foot rotation blend on the slope step align.")]
        [FPD_Suffix(0f,1f)] public float FootRotationBlend = 1f;
        //[Tooltip("Allowing to align foot on ground hit more below foot rather than only overlapping ground hit height")]
        //[Range(0f,1f)] public float FootDeeperRange = 0f;
        //[FPD_Suffix(0f, 90f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°")] public float FootAngleLimit = 0.75f;

        //[Space(2)]
        //[Range(0f, 1f)] public float FootRollBlend = 0.5f;

        [Space(4)]
        [Tooltip("How quickly foot should align it's rotation to the slopes")]
        [Range(0f, 1f)] public float FootAlignRapidity = 0.75f;

        [Tooltip("If it's human leg limb with foot, then turn it on for the foot bone animation and alignments. But if it's something like spider leg, then disable it")]
        public bool AnimateFeet = true;

        [Tooltip("If feet rotation is above this value, feet rotation will be limited to avoid weird foot rotation pose")]
        [FPD_Suffix(0f, 90f, FPD_SuffixAttribute.SuffixMode.FromMinToMax, "°")] public float LimitFeetYaw = 30f;

        [Tooltip("Local space ANKLE-step height detection level. It's detail parameter to adjust feet aligning sooner/later when foot is near to ground.")]
        [Range(-0.05f, 0.15f)] public float AnimationFloorLevel = 0.001f;

    }
}