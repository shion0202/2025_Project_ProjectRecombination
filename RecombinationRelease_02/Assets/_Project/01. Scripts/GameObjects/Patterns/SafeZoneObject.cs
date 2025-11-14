using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeZoneObject : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player)
            {
                player.SetPlayerState(EPlayerState.Invincibility, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player)
            {
                player.SetPlayerState(EPlayerState.Invincibility, false);
            }
        }
    }
}
