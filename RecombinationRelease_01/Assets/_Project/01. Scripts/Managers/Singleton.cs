using UnityEngine;

namespace Managers
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = (T)FindObjectOfType(typeof(T));

                if (_instance != null) return _instance;
                var singleton = new GameObject();

                _instance = singleton.AddComponent<T>();
                singleton.name = typeof(T).Name + " (Singleton)";
                DontDestroyOnLoad(singleton);
                return _instance;
            }
        }
    }
}
