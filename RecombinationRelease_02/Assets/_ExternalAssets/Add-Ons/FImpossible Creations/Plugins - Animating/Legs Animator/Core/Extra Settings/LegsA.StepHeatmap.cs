//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using static FIMSpace.FProceduralAnimation.LegsAnimator;

//namespace FIMSpace.FProceduralAnimation
//{
//    public partial class LegsAnimator
//    {
//        private bool _usingStepHeatmap = false;
//        private StepHeatmapManager _stepHeatmap;

//        [FPD_FixedCurveWindow]
//        public AnimationCurve _StepHeatPenaltyCurve = AnimationCurve.EaseInOut(0.25f, 1f, 1f, 0f);
//        [FPD_FixedCurveWindow]
//        public AnimationCurve _StepHeatPenaltySameSideCurve = AnimationCurve.EaseInOut(0.2f, 1f, 0.7f, 0f);

//        public void StepHeatmap_Setup()
//        {
//            //if (Legs.Count < 4) return;
//            _stepHeatmap = new StepHeatmapManager(this);
//            _usingStepHeatmap = true;
//            _stepHeatmap.Initialize();
//        }

//        public void StepHeatmap_Update()
//        {
//            if (!_usingStepHeatmap) return;
//            _stepHeatmap.Update();
//        }

//        public void StepHeatmap_UpdateLeg(int leg)
//        {
//            //if (Legs.Count < 4) return;
//            _stepHeatmap.UpdatePreGlue(leg);
//        }

//        public class StepHeatmapManager
//        {
//            public LegsAnimator Owner;
//            List<StepLeg> StepLegs;

//            public StepHeatmapManager(LegsAnimator owner)
//            {
//                Owner = owner;
//                Initialized = false;
//            }

//            public bool Initialized { get; private set; }
//            public Bounds LocalLegsBounds { get; private set; }
//            public void Initialize()
//            {
//                StepLegs = new List<StepLeg>();

//                #region Prepare Step Legs

//                for (int l = 0; l < Owner.Legs.Count; l++)
//                {
//                    StepLeg sLeg = new StepLeg(this, Owner.Legs[l]);
//                    StepLegs.Add(sLeg);
//                }

//                #endregion

//                Bounds b = new Bounds(StepLegs[0].initialLocalPos, Vector3.zero);
//                for (int l = 1; l < StepLegs.Count; l++)
//                {
//                    b.Encapsulate(StepLegs[l].initialLocalPos);
//                }

//                LocalLegsBounds = b;

//                for (int l = 0; l < StepLegs.Count; l++)
//                {
//                    StepLegs[l].ConfigureRelations();
//                }

//                Initialized = true;
//            }


//            public void Update()
//            {
//                //for (int i = 0; i < StepLegs.Count; i++)
//                //{
//                //    var leg = StepLegs[i];
//                //    leg.StepFactorsCompute();
//                //}
//            }

//            public void UpdatePreGlue(int l)
//            {
//                var leg = StepLegs[l];
//                leg.StepFactorsCompute();
//            }


//            #region Editor Code
//#if UNITY_EDITOR
//            public void OnSceneView(int debugLeg = -1)
//            {
//                if (debugLeg >= StepLegs.Count) debugLeg = -1;

//                if (debugLeg < 0)
//                {
//                    // All Legs
//                    for (int l = 0; l < StepLegs.Count; l++)
//                    {
//                        var leg = StepLegs[l];
//                        Handles.color = Color.HSVToRGB((0.2f + l * 0.15f) % 1, 0.7f, 0.6f);

//                        Handles.DrawAAPolyLine(2, leg.LALeg._FinalIKPos, leg.relationsOppositeSide[0].Leg.LALeg._FinalIKPos);
//                    }

//                    for (int l = 0; l < StepLegs.Count; l++)
//                    {
//                        var leg = StepLegs[l];

//                        if (leg.AllowAccum < 0.5f)
//                            Handles.color = Color.Lerp(Color.red, Color.yellow, leg.AllowAccum * 2f);
//                        else
//                            Handles.color = Color.Lerp(Color.yellow, Color.green, (leg.AllowAccum * 2f) - 1f);

//                        Handles.SphereHandleCap(0, leg.LALeg._FinalIKPos, Quaternion.identity, Owner.ScaleReference * (0.05f + leg.AllowAccum * 0.03f), EventType.Repaint);
//                        Handles.Label(leg.LALeg._FinalIKPos, "\nAllow Leg Up: " + Rnd(leg.AllowAccum) + "\nPenalty: " + Rnd(leg.LastPenalty));
//                    }
//                }
//                else
//                {
//                    Handles.color = Color.white * 0.8f;

//                    var leg = StepLegs[debugLeg];

//                    Handles.DrawAAPolyLine(3, leg.LALeg._FinalIKPos, leg.relationsOppositeSide[0].Leg.LALeg._FinalIKPos);

//                    for (int r = 0; r < leg.relationsOppositeSide.Count; r++)
//                    {
//                        var rel = leg.relationsOppositeSide[r];
//                        Handles.DrawDottedLine(leg.LALeg._FinalIKPos, rel.Leg.LALeg._FinalIKPos, 3f);
//                        float penalty = leg.ComputePenaltyForOppositeSide(rel, r);
//                        Handles.Label(rel.Leg.LALeg._FinalIKPos, "\n" + Rnd(rel.Factor) + "\nPenalty=" + Rnd( penalty) );
//                    }

//                    for (int r = 0; r < leg.relationsSameSide.Count; r++)
//                    {
//                        var rel = leg.relationsSameSide[r];
//                        Handles.DrawDottedLine(leg.LALeg._FinalIKPos, rel.Leg.LALeg._FinalIKPos, 5f);

//                        float penalty = leg.ComputePenaltyForSameSide(rel);
//                        Handles.Label(rel.Leg.LALeg._FinalIKPos, "\n" + Rnd(rel.Factor)+"\nPenalty="+ Rnd(penalty));
//                    }

//                }


//            }

//            float Rnd(float v)
//            {
//                return (float)System.Math.Round(v, 2);
//            }
//#endif
//            #endregion


//            #region Step Leg Class

//            class StepLeg
//            {
//                StepHeatmapManager Heatmapper;
//                public LegsAnimator Owner { get { return Heatmapper.Owner; } }
//                public Leg LALeg { get; private set; }

//                List<StepLeg> StepLegs { get { return Heatmapper.StepLegs; } }
//                public List<LegRelation> relationsSameSide { get; private set; }
//                public List<LegRelation> relationsOppositeSide { get; private set; }

//                public Vector3 initialLocalPos { get; private set; }
//                ELegSide side;

//                LegRelation nearestSameSide;
//                float nrstSame = float.MaxValue;
//                LegRelation farestSameSide;
//                float frstSame = float.MinValue;
//                LegRelation nearestOppositeSide;
//                float nrstOppos = float.MaxValue;
//                LegRelation farestOppositeSide;
//                float frstOppos = float.MinValue;

//                public StepLeg(StepHeatmapManager heatmapper, Leg leg)
//                {
//                    Heatmapper = heatmapper;
//                    LALeg = leg;

//                    initialLocalPos = leg.Owner.ToRootLocalSpace(leg.BoneEnd.position);
//                    AllowAccum = 0f;

//                    if (leg.Side == ELegSide.Undefined)
//                    {
//                        if (initialLocalPos.x < 0f) side = ELegSide.Left;
//                        else if (initialLocalPos.x > 0f) side = ELegSide.Right;
//                    }
//                    else
//                        side = leg.Side;
//                }


//                public void ConfigureRelations()
//                {
//                    relationsSameSide = new List<LegRelation>();
//                    relationsOppositeSide = new List<LegRelation>();

//                    nrstSame = float.MaxValue;
//                    frstSame = float.MinValue;
//                    nrstOppos = float.MaxValue;
//                    frstOppos = float.MinValue;
//                    Bounds opposideSideBounds = new Bounds(Vector3.zero, Vector3.zero);


//                    // Calculate initial relations
//                    for (int l = 0; l < StepLegs.Count; l++)
//                    {
//                        if (StepLegs[l] == this) continue;

//                        var oLeg = StepLegs[l];

//                        LegRelation rel = new LegRelation(oLeg);
//                        rel.Distance = Vector3.Distance(initialLocalPos, oLeg.initialLocalPos);

//                        if (oLeg.side == side) // Same Side
//                        {
//                            if (rel.Distance < nrstSame) { nrstSame = rel.Distance; nearestSameSide = rel; }
//                            if (rel.Distance > frstSame) { frstSame = rel.Distance; farestSameSide = rel; }

//                            rel.WeightedDistance = rel.Distance;

//                            relationsSameSide.Add(rel);
//                        }
//                        else // Opposite Side
//                        {
//                            if (opposideSideBounds.center == Vector3.zero) opposideSideBounds = new Bounds(oLeg.initialLocalPos, Vector3.zero);
//                            else opposideSideBounds.Encapsulate(oLeg.initialLocalPos);

//                            relationsOppositeSide.Add(rel);
//                        }
//                    }

//                    // Compute weighted distance basing on position difference in axes
//                    for (int i = 0; i < relationsOppositeSide.Count; i++)
//                    {
//                        var oLeg = relationsOppositeSide[i];
//                        float a = Mathf.Abs(initialLocalPos.z - oLeg.Leg.initialLocalPos.z);

//                        oLeg.WeightedDistance = a;

//                        if (oLeg.WeightedDistance < nrstOppos) { nrstOppos = oLeg.WeightedDistance; nearestOppositeSide = oLeg; }
//                        if (oLeg.WeightedDistance > frstOppos) { frstOppos = oLeg.WeightedDistance; farestOppositeSide = oLeg; }
//                    }

//                    float scaleRefScale = Heatmapper.LocalLegsBounds.size.magnitude;

//                    // Use distances to define relation factors
//                    for (int s = 0; s < relationsSameSide.Count; s++)
//                        relationsSameSide[s].WeightedFactor = relationsSameSide[s].WeightedDistance / scaleRefScale;

//                    for (int s = 0; s < relationsOppositeSide.Count; s++)
//                        relationsOppositeSide[s].WeightedFactor = relationsOppositeSide[s].WeightedDistance / scaleRefScale;

//                    // Sort by weighted factor
//                    relationsSameSide.Sort((a, b) => a.WeightedFactor.CompareTo(b.WeightedFactor));
//                    relationsOppositeSide.Sort((a, b) => a.WeightedFactor.CompareTo(b.WeightedFactor));

//                    // Define main factor
//                    for (int s = 0; s < relationsSameSide.Count; s++)
//                        relationsSameSide[s].Factor = relationsSameSide[s].Distance / scaleRefScale;

//                    for (int s = 1; s < relationsOppositeSide.Count; s++)
//                        relationsOppositeSide[s].Factor = relationsOppositeSide[s].Distance / scaleRefScale;

//                    // Remove far relations
//                    //if (relationsSameSide.Count > 2) relationsSameSide.RemoveRange(2, relationsSameSide.Count - 2);
//                    //if (relationsOppositeSide.Count > 3) relationsOppositeSide.RemoveRange(3, relationsOppositeSide.Count - 3);

//                    // Ensure opposite leg assignment
//                    if (LALeg.OppositeLegIndex < 0)
//                    {
//                        LALeg.AssignOppositeLegIndex(relationsOppositeSide[0].Leg.LALeg.PlaymodeIndex);
//                    }

//                }

//                public float LastAllowFactor { get; private set; }
//                public float LastPenalty { get; private set; }
//                public float AllowAccum { get; private set; }

//                float moveCulldown = -1f;
//                float askingForDetachSince = -1f;
//                bool wasAskingForDetach = false;

//                internal void StepFactorsCompute()
//                {
//                    float penalty = 0f;

//                    for (int i = 0; i < relationsOppositeSide.Count; i++)
//                    {
//                        var rel = relationsOppositeSide[i];
//                        penalty += ComputePenaltyForOppositeSide(rel, i);
//                    }

//                    for (int i = 0; i < relationsSameSide.Count; i++)
//                    {
//                        LegRelation rel = relationsSameSide[i];
//                        penalty += ComputePenaltyForSameSide(rel);
//                    }

//                    LastPenalty = penalty;
//                    LastAllowFactor = 1f - penalty;

//                    AllowAccum += LastAllowFactor * Owner.DeltaTime * 10f;
//                    AllowAccum = Mathf.Clamp01(AllowAccum);

//                    float stretch = LALeg.IKProcessor.GetStretchValue(LALeg._PreviousFinalIKPos);
//                    if (stretch > 0.95f )
//                    {
//                        if (stretch > 1f) stretch += 1f;
//                        AllowAccum += LastAllowFactor * Owner.DeltaTime * 10f * stretch;
//                        return;
//                    }

//                    if (AllowAccum < 0.99f) LALeg.G_StepHeatmapForceNOTDetach = true;
//                }



//                public float ComputePenaltyForSameSide(LegRelation rel)
//                {
//                    float omFactor = 1f - rel.Factor;
//                    float penalty = 0f;

//                    if (rel.Leg.LALeg.G_Attached == false || rel.Leg.LALeg.G_StepHeatmapForceDetach)
//                    {
//                        float animationProgress = rel.Leg.LALeg.G_GlueInternalTransition * Owner.LegAnimatingSettings.AllowDetachBefore;
//                        float eval = Owner._StepHeatPenaltySameSideCurve.Evaluate(animationProgress);
//                        penalty += Mathf.Lerp(1f, omFactor,animationProgress) * eval * 1f;
//                    }
//                    else
//                    {
//                        float timeDiff = Time.time - rel.Leg.LALeg.G_LastAttachCompleteTime;
//                        penalty -= Mathf.Min(1f, timeDiff) * 0.1f * omFactor;
//                    }

//                    return penalty;
//                }


//                public float ComputePenaltyForOppositeSide(LegRelation rel, int i)
//                {
//                    float omFactor = 1f - rel.Factor;
//                    float penalty = 0f;

//                    if (rel.Leg.LALeg.G_Attached == false || rel.Leg.LALeg.G_StepHeatmapForceDetach)
//                    {
//                        float animationProgress = rel.Leg.LALeg.G_GlueInternalTransition * Owner.LegAnimatingSettings.AllowDetachBefore;
//                        float eval = Owner._StepHeatPenaltyCurve.Evaluate(animationProgress);
//                        penalty += omFactor * eval;
//                        if (i == 0) AllowAccum = 0f;
//                    }
//                    else
//                    {
//                        float timeDiff = Time.time - rel.Leg.LALeg.G_LastAttachCompleteTime;
//                        penalty -= Mathf.Min(1f, timeDiff) * 0.05f * omFactor;
//                    }

//                    return penalty;
//                }



//                public class LegRelation
//                {
//                    /// <summary> Relation with this leg </summary>
//                    public StepLeg Leg;
//                    public float Distance;
//                    public float Factor;
//                    public float WeightedDistance;
//                    public float WeightedFactor;

//                    public LegRelation(StepLeg with)
//                    {
//                        Leg = with;
//                    }
//                }

//            }


//            #endregion

//        }


//        #region Editor Code

//#if UNITY_EDITOR

//        public void StepHeatmap_DebugOnSceneView(int debugLeg = -1)
//        {
//            if (_stepHeatmap == null) return;
//            _stepHeatmap.OnSceneView(debugLeg);
//        }

//#endif

//        #endregion

//    }
//}