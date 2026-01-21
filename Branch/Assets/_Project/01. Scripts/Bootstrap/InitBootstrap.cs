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

            // SceneManager.UnloadSceneAsync(gameObject.scene); // InitBootstrap 씬 언로드
        }
        catch (Exception e)
        {
            Debug.LogError($"[InitBootstrap] 시스템 초기화 중 오류 발생: {e.Message}");
        }
    }

    private static void Init()
    {
        // Application.targetFrameRate = 60;    // 프레임 레이트를 설정하는 것 때문에 CPU 사용량이 많을 수 있을 수 있다.
        
        // 현재 기기의 화면 비율(Aspect Ratio) 계산
        float targetAspectRatio = (float)Screen.width / (float)Screen.height;
    
        // 세로 모드 기준 HD 가로폭인 720을 타겟으로 설정 (가로 모드라면 1280)
        int targetWidth = 720; 
        int targetHeight = Mathf.RoundToInt(targetWidth / targetAspectRatio);

        // 해상도 변경 (세 번째 인자는 전체화면 여부)
        Screen.SetResolution(targetWidth, targetHeight, true);
    
        Debug.Log($"Resolution Set to: {targetWidth} x {targetHeight}");
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
