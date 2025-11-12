using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            Transform Root { get { return Owner.BaseTransform; } }
            float ScaleRef { get { return Owner.ScaleReference; } }

            float FloorLevel { get { return Owner._glueingFloorLevel; } }
            float DeltaTime { get { return Owner.DeltaTime; } }

            public LegHelper ThighH { get { return _h_boneStart; } }
            FimpIK_Limb.IKBone ThighIK { get { return IKProcessor.StartIKBone; } }

            public LegHelper LowerLegH { get { return _h_boneMid; } }
            FimpIK_Limb.IKBone LowerLegIK { get { return IKProcessor.MiddleIKBone; } }

            public LegHelper AnkleH { get { return _h_boneEnd; } }
            public FimpIK_Limb.IKBone AnkleIK { get { return IKProcessor.EndIKBone; } }

            /// <summary> Assigned during initialization </summary>
            private Vector3 C_AnkleToHeelRootSpace = Vector3.one;

            Vector3 C_LastHeelWorldPos;
            Vector3 C_LastHeelRootSpacePos;

            public Vector3 C_LastMidRefFootWorldPos { get; private set; }
            public Vector3 C_LastMidRefFootRootSpacePos { get; private set; }

            Vector3 C_LastFootEndWorldPos;
            Vector3 C_LastFootEndRootSpacePos;

            /// <summary> When Y > 0 that means foot is above ground </summary>
            public Vector3 C_Local_MidFootPosVsGroundHit { get; private set; }
            Vector3 C_Local_AnkleToHeelRotated;

            //float C_AnkleHeightToGlueFloorHeight = 0f;

            /// <summary> Below zero when foot in animation is below root origin zero position, greater than zero when foot is above root origin zero position </summary>
            public float C_Local_FootElevateInAnimation { get; private set; }
            public float C_AnkleToHeelWorldHeight { get; private set; }
            public float C_AnimatedAnkleFlatHeight { get; private set; }
            float _C_DynamicYScale = 1f;


            void Controll_Init()
            {
                C_AnkleToHeelRootSpace = ToRootLocalSpace(Root.position + BoneEnd.TransformVector(AnkleToHeel));
                ThighH.Calibrate(this, ThighH.Bone.position);
                AnkleH.Calibrate(this, AnkleH.Bone.position);
            }

            Vector3 TransformVectorAnkleWithAlignedRotation(Vector3 offset)
            {
                return ankleAlignedOnGroundHitRotation * (Vector3.Scale( offset, BoneEnd.lossyScale) );
            }

            /// <summary>
            /// On Begin LateUpdate
            /// </summary>
            void Controll_Calibrate()
            {
                _C_DynamicYScale = Owner.DynamicYScale;

                ThighH.Calibrate(this, ThighH.Bone.position);
                AnkleH.Calibrate(this, _SourceIKPosUnchangedY);

                Vector3 footEnd = AnkleToHeel;

                // Mid foot with aligned rotation use

                Vector3 ankleToHeelShift = TransformVectorAnkleWithAlignedRotation(AnkleToHeel);
                //if (Owner.AnimateFeet) footEnd = AnkleToFeetEnd;

                //if (Owner.AnimateFeet)
                //{
                //    footEnd = AnkleToFeetEnd;
                //    footEnd = TransformVectorAnkleWithAlignedRotation(footEnd);
                //    C_LastFootEndWorldPos = _SourceIKPosUnchangedY + footEnd + (footEnd - ankleToHeelShift) * Owner.FeetLengthAdjust;
                //}
                //else
                //    C_LastFootEndWorldPos = _SourceIKPosUnchangedY + TransformVectorAnkleWithAlignedRotation(footEnd);

                C_LastFootEndWorldPos = _SourceIKPosUnchangedY + TransformVectorAnkleWithAlignedRotation(footEnd);
                C_LastFootEndRootSpacePos = ToRootLocalSpace(C_LastFootEndWorldPos);
                C_AnimatedAnkleFlatHeight = ToRootLocalSpaceDir(ankleToHeelShift).y;

                //C_AnkleHeightToGlueFloorHeight = (FloorLevel * Owner.baseTransform.lossyScale.y) + C_AnimatedAnkleFlatHeight;
                //_Editor_Label = "animH: " + C_AnimatedAnkleFlatHeight;
                //if (PlaymodeIndex == 0) UnityEngine.Debug.Log("ankleToHeelShift " + ankleToHeelShift.x+","+ankleToHeelShift.y+","+ankleToHeelShift.z);

                C_LastHeelWorldPos = _SourceIKPosUnchangedY + ankleToHeelShift;
                C_LastHeelRootSpacePos = ToRootLocalSpace(C_LastHeelWorldPos);


                if (Owner.AnimateFeet)
                {
                    C_LastMidRefFootWorldPos = Vector3.LerpUnclamped(C_LastFootEndWorldPos, C_LastHeelWorldPos, FootMiddlePosition);
                    C_LastMidRefFootRootSpacePos = Vector3.LerpUnclamped(C_LastFootEndRootSpacePos, C_LastHeelRootSpacePos, FootMiddlePosition);

                    // Mid foot with aligned rotation use
                    //C_LastMidRefFootWorldPos = Vector3.LerpUnclamped(C_LastFootEndWorldPos, C_LastHeelWorldPos, FootMiddlePosition);
                    //ankleAlignedOnGroundHitRotation
                }
                else
                {
                    C_LastMidRefFootRootSpacePos = C_LastHeelRootSpacePos;
                    C_LastMidRefFootWorldPos = C_LastHeelWorldPos;
                }

                //if (PlaymodeIndex == 0) UnityEngine.Debug.DrawRay(C_LastHeelWorldPos, Vector3.forward, Color.green, 0.11f);
                //if (PlaymodeIndex == 0) UnityEngine.Debug.DrawRay(C_LastMidRefFootWorldPos, Vector3.forward, Color.yellow, 0.11f);

                C_Local_MidFootPosVsGroundHit = C_LastMidRefFootRootSpacePos - groundHitRootSpacePos;
                
                //UnityEngine.Debug.DrawRay(RootSpaceToWorld(C_LastMidRefFootRootSpacePos), Vector3.up, Color.green, .02f);
                //UnityEngine.Debug.DrawRay(RootSpaceToWorld(groundHitRootSpacePos), Vector3.up, Color.yellow, .02f);
                //UnityEngine.Debug.DrawLine(RootSpaceToWorld(groundHitRootSpacePos), RootSpaceToWorld(C_LastMidRefFootRootSpacePos), Color.yellow, .02f);
                //if (C_Local_MidFootPosVsGroundHit.y < 0f) _Editor_Label = "groundalign";
                //else _Editor_Label = "nope";

                C_Local_FootElevateInAnimation = C_LastMidRefFootRootSpacePos.y - ParentHub._Hips_LastHipsOffset;

                C_Local_AnkleToHeelRotated = ToRootLocalSpace(Root.position + BoneEnd.TransformVector(AnkleToHeel));

                C_AnkleToHeelWorldHeight = BoneEnd.TransformVector(AnkleToHeel).magnitude;
            }


            Vector3 RootSpaceToWorldVec(Vector3 localVec)
            {
                return Owner.RootToWorldSpaceVec(localVec);
            }

            Vector3 RootSpaceToWorld(Vector3 rootLocal)
            {
                return Owner.RootToWorldSpace(rootLocal);
            }

            Vector3 ToRootLocalSpaceDir(Vector3 worldDir)
            {
                return Owner.ToRootLocalSpaceVec(worldDir);
            }

            Vector3 ToRootLocalSpace(Vector3 worldPos)
            {
                return Owner.ToRootLocalSpace(worldPos);
            }


            Vector3 ChangeLocalY(Vector3 worldPos, float targetLocalY)
            {
                worldPos = ToRootLocalSpace(worldPos);
                worldPos.y = targetLocalY;
                return RootSpaceToWorld(worldPos);
            }

            Vector3 ChangeLocalPosExceptY(Vector3 worldPos, Vector3 targetWorldPos)
            {
                worldPos = ToRootLocalSpace(worldPos);
                Vector3 newLocal = ToRootLocalSpace(targetWorldPos);
                worldPos.x = newLocal.x;
                worldPos.z = newLocal.z;
                return RootSpaceToWorld(worldPos);
            }


            void Control_StepEventCalcs()
            {
                StepEventRestore();

                if (Owner.UseGluing == false) return;
                if (_StepSent) return;

                if (Owner._glueModeExecuted == EGlueMode.Idle)
                {
                    //if (G_Transition.LastGlueMode != EGlueMode.Idle) return;
                    //if (G_Transition._legMoveFactor < 0.1f) return;

                    if (G_GlueInternalTransition >= 0.85f - Owner.EventExecuteSooner)
                    {
                        if (_ToConfirmStepEvent > 0.1f)
                        {
                            SendStepEvent(G_AttachementHandler.legMoveDistanceFactor, EStepType.IdleGluing);
                        }
                        else
                        {
                            float proMul = Mathf.InverseLerp(1f, 0f, LegAnimatingSettings.RaiseYAxisCurve.Evaluate(G_GlueInternalTransition));
                            _ToConfirmStepEvent += DeltaTime * (3f + 3f * proMul);
                        }
                    }
                    else
                    {
                        _ToConfirmStepEvent = 0f;
                    }

                }
                else // Movement Stage
                {
                    //if (PlaymodeIndex == 1) UnityEngine.Debug.Log("goes here");
                    //if (Owner.MovingTime < 0.1f) return;
                    if (G_HandlerExecutingLegAnimationMode != EGlueMode.Moving) return;
                    if (Owner.SendOnMovingGlue == false) return;
                    if (G_CustomForceNOTAttach) return;
                    //if (Owner.SwingHelper > 0f)
                    //{
                    //    Vector3 desiredLocal = ToRootLocalSpaceDir(Owner.DesiredMovementDirection);
                    //    Vector3 legSwingLocal = ToRootLocalSpaceDir(_G_RefernceSwing);
                    //    float swingDot = Vector3.Dot(desiredLocal.normalized, legSwingLocal.normalized);
                    //    if (swingDot < (1f - Owner.SwingHelper * 1f) * 1f) { return; } // Dont allow attach when swinging foot in the same direction as desired direction
                    //}

                    float heightFactor = FloorLevel * Owner.BaseTransform.lossyScale.y + C_AnkleToHeelWorldHeight * 0.5f + A_LastAlignHeightCompareValue * (1.65f + Owner.EventExecuteSooner);

                    if (G_CustomForceAttach)
                    {
                        _ToConfirmStepEvent += DeltaTime * 5f;
                        heightFactor += ScaleRef * 0.1f;
                    }

                    //if (PlaymodeIndex == 0) UnityEngine.Debug.Log("ightDiff " + A_LastAlignHeightDiff + " vs " + heightFactor);
                    if (A_LastAlignHeightDiff <= heightFactor)
                    {
                        if (_ToConfirmStepEvent > 0.2f)
                        {
                            SendStepEvent(1f, EStepType.MovementGluing);
                            _ToConfirmStepEvent = 0f;
                        }
                        else
                        {
                            _ToConfirmStepEvent += DeltaTime;

                            if (A_LastAlignHeightDiff < heightFactor * 0.75f)
                                _ToConfirmStepEvent += DeltaTime * 1f;

                            if (A_LastAlignHeightDiff < heightFactor * 0.5f)
                                _ToConfirmStepEvent += DeltaTime * 1f;
                        }
                    }
                    else
                        _ToConfirmStepEvent = 0f;

                }

            }

            internal void StepEventSentInCustomWay()
            {
                _StepSent = true;
                _StepSentAt = Time.unscaledTime;
            }

        }
    }
}