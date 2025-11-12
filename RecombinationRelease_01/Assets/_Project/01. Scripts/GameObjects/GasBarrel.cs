using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasBarrel : GlobalGameObject
{
    // [SerializeField] private List<string> target;
    [SerializeField] private List<string> target;
    
    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("OnCollisionEnter called " + other.gameObject.name);
        if (!CheckTriggerTarget(other)) return;
        Debug.Log(other.gameObject.name);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log("OnCollisionEnter called " + other.gameObject.name);
        if (!CheckCollisionTarget(collision)) return;
        Debug.Log(collision.gameObject.name);
        Destroy(gameObject);
    }

    private bool CheckTriggerTarget(Collider collision)
    {
        foreach (var item in target)
        {
            if (collision.CompareTag(item))
                return true;
        }
        return false;
    }
    
    private bool CheckCollisionTarget(Collision collision)
    {
        foreach (var item in target)
        {
            if (collision.gameObject.CompareTag(item))
                return true;
        }
        return false;
    }
}
