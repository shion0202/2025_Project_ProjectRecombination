using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        [Tooltip("If this model is created out of multiple leg bones hubs, you can define it here. Assigned transform must be parent of legs, in order to work properly!")]
        public List<Transform> ExtraHipsHubs = new List<Transform>();
        public enum EHipsHubsHandling
        {
            [Tooltip("Applying hips movement offset to the selected hub, in order to fix disconnected hips bones (rare case)")]
            //[Tooltip("Basic mode is applying same offset to the hips hub like to the main hips bone")]
            FixDisconnected,
            [Tooltip("Detailed mode is computing hips hub offsets individually, giving more realistic effect but costs a bit more")]
            Detailed
        }

        [Tooltip("Enter on the selected option on the right to see description")]
        public EHipsHubsHandling HipsHubsHandling = EHipsHubsHandling.Detailed;
        [Range(0f, 1f)] public float HipsHubsBlend = 1f;

        [Tooltip("If leg hub is having backbones to compensate target rotation, you can controll the spine bend style with this curve")]
        [FPD_FixedCurveWindow(0f, 0f, 1f, 3f)]
        public AnimationCurve HubsBackBonesBlend = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [Tooltip("Adding elasticity effect to the hub spine backbones adjustement animation")]
        [Range(0f, 1f)] public float HubBackBonesElasticity = 0.1f;

        public List<HipsReference> HipsHubs { get; private set; }
        bool _hipsHubs_using = false;


        void HipsHubs_Init()
        {
            _hipsHubs_using = false;
            HipsHubs = new List<HipsReference>();

            for (int i = 0; i < ExtraHipsHubs.Count; i++)
            {
                if (ExtraHipsHubs[i] == null) continue;

                HipsReference hubRef = new HipsReference();
                hubRef.Initialize(this, ExtraHipsHubs[i], BaseTransform);
                hubRef.CopyMuscleSettingsFrom(HipsSetup);
                HipsHubs.Add(hubRef);
            }

            if (ExtraHipsHubs.Count > 0) _hipsHubs_using = true;

            HipsSetup.PrepareLegs();
            for (int i = 0; i < HipsHubs.Count; i++) HipsHubs[i].PrepareHubBones();
        }

        void HipsHubs_PreCalibrate()
        {
            if (!_hipsHubs_using) return;
            for (int h = 0; h < HipsHubs.Count; h++) HipsHubs[h].PreCalibrate();
        }
        void HipsHubs_CaptureAnimation()
        {
            if (!_hipsHubs_using) return;
            for (int h = 0; h < HipsHubs.Count; h++) HipsHubs[h].Calibrate();
        }

        void HipsHubs_ApplyTransformations()
        {
            if (!_hipsHubs_using) return;

            if (HipsHubsHandling == EHipsHubsHandling.FixDisconnected)
            {
                for (int h = 0; h < HipsHubs.Count; h++) HipsHubs_ApplyBasic(HipsHubs[h]);
                return;
            }

            // Detailed Handling
            for (int h = 0; h < HipsHubs.Count; h++) HipsHubs_ApplyDetailed(HipsHubs[h]);
        }


        void HipsHubs_ApplyBasic(HipsReference hub)
        {
            hub.bone.position += _LastAppliedHipsFinalOffset * HipsHubsBlend;

            Quaternion targetRot = (_LastAppliedHipsFinalRotationOffset * _LastHipsRotationOffsetOutsideInfo) * hub.bone.rotation;

            if (HipsHubsBlend > 0.999f)
                hub.bone.rotation = targetRot;
            else
            {
                hub.bone.rotation = Quaternion.Lerp(hub.bone.rotation, targetRot, HipsHubsBlend);
            }
        }

        void HipsHubs_ApplyDetailed(HipsReference hub)
        {
            if (HipsHubsBlend < 0.0001f) return;
            float blend = HipsHubsBlend * _MainBlend * IsGroundedBlend;

            float stabilizingMultiplier = Mathf.LerpUnclamped(1f, StabilizeOnIsMoving, IsMovingBlend);

            Vector3 hubOffset = Vector3.zero;
            hubOffset += hub.CalculateCenterOfMassStability(stabilizingMultiplier);

            Vector3 legMovePush = hub.CalculateGlueMovePush() * PushHipsOnLegMove;
            legMovePush = hub.SmoothPushOffset(legMovePush, Mathf.LerpUnclamped(0.125f, 0.025f, PushReactionSpeed));
            hubOffset += (_MainBlendPlusGrounded * RootToWorldSpaceVec(legMovePush));

            Vector3 stretchPreventer = hub.CalculateStretchPreventerOffset();
            hubOffset += (_MainBlendPlusGrounded * stretchPreventer * HipsStretchPreventer * stabilizingMultiplier);

            hubOffset = hub.AnimateOffset(hubOffset);

            hub.HipsMuscle.Update(DeltaTime, hubOffset);
            hubOffset = hub.HipsMuscle.ProceduralPosition;

            hubOffset += hub.ExtraNonElasticOffset;
            hub.ExtraNonElasticOffset = Vector3.zero;

            Vector3 extraAdjustHelper = Vector3.zero;
            if (HipsSetup._Hips_LastHipsOffset > 0f) extraAdjustHelper.y -= HipsSetup._Hips_LastHipsOffset * 0.1f;
            if (hub._Hips_LastHipsOffset < 0f) extraAdjustHelper.y += hub._Hips_LastHipsOffset * 0.1f;


            Vector3 stretchReAdj = hub.CalculateStretchReadjust();
            Vector3 hubOffsetWorld = RootToWorldSpaceVec(hubOffset + extraAdjustHelper + stretchReAdj);


            Vector3 targetPos = hub.bone.position + hubOffsetWorld;

            hub.bone.localPosition = hub.LastKeyframeLocalPosition;
            hub.bone.position += RootToWorldSpaceVec(extraAdjustHelper);

            #region Realign with parent rotation

            Quaternion preRot = hub.bone.rotation;

            //float diff = Vector3.Distance(targetPos, hub.LastKeyframePosition) / ScaleReference;
            //diff = Mathf.InverseLerp(0.1f, 0.7f, diff);
            float diff = 1f;
            if (diff > 0)
                if (hub.HubBackBones.Count > 0)
                {
                    float countD = (float)hub.HubBackBones.Count - 1;
                    if (countD == 0f) countD = 1f;
                    float id = 0;

                    for (int b = hub.HubBackBones.Count - 1; b >= 0; b--)
                    {
                        var backBone = hub.HubBackBones[b];

                        // From main hub towards this modified hub position
                        Vector3 toHubB = (backBone.frontBone.position - backBone.bone.position).normalized;
                        Vector3 toHubNewB = (targetPos - backBone.bone.position).normalized;

                        float dot = Vector3.Dot(toHubB, toHubNewB);
                        float soother = 0f;

                        if (dot < 0.985f)
                        {
                            // Check if spine is not being rotated too much to the sides/backwards
                            Vector3 toHubLocal = ToRootLocalSpaceVec(toHubB);
                            Vector3 toHubNewLocal = ToRootLocalSpaceVec(toHubNewB);
                            toHubLocal.y = 0f;
                            toHubNewLocal.y = 0f;
                            float localDot = Vector3.Dot(toHubLocal.normalized, toHubNewLocal.normalized);
                            soother = Mathf.InverseLerp(0.985f, 0.5f, localDot);

                            toHubNewB = Vector3.Slerp(toHubNewB, toHubB, soother);
                        }

                        toHubNewB = backBone.AnimateTargetDirection(toHubNewB);

                        Vector3 finalDir = Vector3.LerpUnclamped(toHubB, toHubNewB, (diff * blend / countD) * (HubsBackBonesBlend.Evaluate(id / countD)) * (1f - soother));
                        
                        Quaternion fromTo = Quaternion.FromToRotation(toHubB, finalDir);

                        backBone.bone.rotation = fromTo * backBone.bone.rotation;

                        id += 1f;
                    }
                }

            Quaternion compensateRot = Quaternion.Inverse(hub._LastHipsRotationOffsetOutsideInfo);
            hub.bone.rotation = compensateRot * Quaternion.SlerpUnclamped(hub.bone.rotation, preRot, 0.75f * HipsHubsBlend);
            hub._LastHipsRotationOffsetOutsideInfo = Quaternion.identity;

            #endregion


        }



    }
}