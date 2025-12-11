using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageUpdateTrigger : MonoBehaviour
{
    [SerializeField] private int stageIndex;
    
    private int _previousStageIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // DungeonManager.Instance.UpdateStage(stageIndex);
        _previousStageIndex = DungeonManager.Instance.CurrentPlayerStageIndex;
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (_previousStageIndex > stageIndex)
        {
            DungeonManager.Instance.UpdatePlayerStageIndex(stageIndex);
        }
        else if (_previousStageIndex == stageIndex)
        {
            DungeonManager.Instance.UpdatePlayerStageIndex(stageIndex + 1);
        }
    }
}
