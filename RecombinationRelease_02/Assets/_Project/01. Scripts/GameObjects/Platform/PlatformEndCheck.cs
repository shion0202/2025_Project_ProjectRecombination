using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformEndCheck : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Platform"))
        {
            player.TriggerPlatformEnd();
        }
    }
}
