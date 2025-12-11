using Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EManagerType
{
    GameManager,
    GUIManager,
    DataManager,
    MainCamera,
    DungeonManager,
    MapManager,
    MonsterManager,
    PoolManager,
    SceneManager,
    SoundManager,
    EventManager,
}

[Serializable]
public struct ManagerInfo
{
    public EManagerType managerType;
    public GameObject managerPrefab;
}

public class InitPersistent : MonoBehaviour
{
    [SerializeField] private ManagerInfo[] managersPrefabs;

    private void Awake()
    {
        try
        {
            Debug.Log($"persistent InitPersistent Awake()");
            
            Dictionary<EManagerType, GameObject> createdManagers = new ();
            
            foreach (ManagerInfo managerInfo in managersPrefabs)
            {
                if (!managerInfo.managerPrefab) continue;
                if (createdManagers.ContainsKey(managerInfo.managerType)) continue;

                Debug.Log($"{managerInfo.managerPrefab.name} Instantiate()");
                GameObject managerInstance = Instantiate(managerInfo.managerPrefab);
                
                createdManagers.Add(managerInfo.managerType, managerInstance);
            }
            
            Debug.Log($"persistent LoadPersistent()"); // 모든 매니저가 로드되었음을 GameManager에 알림
            GameManager.Instance.MainCamera = createdManagers[EManagerType.MainCamera];
            GameManager.Instance.SceneLoaded();
            
            Destroy(gameObject);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    
    private void OnDestroy()
    {
        GameManager.Instance.EnterTitle();
    }
}
