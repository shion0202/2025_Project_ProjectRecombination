using FIMSpace.AnimationTools;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {

        public partial class Leg
        {
            LegHelper _h_boneStart;
            LegHelper _h_boneMid;
            LegHelper _h_boneEnd;

            public class LegHelper
            {
                public Transform Bone;
                public LegHelper Child;

                public Vector3 InitPositionRootSpace;
                public Vector3 LastKeyframeRootPos;

                public LegHelper(Leg leg, Transform bone)
                {
                    Bone = bone;
                    InitPositionRootSpace = leg.ToRootLocalSpace(bone.position);
                }

                public void Calibrate(Leg leg, Vector3 wPos)
                {
                    LastKeyframeRootPos = leg.ToRootLocalSpace(wPos);
                }
            }
        }



        public Leg Setup_TryAutoLegSetup( Leg scheme, Transform toSetup, bool apply = true)
        {
            if (scheme == null) return null;
            if (toSetup == null) return null;

            // First check if most bottom bone has the same depth, conditions to replicate scheme properly
            Transform mostBottomScheme = SkeletonRecognize.GetBottomMostChildTransform(scheme.BoneStart);
            int bottomDepth = SkeletonRecognize.SkeletonInfo.GetDepth(mostBottomScheme, scheme.BoneStart);

            Transform mostBottomToSet = SkeletonRecognize.GetBottomMostChildTransform(toSetup);
            int bottomDepthToSet = SkeletonRecognize.SkeletonInfo.GetDepth(mostBottomToSet, toSetup);

            if (mostBottomToSet == null) return null;
            if (mostBottomScheme == null) return null;
            if (bottomDepthToSet != bottomDepth) return null;
            if (bottomDepth == 0) return null;
            if (bottomDepthToSet == 0) return null;

            Transform targetStart, targetMid, targetEnd;
            targetStart = toSetup;

            int referenceDepth = SkeletonRecognize.SkeletonInfo.GetDepth(mostBottomScheme, scheme.BoneEnd);
            targetEnd = mostBottomToSet;
            for (int i = 0; i < referenceDepth; i++) { if (targetEnd.parent == null) return null; targetEnd = targetEnd.parent; }

            referenceDepth = SkeletonRecognize.SkeletonInfo.GetDepth(mostBottomScheme, scheme.BoneMid);
            targetMid = mostBottomToSet;
            for (int i = 0; i < referenceDepth; i++) { if (targetEnd.parent == null) return null; targetMid = targetMid.parent; }


            #region Commented but can be helpful later

            //referenceDepth = SkeletonRecognize.SkeletonInfo.GetDepth(scheme.BoneMid, scheme.BoneStart);
            //targetStart = targetMid;
            //for (int i = 0; i < referenceDepth; i++) { if (targetEnd.parent == null) return null; targetStart = targetStart.parent; }

            #endregion


            if (targetStart == targetMid) return null;
            if (targetStart == targetEnd) return null;
            if (targetMid == targetEnd) return null;

            float referenceLength = scheme.LegLimbLength();

            Leg leg = new Leg();
            leg.BoneStart = targetStart;
            leg.BoneMid = targetMid;
            leg.BoneEnd = targetEnd;
            leg.Owner = this;

            if (leg.LegLimbLength() < referenceLength * 0.2f) return null;

            bool can = true;
            for (int l = 0; l < Legs.Count; l++)
            {
                if (Legs[l].BoneStart == leg.BoneStart) { can = false; break; }
                if (Legs[l].BoneStart == leg.BoneMid) { can = false; break; }
                if (Legs[l].BoneMid == leg.BoneStart) { can = false; break; }
                if (Legs[l].BoneMid == leg.BoneMid) { can = false; break; }
            }

            if (!can) return null;

            if (apply)
            {
                Legs.Add(leg);
                leg.DefineLegSide(this);
                leg.RefreshLegAnkleToHeelAndFeetAndAxes(BaseTransform);

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif

            }

            return leg;
        }


        public void Setup_TryAutoLegsSetup(Leg scheme, Transform parentOfLegs)
        {
            if (parentOfLegs == null) return;
            for (int i = 0; i < parentOfLegs.childCount; i++)
            {
                Setup_TryAutoLegSetup(scheme, parentOfLegs.GetChild(i), true);
            }
        }


    }
}