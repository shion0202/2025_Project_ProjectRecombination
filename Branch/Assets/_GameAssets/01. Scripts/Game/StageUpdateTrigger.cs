using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageUpdateTrigger : MonoBehaviour
{
    [SerializeField] private int stageIndex;
    [SerializeField] private GameObject respawnPosition;
    
    private int _previousStageIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        //DungeonManager.Instance.UpdateStage(stageIndex);
        _previousStageIndex = DungeonManager.Instance.CurrentPlayerStageIndex;
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 플레이어가 뒤로 이동
        if (_previousStageIndex > stageIndex)
        {
            DungeonManager.Instance.UpdatePlayerStageIndex(stageIndex);
        }
        // 플레이어가 앞으로 이동
        else if (_previousStageIndex == stageIndex)
        {
            DungeonManager.Instance.UpdatePlayerStageIndex(stageIndex + 1);
            DungeonManager.Instance.RestartPosition = respawnPosition.transform.position;
        }
    }
}
