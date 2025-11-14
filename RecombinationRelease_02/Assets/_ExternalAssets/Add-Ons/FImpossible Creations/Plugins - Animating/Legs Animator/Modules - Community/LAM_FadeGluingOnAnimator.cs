using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_FadeGluingOnAnimator", menuName = "LAM_FadeGluingOnAnimator", order = 1)]
    public class LAM_FadeGluingOnAnimator : LAM_FadeOnAnimatorStatusBase
    {
        protected override void OnFadeAction(LegsAnimator.LegsAnimatorCustomModuleHelper helper, float fadeValue)
        {
            helper.Parent.MainGlueBlend = fadeValue;
        }
    }
}