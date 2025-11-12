using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        public float DeltaTime { get; private set; }
        /// <summary> World Scale </summary>
        public float Scale { get; private set; }

        protected bool legsWasDisabled = false;
        protected void OnDisable()
        {
            legsWasDisabled = true;
        }

        public Matrix4x4 CastMx { get; private set; }
        public Matrix4x4 InvCastMx { get; private set; }
        public void User_OverwriteCastMatrix(Matrix4x4 mx)
        {
            Up = mx.MultiplyVector(Vector3.up).normalized;
            CastMx = mx;
            InvCastMx = mx.inverse;
        }

        void RefreshMatrices()
        {
            if (LocalWorldUp)
            {
                Up = BaseTransform.up;
                CastMx = baseTransform.localToWorldMatrix;
                InvCastMx = baseTransform.worldToLocalMatrix;
            }
            else
            {
                Quaternion rootTopSpace = Quaternion.FromToRotation(Vector3.ProjectOnPlane(baseTransform.forward, Up), Vector3.forward);
                CastMx = Matrix4x4.TRS(BaseTransform.position, rootTopSpace, BaseTransform.lossyScale);
                InvCastMx = CastMx.inverse;
            }
        }


        public bool Util_OnLeftSide(Transform t)
        {
            Vector3 inLocal = BaseTransform.InverseTransformPoint(t.position);
            return inLocal.x < 0f;
        }


        public float Util_SideMul(Transform t)
        {
            return Util_OnLeftSide(t) ? -1f : 1f;
        }


        /// <summary> Current frame world space distance from hips bone to the ground </summary>
        public float HipsToGroundDistance()
        {
            if (Hips == null) return 0.1f;
            return Vector3.Distance(BaseTransform.position, Hips.position);
        }
        public float HipsToGroundDistanceLocal()
        {
            if (Hips == null) return 0.1f;
            return BaseTransform.InverseTransformPoint(Hips.position).y;
        }

        void UpdateGroundedBlend()
        {
            if (IsGrounded)
            {
                if (UngroundFadeSpeed >= 1f) IsGroundedBlend = 1f;
                else
                {
                    IsGroundedBlend = Mathf.MoveTowards(IsGroundedBlend, 1f, DeltaTime * Mathf.LerpUnclamped(20f, 60f, UngroundFadeSpeed));
                    //IsGroundedBlend = Mathf.Lerp(IsGroundedBlend, 1.01f, DeltaTime * Mathf.LerpUnclamped(10f, 25f, UngroundFadeSpeed));
                    //if (IsGroundedBlend > 1f) IsGroundedBlend = 1f;
                }
            }
            else
            {
                if (UngroundFadeSpeed >= 1f) IsGroundedBlend = 0f;
                else
                {
                    IsGroundedBlend = Mathf.Lerp(IsGroundedBlend, -0.01f, DeltaTime * Mathf.LerpUnclamped(6f, 20f, UngroundFadeSpeed));
                    if (IsGroundedBlend < 0f) IsGroundedBlend = 0f;
                    //IsGroundedBlend = Mathf.MoveTowards(IsGroundedBlend, 0f, DeltaTime * Mathf.LerpUnclamped(1f, 15f, UngroundFadeSpeed));
                }
            }
        }

        void UpdateMovingBlend()
        {
            if (IsMoving)
            {
                if (IsMovingFadeSpeed >= 1f) IsMovingBlend = 1f;
                else IsMovingBlend = Mathf.MoveTowards(IsMovingBlend, 1f, DeltaTime * Mathf.LerpUnclamped(5f, 60f, IsMovingFadeSpeed));
            }
            else
            {
                if (IsMovingFadeSpeed >= 1f) IsMovingBlend = 0f;
                else IsMovingBlend = Mathf.MoveTowards(IsMovingBlend, 0f, DeltaTime * Mathf.LerpUnclamped(5f, 60f, IsMovingFadeSpeed));
            }
        }

        void UpdateSlidingBlend()
        {
            if (IsSliding)
            {
                NotSlidingBlend = Mathf.MoveTowards(NotSlidingBlend, 0f, DeltaTime * 6f);
            }
            else
            {
                NotSlidingBlend = Mathf.MoveTowards(NotSlidingBlend, 1f, DeltaTime * 6f);
            }
        }

        public Vector3 ToRootLocalSpaceVec(Vector3 vec)
        {
            return InvCastMx.MultiplyVector(vec);
        }

        /// <summary> Method to help getting angle value for character orientation in reference to target look rotation and current movement direction </summary>
        internal float User_GetLocalRotationAngle(Vector3 worldMoveDirection, Vector3 currentWorldLookForwardDirection)
        {
            Vector3 worldDirToLoc = ToRootLocalSpaceVec(worldMoveDirection);
            worldDirToLoc.y = 0f;
            Vector3 currWForw = ToRootLocalSpaceVec(currentWorldLookForwardDirection);
            currWForw.y = 0f;
            worldDirToLoc.Normalize();
            currWForw.Normalize();
            return -Vector3.SignedAngle(worldDirToLoc, currWForw, Vector3.up);
        }

        public Vector3 ToRootLocalSpace(Vector3 worldPos)
        {
            return InvCastMx.MultiplyPoint3x4(worldPos);
        }

        public Vector3 RootToWorldSpaceVec(Vector3 vec)
        {
            return CastMx.MultiplyVector(vec);
        }

        public void User_AddImpulse(PelvisImpulseSettings debugPushHipsImpulse, float multiplyPower = 1f, float multiplyDuration = 1f)
        {
            User_AddImpulse(new ImpulseExecutor(debugPushHipsImpulse, multiplyPower, multiplyDuration));
        }

        public void User_AddImpulse(ImpulseExecutor newImpulse)
        {
            if (newImpulse.ImpulseDuration <= 0f) return;

            Impulses.Add(newImpulse);
        }

        public Vector3 RootToWorldSpace(Vector3 localPos)
        {
            return CastMx.MultiplyPoint3x4(localPos);
        }


        /// <summary> Forcing Leg IK to instantly move towards given position / rotation. Set to null to disable following custom ik position. </summary>
        public void User_OverwriteIKCoords(int legID, Vector3? position, Quaternion? rotation = null)
        {
            if (Legs.ContainsIndex(legID) == false) return;

            Legs[legID].OverrideTargetIKPosition(position);
            Legs[legID].OverrideTargetIKRotation(rotation);
        }

        public void DoBackCompatibilityChecks()
        {
            if ( ZeroStepsOnNoRaycast)
            {
                ZeroStepsOnNoRaycast = false;
                NoRaycastGroundBehaviour = ENoRaycastBehviour.ZeroFloorSteps;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }


        public bool CheckIfSomeOfTheLegsHasNullBone()
        {
            bool lackingBone = false;
            for (int i = 0; i < Legs.Count; i++)
            {
                var leg = Legs[i];
                if (leg.BoneStart == null || leg.BoneMid == null || leg.BoneEnd == null) { lackingBone = true; break; }
            }

            return lackingBone;
        }

        /// <summary>
        /// Removing leg reference from the LegsAnimator system and refreshing other legs properties
        /// </summary>
        public void RemoveLeg(int id)
        {
            if (id < 0) return;
            if (id >= Legs.Count) return;

            var leg = Legs[id];

            Leg parentLeg = null;

            var opposideIds = new System.Collections.Generic.Dictionary<Leg, Leg>();

            for (int i = 0; i < Legs.Count; i++)
            {
                if (Legs[i].OppositeLegIndex == id) Legs[i].OppositeLegIndex = (-1);
                else if (Legs[i].OppositeLegIndex != -1) opposideIds.Add(Legs[i], Legs[i].GetOppositeLegReference(this));

                if (parentLeg == null) if (Legs[i].NextLeg == leg) { parentLeg = Legs[i]; }
            }

            parentLeg.SetNextLeg(leg.NextLeg);

            Legs.RemoveAt(id);
            leg.ParentHub.ChildLegs.Remove(leg);

            for (int i = 0; i < Legs.Count; i++) Legs[i].RefreshPlaymodeIndex(i);

            foreach (var legPair in opposideIds) legPair.Key.OppositeLegIndex = legPair.Value.PlaymodeIndex;

            for (int i = 0; i < Legs.Count; i++) Legs[i].RefreshHasOppositeLeg();
        }

    }

}