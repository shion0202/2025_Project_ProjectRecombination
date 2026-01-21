using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    public partial class LegsAnimator
    {

        [System.Serializable]
        public class LegsAnimatorCustomModuleHelper
        {
            public bool Enabled = true;
            public LegsAnimator Parent;

            public LegsAnimatorControlModuleBase ModuleReference = null;
            public LegsAnimatorControlModuleBase PlaymodeModule { get; private set; }

            #region Get Module

            public LegsAnimatorControlModuleBase CurrentModule 
            {
                get
                {
#if UNITY_EDITOR
                    if (Application.isPlaying) return PlaymodeModule; else return ModuleReference;
#else
                    return PlaymodeModule;
#endif
                }
            }

            #endregion


            /// <summary> Can be used for containing any parasable value or just strings </summary>
            [SerializeField, HideInInspector] public List<string> customStringList = null;
            /// <summary> Support for list of unity objects </summary>
            [SerializeField, HideInInspector] public List<UnityEngine.Object> customObjectList = null;


            public LegsAnimatorCustomModuleHelper(LegsAnimator get)
            {
                Parent = get;
            }


            public void PreparePlaymodeModule(LegsAnimator parent)
            {
                if (PlaymodeModule != null) return;
                if (ModuleReference == null) return;
                PlaymodeModule = Instantiate(ModuleReference) as LegsAnimatorControlModuleBase;
                PlaymodeModule.Base_Init(parent, this);
            }

            public void DisposeModule()
            {
                if (PlaymodeModule != null) Destroy(PlaymodeModule);
                PlaymodeModule = null;
            }

            [SerializeField] private List<LegsAnimator.Variable> variables = new List<LegsAnimator.Variable>();

            public LegsAnimator.Variable RequestVariable(string name, object defaultValue)
            {
                if (variables == null) variables = new List<LegsAnimator.Variable>();

                int hash = name.GetHashCode();
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i].GetNameHash == hash) return variables[i];
                }

                LegsAnimator.Variable nVar = new LegsAnimator.Variable(name, defaultValue);
                variables.Add(nVar);
                return nVar;
            }

            #region Editor Code
#if UNITY_EDITOR

            [NonSerialized] public string formattedName = "";//
#endif

            #endregion

        }

    }
}