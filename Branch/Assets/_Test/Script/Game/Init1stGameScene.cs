using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Init1stGameScene : MonoBehaviour
{
    [SerializeField] private GameObject startPosition;

    private void Awake()
    {
        if (!startPosition) return;
        
        DungeonManager.Instance.SetStartPosition(startPosition);
        DungeonManager.Instance.RestartPosition = startPosition.transform.position;
    }
}
