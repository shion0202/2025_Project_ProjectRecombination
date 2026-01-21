using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Managers
{
    public class SoundManager : Singleton<SoundManager>
    {
        [SerializeField] private GameObject audioListenerPrefab;
        public GameObject AudioListener { get; private set; }

        private bool _isInit;

        public Task Init()
        {
            try
            {
                if (_isInit) return Task.CompletedTask;

                Debug.Log("[SoundManager] 초기화 시작");
                
                AudioListener = Instantiate(audioListenerPrefab);

                _isInit = true;
                
                Debug.Log("[SoundManager] 초기화 완료");
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SoundManager] 초기화 중 예외 발생: {e}");
                return Task.CompletedTask;
            }
        }
    }
}