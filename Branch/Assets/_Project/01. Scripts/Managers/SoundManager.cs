using System;
using UnityEngine;
using UnityEditor;

namespace Managers
{
    public class SoundManager : Singleton<SoundManager>
    {
        [SerializeField] private GameObject audioListenerPrefab;
        public GameObject AudioListener { get; private set; }

        private bool _isInit;

        public void Init()
        {
            if (_isInit) return;

            AudioListener = Instantiate(audioListenerPrefab);

            _isInit = true;
        }
    }
}