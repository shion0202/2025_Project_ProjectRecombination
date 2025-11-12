using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        [Tooltip("Using algorithm responsive for attaching feet to the ground when detected grounded foot in the played animation.")]
        public bool UseGluing = true;
        [Tooltip("You can smoothly change Glue Blend down to transition into sliding if your character is walking on ice or sliding on steep ground.")]
        [FPD_Suffix(0f,1f)] public float MainGlueBlend = 1f;

        public float GlueBlend
        {
            get { return UseGluing ? (_MainBlend * MainGlueBlend) : 0f; }
        }

        [Space(3)]
        [Tooltip("If distance from the last attach point exceeds this distance (check scene gizmos) the leg will be detached.")]
        [Range(0f, 1f)] public float GlueRangeThreshold = 0.375f;
        [Tooltip("How quickly leg attachement transition should be proceeded.")]
        [Range(0f, 1f)] public float GlueFadeInSpeed = 0.85f;

        [Tooltip("If foot animation in original played clip is not reaching floor soon enough, increase it to attach for position slightly below current foot positioning.")]
        [Range(0f, 1f)] public float AllowGlueBelowFoot = 0.2f;
        [Tooltip("How quickly leg detachement transition should be proceeded.")]
        [Range(0f, 1f)] public float GlueFadeOutSpeed = 0.5f;

        [Space(5)]
        [Tooltip("If leg rotation exceeds this angle during being attach, the leg will be detached.")]
        [FPD_Suffix(0f, 90f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°")] public float UnglueOn = 30f;

        [Space(1)]
        [Tooltip("When leg glue target position is stretching leg too much it will shift leg target towards source animation leg position.")]
        [Range(0f, 1f)] public float AllowGlueDrag = 0.7f;

        [NonSerialized] public float DontGlueAttachIfTooNearOppositeLeg = 0f;

        //[Space(1)]
        //[Tooltip("When main transform is rotating then glue transition will speed up to catch up steps better")]
        //[Range(0f, 1f)] public float SpeedupOnRotation = 0.0f;
        //[Space(6)]
        //public bool AllowReRaycastDuringTransition = false;

        //[Space(3)]
        //[Range(0.9f, 1.2f)] public float UnglueIfStretch = 1.05f;

        public enum EGlueMode
        {

            [Tooltip("Idle Mode is applying leg animation with extra motion and is checking some extra conditions like opposite leg grounded state etc.")]
            Idle,
            [Tooltip("Moving Mode is dedicated to be applied during playing animations with dynamic legs, it's checking less conditions than Idle Mode and is snapping glue points in a more straight forward slide animation.")]
            Moving,
            [Tooltip("Automatic mode is syncing with IsMoving/IsIdling LegsAnimator flags.")]
            Automatic
        }

        [Tooltip("Enter on the value field on the right to see tooltip.")]
        public EGlueMode GlueMode = EGlueMode.Automatic;

        /// <summary> Change only when overriding automatic value! </summary>
        public EGlueMode _glueModeExecuted { get; set; }

        [Tooltip("Making Gluing animations only local space, which can be helpful when character is standing on the moving platform.")]
        public bool OnlyLocalAnimation = false;


    }
}