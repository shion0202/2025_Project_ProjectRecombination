using System.Collections;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {

        bool _grounded = true;
        //public bool IsGrounded { get { return Debug_IsGrounded; } }
        public bool IsGrounded { get { return _grounded; } }
        //public bool IsInAir { get { return !Debug_IsGrounded; } }
        public bool IsInAir { get { return !_grounded; } }
        /// <summary> Value which smoothly transitions to value =zero if grounded param is false </summary>
        public float IsGroundedBlend { get; private set; }
        /// <summary> When Ragdolled then goes to zero, when not ragdolled goes to value = 1 </summary>
        public float RagdolledDisablerBlend { get; protected set; } = 1f;

        public void User_SetIsGrounded(bool grounded)
        {
            if (grounded != _grounded)
            {
                if (grounded == true) // On landing
                {
                    Control_OnLand();
                }
                else // On loosing ground
                {
                    Control_OnLooseGround();
                }
            }

            _grounded = grounded;
        }


        bool _moving = false;
        //public bool IsMoving { get { return Debug_IsMoving; } }
        public bool IsMoving { get { return _moving; } }
        //public bool IsIdling { get { return !Debug_IsMoving; } }
        public bool IsIdling { get { return !_moving; } }
        /// <summary> Value which smoothly transitions to value =one if moving param is true </summary>
        public float IsMovingBlend { get; private set; }

        public void User_SetIsMoving(bool moving)
        {
            if (moving != _moving)
            {
                if (moving == true) // On Start Moving
                {
                    Control_OnStartMoving();
                }
                else // On End Moving
                {
                    Control_OnStopMoving();
                }
            }

            _moving = moving;
        }



        bool _sliding = false;
        //public bool IsMoving { get { return Debug_IsMoving; } }
        public bool IsSliding { get { return _sliding; } }
        /// <summary> Value which smoothly transitions to value =one if moving param is true </summary>
        public float IsSlidingBlend { get { return 1f - NotSlidingBlend; } }
        protected float NotSlidingBlend = 1f;

        public void User_SetIsSliding(bool moving)
        {
            _sliding = moving;
        }

        // Do it with modules
        //public void User_ForceLegIKOnPosition(int legIndex, Vector3 position, bool useIdleGlue = false)
        //{
        //    if (Legs.ContainsIndex(legIndex) == false) return;
        //    User_ForceLegIKOnPosition(legIndex, position, Legs[legIndex]._PreviousFinalIKRot, useIdleGlue);
        //}
        //public void User_ForceLegIKOnPosition(int legIndex, Vector3 position, Quaternion rotation, bool useIdleGlue = false)
        //{
        //    if (Legs.ContainsIndex(legIndex) == false) return;
        //}

        /// <summary> Fading single leg animation weight. Consider using custom modules for it. </summary>
        public void User_FadeLeg(int legIndex, float blend, float duration)
        {
            if (Legs.ContainsIndex(legIndex) == false) return;
            StartCoroutine(IEFadeLegTo(Legs[legIndex], 0f, duration));
        }

        bool? _wasFadingOn = null;

        /// <summary> Fading legs animation weight to disabled state. </summary>
        public void User_FadeToDisabled(float duration)
        {
            StopAllCoroutines();
            if (duration <= 0f) { LegsAnimatorBlend = 0f; enabled = false; } else { _wasFadingOn = false; StartCoroutine(IEFadeLegsAnimatorTo(0f, duration)); }
        }

        /// <summary> Fading legs animation weight to fully enabled state. </summary>
        public void User_FadeEnabled(float duration)
        {
            if (enabled == false) enabled = true;
            StopAllCoroutines();

            if (duration <= 0f) LegsAnimatorBlend = 1f; else { _wasFadingOn = true; StartCoroutine(IEFadeLegsAnimatorTo(1f, duration)); }
            for (int l = 0; l < Legs.Count; l++) Legs[l].LegBlendWeight = 1f;
        }


        /// <summary> For Now, Working only with IDLE GLUING. Make custom step for the leg. </summary>
        public void User_MoveLegTo(int legIndex, Transform transform)
        {
            if (Legs.ContainsIndex(legIndex) == false) return;
            var leg = Legs[legIndex];
            leg.User_OverrideRaycastHit(transform);
        }

        /// <summary> For Now, Working only with IDLE GLUING. Make custom step for the leg. </summary>
        public void User_MoveLegTo(int legIndex, Vector3 position, Vector3 normal)
        {
            RaycastHit newHit = new RaycastHit();
            newHit.point = position;
            newHit.normal = normal;
            User_MoveLegTo(legIndex, newHit);
        }

        /// <summary> For Now, Working only with IDLE GLUING. Make custom step for the leg. </summary>
        public void User_MoveLegTo(int legIndex, RaycastHit hit)
        {
            if (Legs.ContainsIndex(legIndex) == false) return;
            var leg = Legs[legIndex];
            leg.User_OverrideRaycastHit(hit);
        }

        /// <summary> Release custom leg step. </summary>
        public void User_MoveLegTo_Restore(int legIndex)
        {
            if (Legs.ContainsIndex(legIndex) == false) return;
            Legs[legIndex].User_RestoreRaycasting();
        }


        #region Coroutines

        protected IEnumerator IEFadeLegsAnimatorTo(float blend, float duration)
        {
            float startBlend = LegsAnimatorBlend;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += DeltaTime;
                LegsAnimatorBlend = Mathf.Lerp(startBlend, blend, elapsed / duration);
                yield return null;

                if ( blend == 0f) { if (_wasFadingOn == true) { _wasFadingOn = null;  yield break; } }
                else if ( blend == 1f) { if (_wasFadingOn == false) { _wasFadingOn = null; yield break; } }
            }

            LegsAnimatorBlend = blend;
            if (blend <= 0f) enabled = false;

            yield break;
        }

        protected IEnumerator IEFadeLegTo(Leg leg, float blend, float duration)
        {
            float startBlend = leg.LegBlendWeight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += DeltaTime;
                leg.LegBlendWeight = Mathf.Lerp(startBlend, blend, elapsed / duration);
                yield return null;
            }

            leg.LegBlendWeight = blend;
            yield break;
        }

        #endregion


        /// <summary>
        /// Changing legs animator position with re-initializing in new position to prevent legs pose glitches.
        /// </summary>
        public void User_Teleport(Vector3 toPosition)
        {
            enabled = false;
            transform.position = toPosition;
            enabled = true;
            CheckActivation();
            PrepareValues(1f);
            UpdateStack(1f);
        }


        /// <summary> Simple utility method for events </summary>
        public void User_HipsPushDown(float power)
        {
            User_AddImpulse(new ImpulseExecutor(Vector3.down * power, 0.3f + power * 0.3f, 1f, null));
        }

        public void User_HipsPushDownLong(float power)
        {
            User_AddImpulse(new ImpulseExecutor(Vector3.down * power, 0.5f + power * 0.5f, 1f, null));
        }

    }

}