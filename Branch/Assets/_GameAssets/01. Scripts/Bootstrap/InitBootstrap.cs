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

    /// <summary>
    /// 최대 프레임을 MaxFrameRate로 제한한다.
    /// 수직 동기화가 켜져 있으면 targetFrameRate가 무시되고 모니터 주사율(144Hz 등)까지 올라가므로,
    /// 주사율이 목표 이하인 모니터에서는 수직 동기화로 맞추고(화면 찢김 없음),
    /// 더 높은 모니터에서는 수직 동기화를 끄고 targetFrameRate로 묶는다.
    /// </summary>
    private static void ApplyFrameRateLimit()
    {
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;
        if (refreshRate > 0.0 && refreshRate <= MaxFrameRate + 1)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MaxFrameRate;
        }

        Debug.Log($"[InitBootstrap] 프레임 제한: 모니터 {refreshRate:F0}Hz, VSync {QualitySettings.vSyncCount}, 목표 {Application.targetFrameRate}");
    }

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

    // 목표 프레임. 이보다 높게 올라가면 프레임이 들쭉날쭉해지기 쉬워 상한으로 고정한다.
    private const int MaxFrameRate = 60;

    private static void Init()
    {
        ApplyFrameRateLimit();

        // 현재 기기의 화면 비율(Aspect Ratio) 계산
        float targetAspectRatio = (float)Screen.width / (float)Screen.height;

        // 기준 가로폭. 화면 비율은 기기를 따라가고 가로폭만 이 값으로 맞춘다.
        // 16:9 화면이라면 아래 값 그대로의 해상도가 나온다.
#if UNITY_ANDROID || UNITY_IOS
        // 모바일은 성능을 위해 의도적으로 낮춘다. (세로 모드 기준 HD 가로폭)
        int targetWidth = 720;
#else
        // PC는 FHD 기준. 행사 부스 모니터에서 업스케일로 흐려지지 않도록 한다.
        int targetWidth = 1920;
#endif
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
