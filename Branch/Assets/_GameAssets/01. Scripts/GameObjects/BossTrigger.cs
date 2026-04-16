using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private FSM amonFirstPhasePrefab;
    [SerializeField] private FSM amonSecondPhasePrefab;
    [SerializeField] private Transform playerTeleportPoint;
    [SerializeField] private Transform playerRespawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (amonFirstPhasePrefab && amonFirstPhasePrefab != null)
            {
                Managers.DungeonManager.Instance.AmonFirstPhasePrefab = amonFirstPhasePrefab;
            }
            if (amonSecondPhasePrefab && amonSecondPhasePrefab != null)
            {
                Managers.DungeonManager.Instance.AmonSecondPhasePrefab = amonSecondPhasePrefab;
            }
            if (playerTeleportPoint && playerTeleportPoint != null)
            {
                Managers.DungeonManager.Instance.PlayerTeleportPoint = playerTeleportPoint;
            }
            if (playerRespawnPoint && playerRespawnPoint != null)
            {
                Managers.DungeonManager.Instance.PlayerRespawnPoint = playerRespawnPoint;
            }

            gameObject.SetActive(false);
        }
    }
}
