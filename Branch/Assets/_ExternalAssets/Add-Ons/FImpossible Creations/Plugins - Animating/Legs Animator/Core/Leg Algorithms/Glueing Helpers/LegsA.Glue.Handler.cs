using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            /// <summary>
            /// This class is handling blend between glued and not glued leg IK position transition.
            /// (This class is very simple, just forwarding methods and handling basic blend transition)
            /// It's also operating LegTransitionAnimation class (more complex one)
            /// which is responsive for IK transitioning between two positions. 
            /// </summary>
            partial class GlueAttachementHandler
            {
                LegsAnimator Owner;
                #region Leg References

                Leg ParentLeg;
                Leg leg { get { return ParentLeg; } }

                #endregion

                public float glueAnimationBlend { get; private set; }
                float _sd_glueAnimationBlend = 0f;

                public float attachTransitionProgress { get { return legMoveAnimation.transitionProgress; } }
                public float attachTransitionProgressLastFrame { get { return legMoveAnimation.transitionProgressLastFrame; } }
                public float legMoveDistanceFactor { get { return legMoveAnimation.legMoveDistanceFactor; } }
                public EGlueMode lasGlueModeOnAttaching { get; private set; }


                public GlueAttachementHandler(Leg leg)
                {
                    ParentLeg = leg;
                    Owner = leg.Owner;
                    legMoveAnimation = new LegTransitionAnimation(this);
                    lasGlueModeOnAttaching = Owner._glueModeExecuted;
                    Reset(true);
                }

                public void Reset(bool initializing)
                {
                    glueAnimationBlend = 0f;
                    _sd_glueAnimationBlend = 0f;

                    if (initializing)
                    {
                        lastGluePosition = leg.BoneEnd.position;
                        lastGlueRotation = leg.BoneEnd.rotation;
                    }

                    legMoveAnimation.Reset();
                    //_instantTransition = true;
                }


                #region Instant Transition 

                bool _instantTransition = false;

                /// <summary>
                /// Instant transition to being glued and instant transition to complete glue transition animation
                /// </summary>
                public void SheduleInstantTransition()
                {
                    _instantTransition = true;
                    legMoveAnimation.ScheduleInstantTransition();
                }

                #endregion


                /// <summary>
                /// Found alignable placement for foot glue
                /// </summary>
                public void TransitionToGlueAnimation()
                {
                    legMoveAnimation.DoAttaching(true);
                    ChangeGlueAnimationBlendTo(1f, Owner.GlueFadeInSpeed);
                }

                public void TransitionToDisableGlueAnimation()
                {
                    legMoveAnimation.DoAttaching(false);
                    ChangeGlueAnimationBlendTo(0f, Owner.GlueFadeOutSpeed);
                }

                Vector3 lastGluePosition = Vector3.zero;
                public Vector3 GetGluePosition()
                {
                    if (glueAnimationBlend > 0.9995f) lastGluePosition = legMoveAnimation.GetTargetPosition();
                    else
                    if (glueAnimationBlend < 0.0001f) lastGluePosition = leg.A_PreIKPosForGluing;
                    else
                        lastGluePosition = Vector3.LerpUnclamped(leg.A_PreIKPosForGluing, legMoveAnimation.GetTargetPosition(), glueAnimationBlend);

                    return lastGluePosition;
                }

                Quaternion lastGlueRotation = Quaternion.identity;
                public Quaternion GetGlueRotation()
                {
                    if (glueAnimationBlend > 0.999f) lastGlueRotation = legMoveAnimation.GetTargetRotation();
                    else
                    if (glueAnimationBlend < 0f) lastGlueRotation = leg._FinalIKRot;
                    else
                    {
                        lastGlueRotation = Quaternion.LerpUnclamped(leg._FinalIKRot, legMoveAnimation.GetTargetRotation(), glueAnimationBlend);
                    }

                    return lastGlueRotation;
                }


                public void UpdateTransitioning(bool attaching)
                {
                    legMoveAnimation.UpdateAnimation();

                    #region Editor Code (Label)
#if UNITY_EDITOR

                    //if (leg.PlaymodeIndex == 1)
                    //{
                    //    //LADB
                    //    if (Owner._EditorMotionCategory == EEditorMotionCategory.Glue)
                    //        leg._Editor_Label += "BlendIN:" + leg.R(glueAnimationBlend, 2) + " [] " + leg.R(attachTransitionProgress, 2) + "  ||  GAttached: " + leg.G_Attached;
                    //}

                    //if (Owner._EditorMotionCategory == EEditorMotionCategory.Glue)
                    //    leg._Editor_Label += "\n" + (leg.G_DuringAttaching ? "During Attach" : "N") + "  |  " + (leg.G_Attached ? "  Attached " : " No Attached");

#endif
                    #endregion
                }


                public void PostUpdate()
                {
                    legMoveAnimation.PostUpdate();
                }

                internal void OnLegRequireRepose()
                {
                    legMoveAnimation.RequireRepose();
                }


                void ChangeGlueAnimationBlendTo(float target, float speed)
                {
                    if (Owner.GroundedTime < 0.0f) speed = .99f;

                    if (_instantTransition) if (target > 0f)
                        {
                            glueAnimationBlend = target;
                            _instantTransition = false;
                            return;
                        }

                    if (speed >= 1f)
                    {
                        glueAnimationBlend = target;
                        return;
                    }

                    // Fast fade on landing
                    if (leg.G_JustLanded)
                        glueAnimationBlend = Mathf.MoveTowards(glueAnimationBlend, target, Owner.DeltaTime * 3f);

                    glueAnimationBlend = Mathf.SmoothDamp(glueAnimationBlend, target, ref _sd_glueAnimationBlend, Mathf.LerpUnclamped(0.2f, 0.005f, speed), 100000f, Owner.DeltaTime);
                    if (float.IsNaN(_sd_glueAnimationBlend)) _sd_glueAnimationBlend = 0f;
                }

            }


            GlueAttachementHandler G_AttachementHandler;
            public Vector3 G_GluePosition { get { return _GluePosition; } }
            public float G_GlueAnimationBlend { get { return G_AttachementHandler.glueAnimationBlend; } }
            public float G_GlueInternalTransition { get { return G_AttachementHandler.attachTransitionProgress; } }
            public float G_LastAttachCompleteTime { get { return G_AttachementHandler.legMoveAnimation.lastAttachCompleteTime; } }
            public float G_GlueInternalTransitionLastFrame { get { return G_AttachementHandler.attachTransitionProgressLastFrame; } }
            public float G_LastLegMoveDistanceFactor { get { return G_AttachementHandler.legMoveDistanceFactor; } }
            public bool G_DuringLegAdjustMovement { get { return G_AttachementHandler.legMoveAnimation.duringLegAdjustMovement; } }
            public EGlueMode G_HandlerExecutingLegAnimationMode { get { return G_AttachementHandler.legMoveAnimation.LastAnimationGlueMode; } }

        }
    }
}