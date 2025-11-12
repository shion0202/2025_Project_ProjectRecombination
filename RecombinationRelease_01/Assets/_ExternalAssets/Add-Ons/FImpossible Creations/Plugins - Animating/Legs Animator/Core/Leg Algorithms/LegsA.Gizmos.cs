#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {

#if UNITY_EDITOR

            public void _Editor_Align_DrawControls()
            {
                if (Application.isPlaying == false) return;

                if (A_PreWasAligning) Handles.color = Color.green;
                else Handles.color = Color.red;

                Handles.SphereHandleCap(0, _SourceIKPos, Quaternion.identity, ScaleRef * 0.02f, EventType.Repaint); ;
                Handles.DrawLine(C_LastMidRefFootWorldPos, _SourceIKPos);
                Handles.DrawWireDisc(C_LastMidRefFootWorldPos, Root.right, ScaleRef * 0.05f);

                Handles.DrawLine(LastGroundHit.point, _SourceIKPos);
                if (!string.IsNullOrWhiteSpace(_Editor_Label))
                {
                    Handles.Label(_FinalIKPos, _Editor_Label);
                }

                Handles.color *= 0.6f;
                Handles.DrawDottedLine(_FinalIKPos, _SourceIKPos, 2f);

                if (A_LastElevation.sqrMagnitude > 0.05f) Handles.DrawAAPolyLine(2f, ankleAlignedOnGroundHitWorldPos, ankleAlignedOnGroundHitWorldPos + A_LastElevation);

                if (Owner.AnimateFeet) Handles.DrawWireDisc(C_LastMidRefFootWorldPos, Owner.Up, ScaleRef * 0.1f);


                Handles.color *= 0.7f;
                Handles.SphereHandleCap(0, _FinalIKPos, Quaternion.identity, ScaleRef * 0.02f, EventType.Repaint); ;
            }

            public void _Editor_Raycasting_DrawSwingReference()
            {
                if (Application.isPlaying == false) return;

                Vector3 lOff = new Vector3(1f, 1f, 0f);
                if (Side == ELegSide.Left) lOff = new Vector3(-1f, 1f, 0f);
                Vector3 origPos = Owner.BaseTransform.TransformPoint(lOff);

                Handles.SphereHandleCap(0, origPos, Quaternion.identity, ScaleRef * 0.02f, EventType.Repaint);
                Handles.DrawLine(origPos, origPos + Owner.BaseTransform.TransformVector(_G_RefernceSwing));
                Handles.SphereHandleCap(0, origPos, Quaternion.identity, ScaleRef * 0.02f, EventType.Repaint);
            }

            public void _Editor_Raycasting_DrawControls()
            {
                if (Application.isPlaying == false) return;
                if (!RaycastHitted) return;

                Handles.color = Color.yellow * 0.8f;

                Handles.SphereHandleCap(0, legGroundHit.point, Quaternion.identity, ScaleRef * 0.02f, EventType.Repaint);
                Handles.DrawWireDisc(legGroundHit.point, legGroundHit.normal, ScaleRef * 0.05f);

                if (Owner.RaycastStyle == LegsAnimator.ERaycastStyle.NoRaycasting) return;

                Handles.DrawDottedLine(legGroundHit.point, lastRaycastingOrigin, 2f);

                Handles.color *= 0.7f;
                Handles.SphereHandleCap(0, lastRaycastingOrigin, Quaternion.identity, ScaleRef * 0.02f, EventType.Repaint);

            }

            string _Editor_Label = "";

            public void _Editor_Hips_DrawControls()
            {
                if (Application.isPlaying == false) return;
                if (!RaycastHitted) return;

                Handles.color = new Color(0.7f, 0.7f, 0.2f, 0.8f);

            }

            public void _Editor_Glue_DrawControls()
            {
                if (!BoneEnd) return;

                Handles.color = (Color.yellow * 0.8f).ChangeColorAlpha(0.5f);
                float gRange = G_GlueTesholdRange * Owner.BaseTransform.lossyScale.x;
                Vector3 heel = BoneEnd.TransformPoint(AnkleToHeel);

                Vector3 off = Vector3.zero;

                if (!Application.isPlaying)
                {
                    off = Owner.BaseTransform.TransformVector(new Vector3(GluePointOffset.x, 0, GluePointOffset.y) * ScaleRef * Owner.GlueRangeThreshold );
                }
                else
                    off = GetGluePointOffset();

                heel += off;

                if (Application.isPlaying && Owner.LegsInitialized)
                {
                    Handles.color *= G_Attached ? 0.1f : 0.7f;
                    if (G_DuringAttaching) Handles.color = new Color(0.8f, 0.5f, 0.1f, 1f);
                }

                if (Owner.AllowGlueBelowFoot > 0f)
                {
                    Vector3 glueRange = -Owner.Up * (BelowFootRange * Owner.AllowGlueBelowFoot);
                    Handles.DrawWireDisc(heel + glueRange, Owner.BaseTransform.up, gRange);
                    Handles.SphereHandleCap(0, heel + glueRange, Quaternion.identity, gRange * 0.2f, EventType.Repaint);
                    Handles.DrawDottedLine(heel, heel + glueRange, 3f);
                }

                //Handles.DrawWireDisc(heel, Root.up, gRange);
                Handles.color *= 0.7f;
                Handles.SphereHandleCap(0, heel, Quaternion.identity, gRange * 0.4f, EventType.Repaint);

                if (G_Attached)
                {
                    Handles.color = new Color(0.8f, 0.5f, 0.1f, 1f);
                    Handles.DrawWireDisc(G_Attachement.GetRelevantHitPoint() + off, G_Attachement.GetRelevantNormal(), gRange);
                    //Handles.DrawWireDisc(G_Attachement.GetRelevantHitPoint(), G_Attachement.GetRelevantNormal(), gRange);
                    Handles.color *= 0.7f;
                    Handles.SphereHandleCap(0, heel, Quaternion.identity, gRange * 0.4f, EventType.Repaint);
                }

                if (!string.IsNullOrWhiteSpace(_Editor_Label))
                {
                    Handles.Label(_FinalIKPos, _Editor_Label);
                }
            }


#endif

        }
    }
}