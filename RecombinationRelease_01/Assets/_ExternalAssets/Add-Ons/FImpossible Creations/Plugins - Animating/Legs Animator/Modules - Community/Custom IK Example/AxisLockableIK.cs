using FIMSpace.AnimationTools;
using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    [System.Serializable]
    public class AxisLockableIK : FimpIK_Limb
    {
        public enum EIKAxisLock { None = 0, X = 2, Y = 4, Z = 8 }
        public EIKAxisLock FirstBoneAxisLock = EIKAxisLock.None;

        public override void Update()
        {
            if (!Initialized) return;

            Refresh();

            // Foot IK Position ---------------------------------------------------

            float posWeight = IKPositionWeight * IKWeight;
            StartIKBone.sqrMagn = (MiddleIKBone.transform.position - StartIKBone.transform.position).sqrMagnitude;
            MiddleIKBone.sqrMagn = (EndIKBone.transform.position - MiddleIKBone.transform.position).sqrMagnitude;

            targetElbowNormal = GetDefaultFlexNormal();
            if (ExtraHintAdjustementOffset != Vector3.zero)
            {
                targetElbowNormal = Vector3.Lerp(targetElbowNormal, CalculateElbowNormalToPosition(EndIKBone.transform.position + EndIKBone.transform.rotation * ExtraHintAdjustementOffset), ExtraHintAdjustementOffset.magnitude).normalized;
            }

            Vector3 orientationDirection = GetOrientationDirection(IKTargetPosition, InverseHint ? -targetElbowNormal : targetElbowNormal);
            if (orientationDirection == Vector3.zero) orientationDirection = MiddleIKBone.transform.position - StartIKBone.transform.position;

            if (posWeight > 0f)
            {
                Quaternion sBoneRot = StartIKBone.GetRotation(orientationDirection, targetElbowNormal) * StartBoneRotationOffset;
                if (posWeight < 1f) sBoneRot = Quaternion.LerpUnclamped(StartIKBone.srcRotation, sBoneRot, posWeight);

                if (FirstBoneAxisLock != EIKAxisLock.None) ApplyAxisLock(FirstBoneAxisLock, StartIKBone, ref sBoneRot);

                StartIKBone.transform.rotation = sBoneRot;

                Quaternion sMidBoneRot = MiddleIKBone.GetRotation(IKTargetPosition - MiddleIKBone.transform.position, MiddleIKBone.GetCurrentOrientationNormal());
                if (posWeight < 1f) sMidBoneRot = Quaternion.LerpUnclamped(MiddleIKBone.srcRotation, sMidBoneRot, posWeight);
                //if (SecondBoneAxisLock != EIKAxisLock.None) ApplyAxisLock(SecondBoneAxisLock, MiddleIKBone, ref sMidBoneRot);
                MiddleIKBone.transform.rotation = sMidBoneRot;
            }

            postIKAnimatorEndBoneRot = EndIKBone.transform.rotation;

            EndBoneRotation();
        }

        void ApplyAxisLock(EIKAxisLock axisLock, IKBone ikBone, ref Quaternion targetRotation)
        {
            Vector3 local = FEngineering.QToLocal(ikBone.transform.parent.rotation, targetRotation).eulerAngles;
            if ((axisLock & EIKAxisLock.X) != 0) local.x = ikBone.LastKeyLocalRotation.eulerAngles.x;
            if ((axisLock & EIKAxisLock.Y) != 0) local.y = ikBone.LastKeyLocalRotation.eulerAngles.y;
            if ((axisLock & EIKAxisLock.Z) != 0) local.z = ikBone.LastKeyLocalRotation.eulerAngles.z;
            targetRotation = FEngineering.QToWorld(ikBone.transform.parent.rotation, AnimationGenerateUtils.EnsureQuaternionContinuity(targetRotation, Quaternion.Euler(local)));
        }
    }
}
