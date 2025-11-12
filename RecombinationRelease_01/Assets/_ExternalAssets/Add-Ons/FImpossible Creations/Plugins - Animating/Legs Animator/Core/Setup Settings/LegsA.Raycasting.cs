using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public enum ERaycastPrecision { Linecast, BoxcastPrecision }


        [Tooltip("Physics layer mask for leg raycasting. Which objects should be considered as steps.")]
        public LayerMask GroundMask = 1 << 0;
        public QueryTriggerInteraction RaycastHitTrigger = QueryTriggerInteraction.Ignore;

        [Tooltip("Maximum raycasting check range. Check Gizmos on the scene view")]
        [Range(0f, 2f)] public float CastDistance = 1f;

        public enum ERaycastStartHeight
        {
            [Tooltip("Casting ray starting from current hips height position of the character. Can be bad for insect creatures!")]
            Hips,
            [Tooltip("Good for spiders! Casting raycast on defined height of the character")]
            StaticScaleReference,
            [Tooltip("Casting ray starting from first bone of the leg (it's affecting height + start raycast position).")]
            FirstBone
        }

        public ERaycastStartHeight RaycastStartHeight = ERaycastStartHeight.Hips;
        [Tooltip("Origin height point for raycasts. Check Gizmos on the scene view")]
        [Range(0.5f, 2.5f)] public float RaycastStartHeightMul = 1f;

        public enum ERaycastStyle
        {
            [Tooltip("Launching raycast from foot above origin point. Good for bipeds with whole body step down/up features.")]
            StraightDown,
            [Tooltip("Launching raycast from pelvis towards foot, good for spider like creatures to detect steep ground more effectively. Body step down/up will not work as precise with this option.")]
            OriginToFoot,
            [Tooltip("Doing raycast like OriginToFoot : but if no ground is found - using another raycast in StraightDown style to find ground below and allow to execute body step down/up feature.")]
            OriginToFoot_DownOnNeed,
            [Tooltip("Doing raycasts per bone : from start bone towards mid bone, mid bone towards end bone, then down. Best precision for insect creatures.")]
            AlongBones,
            [Tooltip("No Raycasting : provide raycast hits custom using code, or leave it custom for just gluing legs animation.")]
            NoRaycasting
        }

        [Tooltip("How physical raycasting should be done. Enter on the selected style to see tooltip.")]
        public ERaycastStyle RaycastStyle = ERaycastStyle.StraightDown;
        public enum ERaycastMode { Linecast, Spherecast }
        [Tooltip("Physics detection ray volume size. Sphere Cast can provide more smooth transitions on the edges but costs a bit more.")]
        public ERaycastMode RaycastShape = ERaycastMode.Linecast;

        [Range(0f,1f)]
        [Tooltip("Shift spherecast hit point result towards original XZ position instead of hit position. Can be helpful to prevent spider legs from being bent too much in narrow spaces.")]
        public float SpherecastRealign = 0f;

        [Tooltip("Shift spherecast hit point result towards original XZ position instead of hit position. Can be helpful to prevent spider legs from being bent too much in narrow spaces.")]
        public float SpherecastResize = 1f;

        public enum ENoRaycastBehviour
        {
            [Tooltip("If ground raycast is lost, leg will detach and switch to ungrounded state")]
            Detach,
            [Tooltip("If no raycast hit detected, should character still animate leg steps in air on zero height floor level? (fake floor)")]
            ZeroFloorSteps, 
            [Tooltip("If ground raycast is lost, leg will stick to lastest found raycast hit until leg get stretched")]
            KeepAttached
        }

        [Tooltip("If foot will not find ground beneath, you can trigger different leg behavior in such scenario")]
        public ENoRaycastBehviour NoRaycastGroundBehaviour = ENoRaycastBehviour.Detach;

        [Tooltip("DEPRECATED - Used just for back compatibility - if true, switching NoRaycastGroundBehaviour to ZeroFloorSteps and changing this value to false.")]
        public bool ZeroStepsOnNoRaycast = false;

        public float NoRaycast_KeepAttachedUntilStretch = 1.1f;

        [Tooltip("How low whole body can be pulled down when one of the legs raycast hit is lower than default object position.")]
        [Range(0f, 1f)] public float BodyStepDown = 0.5f;

        [Space(3)]
        [Tooltip("How high whole body can be pulled up when all legs raycast hits are higher than default object position. (rare case for special character controllers).\nIt can also help out extra spine hubs to adjust on higher steps (for quadrupeds).")]
        [Range(0f, 1f)] public float MaxBodyStepUp = 0f;

        [Space(3)]
        [Tooltip("How fast should be applied fade-out when character starts being ungrounded. (jumping/falling)")]
        [Range(0f, 1f)] public float UngroundFadeSpeed = 0.1f;


        [Range(0f, 1f)] public float IsMovingFadeSpeed = 0.4f;

        float _calc_rayGrounding = 0f;
        float _calc_lastGrounded = -1f;

        void Legs_MidLateUpdateAndRaycasting()
        {
            // TODO : Split calulations in leg processors which are not needed to be computed by each leg
            // but are needed to compute once here, by legs animating hub component and provide to the legs


            if (UseRaycastsForIsGrounded)
            #region Raycasts for grounding
            {

                bool isGrounded = false;
                for (int l = 0; l < Legs.Count; l++)
                {
                    Legs[l].PreLateUpdate();

                    if (isGrounded) continue;
                    if (Legs[l].A_PreWasAligning) isGrounded = true;
                    else
                    if (Legs[l].RaycastHitted)
                    {
                        if (Legs[l].groundHitRootSpacePos.y > -ScaleReference * 0.05f) isGrounded = true;
                    }
                }

                if (!isGrounded) // Check additional middle raycast
                {
                    var preHit = Legs[0].legGroundHit;
                    if (Legs[0].DoRaycasting(_LastAppliedHipsFinalPosition, _LastAppliedHipsFinalPosition - Up * (HipsSetup.LastHipsHeightDiff * BaseTransform.lossyScale.y * 1.01f + ScaleReference * 0.075f)))
                    {
                        isGrounded = true;
                    }

                    Legs[0].legGroundHit = preHit;
                }

                if (isGrounded)
                {
                    if (_calc_rayGrounding < 0f) _calc_rayGrounding = 0f;
                    _calc_rayGrounding += DeltaTime;

                    if (_calc_rayGrounding < 0.05f) isGrounded = _grounded;
                }
                else
                {
                    if (_calc_rayGrounding > 0f) _calc_rayGrounding = 0f;
                    _calc_rayGrounding -= DeltaTime;

                    if (_calc_rayGrounding > 0.005f) isGrounded = _grounded;
                }


                if (isGrounded != _grounded)
                {
                    if (isGrounded)
                    {
                        if (Time.time - _calc_lastGrounded > 0.05f)
                        {
                            _calc_lastGrounded = Time.time;
                        }
                        else return;
                    }

                    User_SetIsGrounded(isGrounded);
                }

                return;
            }

            #endregion

            for (int l = 0; l < Legs.Count; l++)
            {
                Legs[l].PreLateUpdate();
            }
        }

    }
}