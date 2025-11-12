using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        [SerializeField, HideInInspector] private ReferencePose setupPose = new ReferencePose();
        public ReferencePose SetupPose => setupPose;

        [SerializeField, HideInInspector] public List<ReferencePose> ExtraSetupPoses = new List<ReferencePose>();

        [System.Serializable]
        public class ReferencePose
        {
            public BonePoseReference MainHipsPose = new BonePoseReference();
            public List<BonePoseReference> HipsPoses = new List<BonePoseReference>();
            public List<LegPoseReference> LegPoses = new List<LegPoseReference>();

            public bool IsSet(LegsAnimator animator) => MainHipsPose.SourceTransform != null && HipsPoses.Count == animator.ExtraHipsHubs.Count && LegPoses.Count == animator.Legs.Count;

            [System.Serializable]
            public class LegPoseReference
            {
                public BonePoseReference UpperLegPose = new BonePoseReference();
                public BonePoseReference LowerLegPose = new BonePoseReference();
                public BonePoseReference AnklePose = new BonePoseReference();
                public BonePoseReference FeetPose = new BonePoseReference();

                public void SaveLegPose(LegsAnimator.Leg leg, LegsAnimator animator)
                {
                    UpperLegPose.SavePose(leg.BoneStart, animator);
                    LowerLegPose.SavePose(leg.BoneMid, animator);
                    AnklePose.SavePose(leg.BoneEnd, animator);
                    FeetPose.SavePose(leg.BoneFeet, animator);
                }

                public void RestoreLegPose(LegsAnimator animator)
                {
                    UpperLegPose.RestorePose(animator);
                    LowerLegPose.RestorePose(animator);
                    AnklePose.RestorePose(animator);
                    FeetPose.RestorePose(animator);
                }
            }

            [System.Serializable]
            public class BonePoseReference
            {
                public Transform SourceTransform;
                public Quaternion RotationInRoot;
                public Vector3 PositionInRoot;

                public void SavePose(Transform transform, LegsAnimator animator)
                {
                    if (animator == null) return;
                    if (transform == null) return;

                    SourceTransform = transform;
                    PositionInRoot = animator.BaseTransform.InverseTransformPoint(transform.position);
                    RotationInRoot = FEngineering.QToLocal(animator.BaseTransform.rotation, transform.rotation);
                }

                public void RestorePose(LegsAnimator animator)
                {
                    if (animator == null) return;
                    if (SourceTransform == null) return;

                    SourceTransform.position = animator.BaseTransform.TransformPoint(PositionInRoot);
                    SourceTransform.rotation = FEngineering.QToWorld(animator.BaseTransform.rotation, RotationInRoot);
                }
            }

            public void TweakListsFor(LegsAnimator animator)
            {
                // Hips Hubs
                while (HipsPoses.Count > animator.ExtraHipsHubs.Count) HipsPoses.RemoveAt(HipsPoses.Count - 1);
                while (HipsPoses.Count < animator.ExtraHipsHubs.Count) HipsPoses.Add(new BonePoseReference());

                //  count
                while (LegPoses.Count > animator.Legs.Count) LegPoses.RemoveAt(LegPoses.Count - 1);
                while (LegPoses.Count < animator.Legs.Count) LegPoses.Add(new LegPoseReference());
            }

            public void Clear()
            {
                MainHipsPose.SourceTransform = null;
                HipsPoses.Clear();
                LegPoses.Clear();
            }
        }

        public void StoreSetupPose() => StoreSetupPose(setupPose);
        public void StoreSetupPose(ReferencePose referencePose)
        {
            referencePose.TweakListsFor(this);
            referencePose.MainHipsPose.SavePose(Hips.transform, this);
            for (int i = 0; i < referencePose.HipsPoses.Count; i++) referencePose.HipsPoses[i].SavePose(ExtraHipsHubs[i], this);
            for (int i = 0; i < referencePose.LegPoses.Count; i++) referencePose.LegPoses[i].SaveLegPose(Legs[i], this);
        }

        public void RestoreSetupPose() => RestoreSetupPose(setupPose);
        public void RestoreSetupPose(ReferencePose referencePose)
        {
            referencePose.MainHipsPose.RestorePose(this);
            for (int i = 0; i < referencePose.HipsPoses.Count; i++) referencePose.HipsPoses[i].RestorePose(this);
            for (int i = 0; i < referencePose.LegPoses.Count; i++) referencePose.LegPoses[i].RestoreLegPose(this);
        }

        /// <summary> Cane be used for example for switching character behaviour from standing to crawling mode </summary>
        public void ApplyCustomReferencePose(ReferencePose pose)
        {
            if (pose == null) return;
            if (pose.IsSet(this) == false) return; // Not compatible

            ReferencePose currentPoseBackup = new ReferencePose();
            StoreSetupPose(currentPoseBackup);

            RestoreSetupPose(pose); // Set reference pose

            // Refresh initial setup parameters
            for (int i = 0; i < Legs.Count; i++)
            {
                Legs[i].IKProcessor.Init(BaseTransform);
            }

            RestoreSetupPose(currentPoseBackup); // Back to last pose
        }

    }

}