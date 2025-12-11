using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class InitBootstrap : MonoBehaviour
{
    [SerializeField] private string persistentSceneAddress = "Scene_Persistent";
    // [SerializeField] private string loadingSceneAddress = "Scene_Loading";
    
    private async void Start()
    {
        try
        {
            Debug.Log("[InitBootstrap] 시스템 초기화 시작...");
            Init();
        
            // Persistent 씬 로드
            await LoadPersistentScene();
        
            Debug.Log("[InitBootstrap] 시스템 초기화 완료!");

            SceneManager.UnloadSceneAsync(gameObject.scene); // InitBootstrap 씬 언로드
        }
        catch (Exception e)
        {
            Debug.LogError($"[InitBootstrap] 시스템 초기화 중 오류 발생: {e.Message}");
        }
    }

    private static void Init()
    {
        Application.targetFrameRate = 60;
    }

    private async Task LoadPersistentScene()
    {
        try
        {
            Debug.Log("[InitBootstrap] Persistent 씬 로딩 중...");
        
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(persistentSceneAddress, LoadSceneMode.Additive); 
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                SceneManager.SetActiveScene(handle.Result.Scene);
        
            Debug.Log("[InitBootstrap] Persistent 씬 로딩 완료!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[InitBootstrap] Persistent 씬 로드 실패: {e.Message}");
        }
    }
}
