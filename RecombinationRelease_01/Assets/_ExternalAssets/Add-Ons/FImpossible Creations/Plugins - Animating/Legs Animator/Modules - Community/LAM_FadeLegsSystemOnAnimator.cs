using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    //[CreateAssetMenu(fileName = "LAM_FadeLegsSystemOnAnimator", menuName = "LAM_FadeLegsSystemOnAnimator", order = 1)]
    public class LAM_FadeLegsSystemOnAnimator : LAM_FadeOnAnimatorStatusBase
    {
        protected override void OnFadeAction(LegsAnimator.LegsAnimatorCustomModuleHelper helper, float fadeValue)
        {
            helper.Parent.LegsAnimatorBlend = Mathf.Max(0.001f, fadeValue);
        }
    }
}