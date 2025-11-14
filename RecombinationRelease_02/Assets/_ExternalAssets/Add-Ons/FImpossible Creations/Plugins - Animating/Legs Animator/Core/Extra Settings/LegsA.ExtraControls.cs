using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {
        [Tooltip("Additional pelvis position push in local space. Can be accesed for custom pelvis offset animation or for constant model pose correction.")]
        public Vector3 ExtraPelvisOffset = Vector3.zero;

        [Tooltip("Time which needs to elapse after character stop, to trigger legs repose to most relevant pose in comparison to played idle animation")]
        [FPD_Suffix(0f, 2f, FPD_SuffixAttribute.SuffixMode.FromMinToMax, "sec")]
        public float ReposeGluingAfter = 0f;

        [Tooltip("Enable if you want to use gluing only when character is idling. Useful when it's too much work needed to setup dynamic gluing during movement for your character. (it will still use feet ground align)")]
        public bool GlueOnlyOnIdle = false;

        [Tooltip("Raycasting down direction will be synced with base transform up axis when this feature is enabled.")]
        public bool LocalWorldUp = true;


        float reposeGluingTimer = 0f;
        bool reposedGluing = false;

        public bool JustGrounded { get; private set; }

        /// <summary> Reglue Controls </summary>
        void ExtraControls_Update()
        {
            if (IsGrounded && GroundedTime < 0.2f) JustGrounded = true; else JustGrounded = false;

            if (ReposeGluingAfter > 0f)
            {
                if (MotionInfluence.rootOffset.magnitude > ScaleReference * 0.05f || IsMoving)
                {
                    reposeGluingTimer = 0f;
                    reposedGluing = false;
                }
                else
                {
                    reposeGluingTimer += DeltaTime;
                }

                if (!reposedGluing)
                {
                    if (reposeGluingTimer > ReposeGluingAfter)
                    {
                        IK_TriggerReglue();
                        reposedGluing = true;
                    }
                }
            }

        }




        #region Rotate IK 


        //bool usingIKRotate = false;
        //public void IK_ToggleForceUseIKRotate() { usingIKRotate = true; }

        Quaternion IK_UseIKRotatorQuat = Quaternion.identity;
        //bool useCustomIKRotatorVector = false;
        public Vector3 IK_CustomIKRotatorVector { get; private set; } = Vector3.zero;

        public void DisableCustomIKRotatorVector()
        {
            //useCustomIKRotatorVector = false;
        }
        public void SetCustomIKRotatorVector(Vector3 localVector)
        {
            IK_CustomIKRotatorVector = localVector;
            //useCustomIKRotatorVector = true;
        }


        #endregion



    }
}