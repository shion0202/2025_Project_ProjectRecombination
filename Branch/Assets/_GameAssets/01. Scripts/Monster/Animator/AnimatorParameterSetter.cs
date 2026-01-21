using System.Collections.Generic;
using UnityEngine;

namespace _Project._01._Scripts.Monster.Animator
{
    public class AnimatorParameterSetter : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Animator animator;
        [SerializeField] private List<string> boolParameterNames;
        [SerializeField] private List<int> boolParameterHashes;

        [ContextMenu("Set Parameters")]
        private void FindAndSetParameters()
        {
            if (animator == null)
            {
                animator = GetComponent<UnityEngine.Animator>();
                if (animator == null)
                {
                    Debug.LogError("Animator component not found!");
                    return;
                }
            }

            boolParameterNames.Clear();
            boolParameterHashes.Clear();
            
            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    boolParameterNames.Add(param.name);
                    boolParameterHashes.Add(param.nameHash);
                }
            }

            Debug.Log($"Found {boolParameterNames.Count} bool parameters.");
        }
        
        #region Properties
        
        public UnityEngine.Animator Animator
        {
            get => animator;
            set => animator = value;
        }
        
        public List<string> BoolParameterNames
        {
            get => boolParameterNames;
            set => boolParameterNames = value;
        }
        
        public List<int> BoolParameterHashes
        {
            get => boolParameterHashes;
            set => boolParameterHashes = value;
        }
        
        public AnimatorStateInfo CurrentAnimatorStateInfo => Animator.GetCurrentAnimatorStateInfo(0);

        #endregion
    }
}