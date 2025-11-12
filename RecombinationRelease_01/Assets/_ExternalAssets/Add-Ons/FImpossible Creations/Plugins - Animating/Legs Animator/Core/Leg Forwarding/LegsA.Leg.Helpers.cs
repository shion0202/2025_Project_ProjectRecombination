using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {

        public partial class Leg
        {
            public List<Leg> Legs { get { return Owner.Legs; } }

            public void DefineLegSide(LegsAnimator get, Leg knownOppositeLeg = null)
            {
                if (knownOppositeLeg != null)
                {
                    if (knownOppositeLeg.Side != ELegSide.Undefined)
                    {
                        if (knownOppositeLeg.Side == ELegSide.Left) Side = ELegSide.Right;
                        else Side = ELegSide.Left;
                        return;
                    }
                }

                if (BoneStart != null)
                {
                    if (get.Util_OnLeftSide(BoneStart)) Side = ELegSide.Left; else Side = ELegSide.Right;
                }
            }

            public void AssignOppositeLegIndex(int oppositeIndex)
            {
                if (oppositeIndex == OppositeLegIndex) return;

                if (Owner)
                    if (Owner.Legs.ContainsIndex(oppositeIndex))
                    {
                        Owner.Legs[oppositeIndex].OppositeLegIndex = Owner.Leg_GetIndex(this);
                    }

                OppositeLegIndex = oppositeIndex;
            }

            public Leg GetOppositeLegReference(LegsAnimator legs)
            {
                if (OppositeLegIndex < 0) return null;
                if (legs.Legs.ContainsIndex(OppositeLegIndex) == false) return null;
                return legs.Legs[OppositeLegIndex];
            }

            public void RefreshLegAnkleToHeelAndFeetAndAxes(Transform baseT)
            {
                RefreshLegAnkleToHeelAndFeet(baseT);
                RefreshLegAnkleAxes(baseT);
            }

            public void RefreshLegAnkleToHeelAndFeet(Transform baseT)
            {
                if (BoneEnd == null) return;

                Vector3 wGroundPos = BoneEnd.position;

                Vector3 locGroundPos = baseT.InverseTransformPoint(wGroundPos); // Align on base transform floor position
                locGroundPos.y = 0f;
                wGroundPos = baseT.TransformPoint(locGroundPos);

                AnkleToHeel = BoneEnd.InverseTransformPoint(wGroundPos);
                AnkleToFeetEnd = BoneEnd.InverseTransformPoint(wGroundPos + baseT.forward * ScaleRef * 0.15f);
            }

            public void RefreshLegAnkleAxes(Transform baseT)
            {
                if (!BoneEnd) return;
                Quaternion baseAdjustRotation = baseT.rotation * Quaternion.Euler(0f, AnkleYawCorrection, 0f);

                AnkleForward = BoneEnd.InverseTransformDirection(baseAdjustRotation * Vector3.forward);
                AnkleUp = BoneEnd.InverseTransformDirection(baseAdjustRotation * Vector3.up);
                AnkleRight = BoneEnd.InverseTransformDirection(baseAdjustRotation * Vector3.right);
            }


            void EnsureAxesNormalization()
            {
                AnkleRight.Normalize();
                AnkleUp.Normalize();
                AnkleForward.Normalize();
            }

        }


        public int Leg_GetIndex(Leg leg)
        {
            for (int i = 0; i < Legs.Count; i++)
            {
                if (leg == Legs[i]) return i;
            }

            return -1;
        }

        public Leg Leg_GetLeg(int index)
        {
            if (index < 0) return null;
            if (index >= Legs.Count) return null;
            return Legs[index];
        }

    }
}