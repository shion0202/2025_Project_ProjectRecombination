using System.Collections.Generic;
using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    /// <summary>
    /// Example class for assigning custom inherited IK processor to the legs animator
    /// </summary>
    //[CreateAssetMenu]
    public class LAM_IKAlgorithmSwitch : LegsAnimatorControlModuleBase
    {
        public bool lockX = true;
        public bool lockY = false;
        public bool lockZ = false;

        [NonSerialized] List<AxisLockableIK> playmodeIKProcessors = null;

        public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            base.OnInit(helper);
            playmodeIKProcessors = new List<AxisLockableIK>();

            for (int i = 0; i < LegsAnim.Legs.Count; i++)
            {
                var leg = LegsAnim.Legs[i];
                AxisLockableIK newIK = new AxisLockableIK();
                playmodeIKProcessors.Add(newIK);
                leg.AssignCustomIKProcessor(newIK);
            }

            OnValidateAfterManualChanges(helper);
        }

        AxisLockableIK.EIKAxisLock GetLock()
        {
            AxisLockableIK.EIKAxisLock lockFlag = AxisLockableIK.EIKAxisLock.None;
            if (lockX) lockFlag |= AxisLockableIK.EIKAxisLock.X;
            if (lockY) lockFlag |= AxisLockableIK.EIKAxisLock.Y;
            if (lockZ) lockFlag |= AxisLockableIK.EIKAxisLock.Z;

            return lockFlag;
        }

        private void OnValidate()
        {
            OnValidateAfterManualChanges(null);
        }

        public override void OnValidateAfterManualChanges(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            if ( helper != null) base.OnValidateAfterManualChanges(helper);

            if (playmodeIKProcessors == null) return;

            for (int i = 0; i < playmodeIKProcessors.Count; i++)
            {
                var ik = playmodeIKProcessors[i];
                ik.FirstBoneAxisLock = GetLock();
            }
        }

    }
}