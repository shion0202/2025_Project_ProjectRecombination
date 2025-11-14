using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private Rigidbody[] ragdollRigidbodies;
    [SerializeField] private Collider[] ragdollColliders;
    
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody mainRigidbody;
    
    // public bool isRagdollActive;

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        mainRigidbody ??= GetComponent<Rigidbody>();
        
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdollState(false);
    }

    private void SetRagdollState(bool b)
    {
        if (animator is not null)
        {
            animator.enabled = !b;
        }
        
        if (mainRigidbody is not null)
            mainRigidbody.isKinematic = b;

        foreach (Rigidbody r in ragdollRigidbodies)
        {
            // if (r == mainRigidbody) continue;
            r.isKinematic = !b;
        }
        
        foreach (Collider c in ragdollColliders)
        {
            if (c.gameObject.name == "Heatbox") continue;
            c.enabled = b;
        }
    }

    public void ActivateRagdoll()
    {
        SetRagdollState(true);
    }

    public void DeactivateRagdoll()
    {
        SetRagdollState(false);
    }
    
    public void ApplyForceToRagdoll(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
    {
        foreach (Rigidbody r in ragdollRigidbodies)
        {
            r.AddForce(force, forceMode);
        }
    }

    // private void Update()
    // {
    //     SetRagdollState(isRagdollActive);
    // }
}
