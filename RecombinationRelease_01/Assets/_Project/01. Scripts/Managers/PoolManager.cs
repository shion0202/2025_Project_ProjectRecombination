using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public class PoolManager : Singleton<PoolManager>
    {
        /// <summary>
        /// 풀링 데이터 구조체
        /// </summary>
        [Serializable]
        private struct PoolData
        {
            public GameObject prefab;
            public int defaultSize;
            public int maxSize;
        }
        
        [Tooltip("풀링을 적용할 게임오브젝트")][SerializeField] private PoolData[] poolsData;
        
        /// <summary>
        /// 실제 풀링 데이터 딕셔너리
        /// </summary>
        private Dictionary<string, ObjectPool<GameObject>> _pools;

        /// <summary>
        /// PoolManager 초기화
        /// </summary>
        private void Awake()
        {
            _pools = new Dictionary<string, ObjectPool<GameObject>>();

            foreach (PoolData poolData in poolsData)
            {
                string key = poolData.prefab.name;
                
                if (_pools.ContainsKey(key)) continue;

                ObjectPool<GameObject> pool = new(
                    createFunc: () => Instantiate(poolData.prefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => obj.SetActive(false),
                    actionOnDestroy: Destroy,
                    collectionCheck: false,
                    defaultCapacity: poolData.defaultSize,
                    maxSize: poolData.maxSize
                );

                for (int i = 0; i < poolData.defaultSize; i++)
                {
                    GameObject obj = pool.Get();
                    pool.Release(obj);
                }

                _pools.Add(key, pool);
            }
        }

        /// <summary>
        /// 게임 오브젝트 가져오기
        /// </summary>
        public GameObject GetObject(GameObject prefab)
        {
            string key = prefab.name;
            return _pools[key]?.Get();
        }

        public GameObject GetObject(string key)
        {
            return _pools[key]?.Get();
        }

        public GameObject GetObject(GameObject prefab, Transform parent)
        {
            string key = prefab.name;
            GameObject obj = _pools[key]?.Get();
            obj?.transform.SetParent(parent);
            return obj;
        }
        
        public GameObject GetObject(string key, Transform parent)
        {
            GameObject obj = _pools[key]?.Get();
            obj?.transform.SetParent(parent);
            return obj;
        }
        
        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;
            GameObject obj = _pools[key]?.Get();
            obj?.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// 게임 오브젝트 반납하기
        /// </summary>
        public void ReleaseObject(GameObject obj)
        {
            // string key = obj.name.Replace("(Clone)", "").Trim();
            (bool keyExists, string key) = IsPooledObject(obj);
            if (keyExists) _pools[key].Release(obj);
            else Destroy(obj);
        }

        public (bool, string) IsPooledObject(GameObject o)
        {
            // 오브젝트의 이름에서 "(Clone)" 제거
            string key = o.name.Replace("(Clone)", "").Trim();
            return (_pools.ContainsKey(key), key);
        }
    }
}
