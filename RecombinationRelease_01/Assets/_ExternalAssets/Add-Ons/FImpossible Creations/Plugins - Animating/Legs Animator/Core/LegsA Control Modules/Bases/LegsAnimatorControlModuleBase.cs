#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public abstract class LegsAnimatorControlModuleBase : ScriptableObject
    {
        protected Transform Transform { get { return Owner.BaseTransform; } }

        protected LegsAnimator LA { get { return Owner; } }
        protected LegsAnimator LegsAnim { get { return Owner; } }
        protected LegsAnimator Owner { get; private set; }
        protected bool Initialized { get; private set; } = false;


        /// <summary> Editor helper to display extra field in the inspector view </summary>
        public virtual bool AskForSpineBone { get { return false; } }
        /// <summary> Editor helper to display extra field in the inspector view </summary>
        public virtual bool AskForChestBone { get { return false; } }


        /// <summary> If module supports it, use this value to fade off module influence </summary>
        public float ModuleBlend { get; set; }
        /// <summary> Module Blend * Legs Animator Blend </summary>
        public float EffectBlend { get { return ModuleBlend * LA._MainBlend; } }

        public void Base_Init(LegsAnimator legs, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
        {
            ModuleBlend = 1f;
            Owner = legs;
            OnInit(helper);
            Initialized = true;
        }

        /// <summary> [Base method does nothing] Called when Legs Animator starts to work for a first time </summary>
        public virtual void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }
        /// <summary> [Base method does nothing] Calle after enabling back legs animator </summary>
        public virtual void OnReInitialize(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }
        /// <summary> [Base method does nothing] Called after main calibration, before leg animator algorithms </summary>
        public virtual void OnUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }

        /// <summary> [Base method does nothing] Called before capturing animator pose </summary>
        public virtual void OnAfterAnimatorCaptureUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }
        /// <summary> [Base method does nothing] Called before main calculations, before hips calculations </summary>
        public virtual void OnPreLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }

        /// <summary> [Base method does nothing] Called after main calculations, after hips calculations, just before applying IK </summary>
        public virtual void OnLateUpdatePreApply(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }
        /// <summary> [Base method does nothing] Called after applying IK </summary>
        public virtual void OnPostLateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }
        /// <summary> [Base method does nothing] Special call, to update some of the IK settings only when big changes are happening. (called every change in the inspector window but needs to be called manually if editing IK settings through code) </summary>
        public virtual void OnValidateAfterManualChanges(LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }

        /// <summary> [Base method does nothing] Updated per leg with leg access after PreLateUpdate </summary>
        public virtual void Leg_LatePreRaycastingUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg) { }
        /// <summary> [Base method does nothing] Updated on each limbs hub (hips) during stability calculation </summary>
        //public virtual void OnHipsStabilizingLegInfluence(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg, ref Vector3 stabilityOffsetLocal) { }

        /// <summary> [Base method does nothing] Updated per leg with leg access after Raycasting </summary>
        public virtual void Leg_LateUpdate(LegsAnimator.LegsAnimatorCustomModuleHelper helper, LegsAnimator.Leg leg) { }



        #region Commented but can be helpful later

        ///// <summary> [Base method does nothing] </summary>
        //public virtual void Leg_OnLeg_Raycasting(LegsAnimator.Leg leg)
        //{

        //}

        ///// <summary> [Base method does nothing] </summary>
        //public virtual void Leg_OnIK_Apply(LegsAnimator.Leg leg)
        //{

        //}

        ///// <summary> [Base method returns true] </summary>
        //public virtual bool Leg_OnGlue_Condition_AllowAttach(LegsAnimator leg)
        //{
        //    return true;
        //}


        ///// <summary> [Base method does nothing] </summary>
        //public virtual void Leg_OnGlue_Apply(LegsAnimator.Leg leg)
        //{

        //}

        ///// <summary> [Base method does nothing] </summary>
        //public virtual void Leg_OnHips_Apply(LegsAnimator.Leg leg)
        //{

        //}

        #endregion  


        #region Editor Code

#if UNITY_EDITOR


        [System.NonSerialized] public SerializedObject BaseSerializedObject = null;
        [System.NonSerialized] public bool Editor_Foldout = false;
        [System.NonSerialized] public bool Editor_PlaymodeFoldout = false;

        /// <summary> [Base method does nothing] </summary>
        public virtual void Editor_OnSceneGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }

        /// <summary> [Base method does nothing] </summary>
        public virtual void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper) { }

#endif

        #endregion


        #region Editor Class
#if UNITY_EDITOR
        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor(typeof(LegsAnimatorControlModuleBase))]
        public class LegsAnimatorControlModuleBaseEditor : UnityEditor.Editor
        {
            public LegsAnimatorControlModuleBase Get { get { if (_get == null) _get = (LegsAnimatorControlModuleBase)target; return _get; } }
            private LegsAnimatorControlModuleBase _get;

            public override void OnInspectorGUI()
            {
                if (LegsAnimator._Editor_LastSelectedLA != null)
                {
                    if (GUILayout.Button(" <   Go Back To  '" + LegsAnimator._Editor_LastSelectedLA.name + "'", GUILayout.Height(24)))
                    {
                        Selection.activeGameObject = LegsAnimator._Editor_LastSelectedLA.gameObject;
                    }

                    FGUI_Inspector.DrawUILineCommon(5);
                }

                DrawDefaultInspector();
            }
        }
#endif
        #endregion


    }
}