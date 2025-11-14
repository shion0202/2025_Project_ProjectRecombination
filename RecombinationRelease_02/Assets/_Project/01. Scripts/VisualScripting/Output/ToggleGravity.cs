using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ToggleGravity : ProcessBase
    {
        [SerializeField] private Rigidbody targetRigidbody;
        
        public override void Execute()
        {
            if (targetRigidbody is null) return;
            IsOn = targetRigidbody.useGravity;
            targetRigidbody.useGravity = !IsOn;
        }
    }
}