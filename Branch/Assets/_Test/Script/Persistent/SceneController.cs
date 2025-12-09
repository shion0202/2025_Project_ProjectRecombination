using Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    private Dictionary<string, AsyncOperationHandle<SceneInstance>> _loadedScenes = new();
    
    // 씬을 추가로 로드하는 함수
    public async void LoadSceneAdditive(string key)
    {
        try
        {
            // 중복 로드 방지
            if (_loadedScenes.ContainsKey(key)) return;

            Debug.Log($"[SceneController] {key} 로드 시작 (Additive)...");

            // Addressables로 씬 로드
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);
        
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 성공 시 핸들 저장
                _loadedScenes.Add(key, handle);
                SceneInstance sceneInstance = handle.Result;

                // ★ 중요: 맵(Map) 씬을 로드했다면, 거기를 Active Scene으로 만들어야
                // 몬스터 생성 시 그 씬 안에 들어갑니다. (UI 씬 제외)
                if (!key.Contains("UI")) 
                {
                    SceneManager.SetActiveScene(sceneInstance.Scene);
                }
            
                Debug.Log($"[SceneController] {key} 로드 완료!");
            }
            else
            {
                Debug.LogError($"[SceneController] {key} 로드 실패!");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"{key} 로드 중 예외 발생: {e.Message}");
        }
    }

    // 씬을 언로드하는 함수
    public async void UnloadScene(string key)
    {
        try
        {
            if (!_loadedScenes.TryGetValue(key, out AsyncOperationHandle<SceneInstance> handle)) return;

            // Addressables로 씬 언로드
            await Addressables.UnloadSceneAsync(handle).Task;

            _loadedScenes.Remove(key);
            Debug.Log($"[SceneController] {key} 언로드 완료.");
        }
        catch (Exception e)
        {
            Debug.Log($"{key} 언로드 중 예외 발생: {e.Message}");
        }
    }
}
