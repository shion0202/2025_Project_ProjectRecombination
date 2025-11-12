using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            float lastFootForwardAngleDiffABS = 0f;
            void ExtraProcessingApply()
            {
                // Applying computed leg IK height animation offset
                if (G_LegAnimation.LegAdjustementYOffset != 0f)
                {
                    _FinalIKPos += RootSpaceToWorldVec(new Vector3(0f, G_LegAnimation.LegAdjustementYOffset * LegRaiseMultiplier * _glueTargetBlend, 0f));
                }

                // Limit foot yaw if using animated foots
                if (Owner.AnimateFeet) PostCalculate_LimitFootYaw();
            }

            void ExtraIKPostProcessingApply()
            {
                // Steps overlap pushing
                if (Owner._stepPointsOverlapRadius > 0f) PostCalculate_FeetOverlapRadius();

                // Feet y offset - bring up
                if (Owner.FeetYOffset != 0f) PostCalculate_FeetYOffset();
            }


            public void PostCalculate_LimitFootYaw()
            {
                //if (_noRaycast_skipFeetCalcs)
                //{
                //    //lastFootForwardAngleDiff = 0f;
                //    lastFootForwardAngleDiffABS = 0f;
                //}
                //else
                {
                    Vector3 originalFootForward = ankleAlignedOnGroundHitRotation * AnkleIK.forward;

                    // Prevent sudden angling on uneven steps
                    originalFootForward = ToRootLocalSpaceDir(originalFootForward);
                    originalFootForward.y = 0f; originalFootForward = RootSpaceToWorldVec(originalFootForward);

                    Vector3 currentFootForward = _FinalIKRot * AnkleIK.forward;

                    originalFootForward = ToRootLocalSpaceDir(originalFootForward);
                    originalFootForward.y = 0f;

                    currentFootForward = ToRootLocalSpaceDir(currentFootForward); 
                    currentFootForward.y = 0f;

                    float angle = Vector3.SignedAngle(originalFootForward.normalized, currentFootForward.normalized, Vector3.up/* -(ankleAlignedOnGroundHitRotation * IKProcessor.EndParentIKBone.up)*/);
                    float angleABS = Mathf.Abs(angle);

                    //lastFootForwardAngleDiff = angle;
                    lastFootForwardAngleDiffABS = angleABS;

                    if (Owner.LimitFeetYaw > 0f)
                    {
                        if (Owner.LimitFeetYaw < 90f)
                            if (angleABS > Owner.LimitFeetYaw)
                            {
                                float angleDiff = (angleABS - Owner.LimitFeetYaw);

                                Quaternion from = (A_WasAligning == true) ? ankleAlignedOnGroundHitRotation : AnkleIK.srcRotation;
                                _FinalIKRot = Quaternion.LerpUnclamped(from, _FinalIKRot, (1f - angleDiff / (90f - Owner.LimitFeetYaw)));
                            }
                    }
                }
            }



            void PostCalculate_FeetOverlapRadius()
            {
                float radius = Owner._stepPointsOverlapRadius * GlueThresholdMultiplier;

                Vector3 ikPosLoc = ToRootLocalSpace(IKProcessor.IKTargetPosition);

                // Check overlapping with other legs
                var leg = Owner.Legs[0];
                while (leg != null)
                {
                    if (leg == this)
                    {
                        leg = leg.NextLeg;
                        continue;
                    }

                    Vector3 otherLastFinalLoc = ToRootLocalSpace(leg.IKProcessor.IKTargetPosition);

                    Vector2 diff = new Vector2(otherLastFinalLoc.x, otherLastFinalLoc.z) - new Vector2(ikPosLoc.x, ikPosLoc.z);

                    float distance = diff.magnitude;

                    if (distance < radius)
                    {
                        Vector2 offset = -diff * (radius - distance) * 2f;
                        IKProcessor.IKTargetPosition += RootSpaceToWorldVec(new Vector3(offset.x, 0f, offset.y));
                    }

                    leg = leg.NextLeg;
                }
            }

            void PostCalculate_FeetYOffset()
            {
                IKProcessor.IKTargetPosition += _FinalIKRot * ((Owner.FeetYOffset * Owner.Scale * (A_AligningHelperBlend)) * AnkleIK.up);
            }

        }

    }
}