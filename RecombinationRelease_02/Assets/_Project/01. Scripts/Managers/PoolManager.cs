using System;
using System.Collections;
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

        // 풀링 데이터 하이어라키 관리를 위한 딕셔너리
        // 하이어라키에서 풀링된 데이터를 보기 쉽게 하기 위한 용도로, 실제 빌드 시 삭제 가능
        private Dictionary<string, Transform> _poolParents;

        /// <summary>
        /// PoolManager 초기화
        /// </summary>
        private void Awake()
        {
            _pools = new Dictionary<string, ObjectPool<GameObject>>();
            _poolParents = new Dictionary<string, Transform>();

            foreach (PoolData poolData in poolsData)
            {
                string key = poolData.prefab.name;
                
                if (_pools.ContainsKey(key)) continue;

                ObjectPool<GameObject> pool = new(
                    createFunc: () => InstantiateObject(poolData.prefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => OnRelease(obj),
                    actionOnDestroy: Destroy,
                    collectionCheck: true,
                    defaultCapacity: poolData.defaultSize,
                    maxSize: poolData.maxSize
                );

                Transform parent = (new GameObject($"{key} Pool")).transform;
                parent.SetParent(Instance.transform);
                if (!_poolParents.ContainsKey(parent.name))
                {
                    _poolParents.Add(key, parent);
                }

                for (int i = 0; i < poolData.defaultSize; i++)
                {
                    //GameObject obj = pool.Get();
                    GameObject obj = InstantiateObject(poolData.prefab);
                    pool.Release(obj);
                }

                _pools.Add(key, pool);
            }
        }

        public GameObject InstantiateObject(GameObject prefab)
        {
            GameObject go = Instantiate(prefab);
            go.name = prefab.name;
            go.transform.SetParent(_poolParents[prefab.name], false);
            return go;
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

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            string key = prefab.name;
            GameObject obj = _pools[key]?.Get();
            obj?.transform.SetParent(parent);
            obj?.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// 게임 오브젝트 반납하기
        /// </summary>
        public void ReleaseObject(GameObject obj, float delay = 0.0f)
        {
            // string key = obj.name.Replace("(Clone)", "").Trim();
            (bool keyExists, string key) = IsPooledObject(obj);
            //if (keyExists) _pools[key].Release(obj);
            if (keyExists) StartCoroutine(CoReleaseObject(obj, key, delay));
            else Destroy(obj, delay);
        }

        public void OnRelease(GameObject go)
        {
            go.transform.SetParent(_poolParents[go.name]);
            go.SetActive(false);
        }

        public (bool, string) IsPooledObject(GameObject o)
        {
            // 오브젝트의 이름에서 "(Clone)" 제거
            //string key = o.name.Replace("(Clone)", "").Trim();
            string key = o.name;
            return (_pools.ContainsKey(key), key);
        }

        public IEnumerator CoReleaseObject(GameObject go, string key, float delay)
        {
            yield return new WaitForSeconds(delay);

            try
            {
                _pools[key].Release(go);
            }
            catch (InvalidOperationException ex)
            {
                // 이미 Release된 오브젝트일 경우 에러 메시지 반환 또는 무시
                Debug.LogWarning(ex.Message);
            }
        }
    }
}
