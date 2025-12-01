using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformEndCheck : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private void OnTriggerEnter(Collider other)
    {
        // Player를 기준으로 할지, Platform을 기준으로 할지 결정 필요
        if (other.gameObject.CompareTag("Player"))
        {
            player.TriggerPlatformEnd();
        }
    }
}
