// using _Test.Script.Bootstrap;
using Cinemachine;
using Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EPlayerPrefabType
{
    Player,
    Minimap,
    Volume,
    Navi,
    FollowCamera,
    MinimapCamera,
    WorldMapCamera,
    LowHp
}

[Serializable]
public struct PlayerInfo
{
    public GameObject prefab;
    public EPlayerPrefabType prefabType;
    public bool isActive;
}

public class InitPlayer : MonoBehaviour
{
    [SerializeField] private PlayerInfo[] initializePlayerPrefabs;

    private void Awake()
    {
        Dictionary<EPlayerPrefabType, GameObject> createdObj = new ();
        try
        {
            foreach (PlayerInfo obj in initializePlayerPrefabs)
            {
                if (obj.prefab is null) continue;
                if (createdObj.ContainsKey(obj.prefabType)) continue;
                
                GameObject objInstance = Instantiate(obj.prefab);
                objInstance.SetActive(obj.isActive);
                createdObj.Add(obj.prefabType, objInstance);
            }
            
            Init(createdObj);
            
            Destroy(gameObject);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static void Init(Dictionary<EPlayerPrefabType, GameObject> createdObj)
    {
        // 1. GameManager에 플레이어 오브젝트 등록
        if (createdObj.TryGetValue(EPlayerPrefabType.Player, out GameObject player))
        {
            GameManager.Instance.Player = player.GetComponent<PlayerController>();
            MonsterManager.Instance.Player = player;
        }
        
        // 2. PlayerController 초기화
        if (GameManager.Instance.Player is not null)
        {
            GameManager.Instance.Player.Init(createdObj);
        }
        
        // 3. FollowAudioListener 초기화
        // if (createdObj.TryGetValue(EPlayerPrefabType.Listener, out GameObject listener))
        // {
        //     listener.GetComponent<FollowAudioListener>()?.Init(player);
        // }

        if (createdObj.TryGetValue(EPlayerPrefabType.Minimap, out GameObject minimap))
        {
            GameManager.Instance.MinimapObject = minimap;
            minimap.GetComponent<FollowAudioListener>()?.Init(player);
        }
        
        SoundManager.Instance.AudioListener.GetComponent<FollowAudioListener>()?.Init(player);
        
        // 4. Minimap Camera 초기화
        if (createdObj.TryGetValue(EPlayerPrefabType.MinimapCamera, out GameObject minimapCamera))
        {
            minimapCamera.GetComponent<FollowMinimap>()?.Init(player);
        }
        
        // 5. Follow Camera 초기화
        if (createdObj.TryGetValue(EPlayerPrefabType.FollowCamera, out GameObject followCamera))
        {
            CinemachineVirtualCamera vCamera = followCamera.GetComponent<CinemachineVirtualCamera>();
            vCamera.Follow = player?.GetComponentInChildren<CameraTarget>().transform;
            vCamera.LookAt = player?.GetComponentInChildren<CameraTarget>().transform;
            
            GameManager.Instance.FollowCamera = followCamera;
        }
        
        // 6. Player Rig Aim 초기화
        if (GameManager.Instance.Player is not null)
        {
            // 게임 메니저가 관리하는 MainCamera를 이용하여 Rig Aim Controller 초기화
            RigAimController rigAimController = GameManager.Instance.Player.GetComponent<RigAimController>();
            if (Camera.main != null)
            {
                rigAimController.Init(Camera.main.gameObject);
            }
        }
    }
}
