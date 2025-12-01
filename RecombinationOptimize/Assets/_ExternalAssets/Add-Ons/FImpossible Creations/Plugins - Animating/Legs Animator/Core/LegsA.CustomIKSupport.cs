namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        // Inherit from LegsAnimator and override stuff down below   -------------------------------

        /// <summary>
        /// On true disables the Fimpossible built in IK processors
        /// </summary>
        protected virtual bool UseCustomIK { get { return false; } }
        /// <summary>
        /// Set true to call UpdateStacks in manual order
        /// </summary>
        //protected virtual bool UseCustomIKOrder { get { return false; } }

        protected virtual void CustomIK_Initialize()
        {
            // Init your IK using Legs[] list
            // and using leg[i].BoneStart  leg[i].BoneMid   leg[i].BoneEnd
            // references and other required variables
        }

        protected virtual void CustomIK_ApplyIK()
        {
            // Your IK processor code here
            // Init your IK using Legs[] list
            // Use leg.IKProcessor.IKTargetPosition and leg.IKProcessor.IKTargetRotation
            // OR use leg.GetFinalIKPos() and leg.GetFinalIKRot()
            // to drive your IK algorithms
            // you can also access more leg details by leg.[...]
        }
    }
}