using UnityEngine;
using UnityEngine.Events;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        public UnityEvent Event_OnStep = new UnityEvent();

        [Tooltip("Increase to execute step event sooner (speed up step confirmation). Useful if step events are executed too late.")]
        [Range(0f, 0.3f)] public float EventExecuteSooner = 0.05f;

        [Tooltip("If you want to send step events also during movement idle (in case you already use animation clip events for it)")]
        public bool SendOnMovingGlue = false;
        [Tooltip("When false, it will not allow to send step event when character is stopped")]
        public bool SendOnStopping = false;

        [Tooltip("Enabling triggering step events when character just switched grounded state")]
        public bool StepEventOnLanding = false;

        [Space(5)]
        [Tooltip("Game Object with attached component implementing LegsAnimator.ILegStepInfoReceiver interface to receiver detailed info about leg step")]
        public Transform StepInfoReceiver;
        private ILegStepReceiver _StepReceiver = null;
        private ILegRaiseReceiver _RaiseReceiver = null;

        public enum EStepType
        { 
            IdleGluing, MovementGluing, OnLanding, OnStopping
        }


        protected bool UseEvents { get; private set; }

        void Events_TriggerStepUnityEvent()
        {
            Event_OnStep.Invoke();
        }

        void Events_OnStep(Leg leg, float stepFactor = 1f, EStepType type = EStepType.IdleGluing)
        {
            if (!StepEventOnLanding)
                if (IsGroundedBlend * RagdolledDisablerBlend < 0.99f) return;

            Events_TriggerStepUnityEvent();

            if ( _StepReceiver != null)
            {
                Vector3 footMidPos = leg._PreviousFinalIKPos + leg.BoneEnd.TransformVector( (leg.AnkleToFeetEnd + leg.AnkleToHeel) * 0.5f);
                Quaternion stepRotation = Quaternion.LookRotation(leg._PreviousFinalIKRot * leg.IKProcessor.EndIKBone.forward, leg._PreviousFinalIKRot * leg.IKProcessor.EndIKBone.up);
                
                _StepReceiver.LegAnimatorStepEvent(leg, stepFactor, leg.Side == ELegSide.Right, footMidPos, stepRotation, type);
            }
        }

        void Events_OnRaise( Leg leg, float distanceToNewLegPosition = 1f, EStepType type = EStepType.IdleGluing )
        {
            if( !StepEventOnLanding )
                if( IsGroundedBlend * RagdolledDisablerBlend < 0.99f ) return;

            if( _RaiseReceiver != null )
            {
                Vector3 footMidPos = leg._PreviousFinalIKPos + leg.BoneEnd.TransformVector( ( leg.AnkleToFeetEnd + leg.AnkleToHeel ) * 0.5f );
                Quaternion stepRotation = Quaternion.LookRotation( leg._PreviousFinalIKRot * leg.IKProcessor.EndIKBone.forward, leg._PreviousFinalIKRot * leg.IKProcessor.EndIKBone.up );

                _RaiseReceiver.LegAnimatorRaiseEvent( leg, distanceToNewLegPosition, leg.Side == ELegSide.Right, footMidPos, stepRotation, type );
            }
        }

        public interface ILegStepReceiver
        {
            void LegAnimatorStepEvent(Leg leg, float power, bool isRight, Vector3 position, Quaternion rotation, EStepType type);
        }

        public interface ILegRaiseReceiver
        {
            void LegAnimatorRaiseEvent( Leg leg, float predictedDistance, bool isRight, Vector3 position, Quaternion rotation, EStepType type );
        }

    }
}