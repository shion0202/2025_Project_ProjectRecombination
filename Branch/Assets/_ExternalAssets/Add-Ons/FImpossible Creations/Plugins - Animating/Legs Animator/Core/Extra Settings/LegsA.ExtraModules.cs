using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {

        [Tooltip("Completely turning off all custom modules scripts execution.")]
        public bool DisableCustomModules = false;

        [Tooltip("Custom coded legs animator modules to change component behaviour without affecting core code")]
        public List<LegsAnimatorCustomModuleHelper> CustomModules = new List<LegsAnimatorCustomModuleHelper>();

        void InitializeModules()
        {
            bool anyModule = false;
            if (CustomModules == null) return;

            for (int i = CustomModules.Count - 1; i >= 0; i--)
            {
                if (CustomModules[i] == null) { CustomModules.RemoveAt(i); continue; }
                if (CustomModules[i].ModuleReference == null) { CustomModules.RemoveAt(i); continue; }
                CustomModules[i].PreparePlaymodeModule(this);
                anyModule = true;
            }

            UsingControlModules = anyModule;
        }

        void DisposeModules()
        {
            if (CustomModules == null) return;

            for (int i = CustomModules.Count - 1; i >= 0; i--)
            {
                if (CustomModules[i] == null) { CustomModules.RemoveAt(i); continue; }
                if (CustomModules[i].ModuleReference == null) { CustomModules.RemoveAt(i); continue; }
                CustomModules[i].DisposeModule();
            }
        }


        public T GetModule<T>() where T : LegsAnimatorControlModuleBase
        {
            if (CustomModules == null) return null;

            for (int i = 0; i < CustomModules.Count; i++)
            {
                if (CustomModules[i].ModuleReference is T)
                {
                    return CustomModules[i].PlaymodeModule as T;
                }
            }

            return null;
        }

        public LegsAnimatorCustomModuleHelper GetModuleHelper<T>() where T : LegsAnimatorControlModuleBase
        {
            if (CustomModules == null) return null;

            for (int i = 0; i < CustomModules.Count; i++)
            {
                if (CustomModules[i].ModuleReference is T)
                {
                    return CustomModules[i];
                }
            }

            return null;
        }

        void Modules_OnReInitialize()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnReInitialize(CustomModules[m]);
        }

        private bool UsingControlModules = false;

        void Modules_Update()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnUpdate(CustomModules[m]);
        }

        void Modules_UpdateAfterManualChanges()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnValidateAfterManualChanges(CustomModules[m]);
        }

        void Modules_LegBeforeRaycastingUpdate(Leg leg)
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.Leg_LatePreRaycastingUpdate(CustomModules[m], leg);
        }

        void Modules_AfterAnimatorCaptureUpdate()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnAfterAnimatorCaptureUpdate(CustomModules[m]);
        }

        void Modules_PreLateUpdate()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnPreLateUpdate(CustomModules[m]);
        }

        void Modules_LateUpdatePreApply()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnLateUpdatePreApply(CustomModules[m]);
        }

        void Modules_PostLateUpdate()
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.OnPostLateUpdate(CustomModules[m]);
        }

        void Modules_Leg_LateUpdate(Leg leg)
        {
            if (UsingControlModules == false) return;
            if (DisableCustomModules) return;
            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.Leg_LateUpdate(CustomModules[m], leg);
        }

#if UNITY_EDITOR

        public void _Editor_ModulesOnSceneGUI()
        {
            if (UsingControlModules == false) return;
            if (CustomModules == null) return;
            if (DisableCustomModules) return;

            if ( Application.isPlaying == false)
            {
                for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].ModuleReference) if (CustomModules[m].Enabled) CustomModules[m].ModuleReference.Editor_OnSceneGUI(this, CustomModules[m]);
                return;
            }

            for (int m = 0; m < CustomModules.Count; m++) if (CustomModules[m].Enabled) CustomModules[m].PlaymodeModule.Editor_OnSceneGUI(this, CustomModules[m]);
        }

#endif  

    }
}