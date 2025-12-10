using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public class PoolableObject : MonoBehaviour
    {
        public bool IsInPool { get; set; } = false;

        // 초기화가 필요하면 여기에 구현 가능
        public virtual void OnGetFromPool()
        {
            IsInPool = false;
        }

        public virtual void OnReturnToPool()
        {
            IsInPool = true;
        }
    }

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
        
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// PoolManager 초기화
        /// </summary>

        // private void Update()
        // {
        //     if (!GameManager.Instance.IsLoad || GameManager.Instance.CurrentState != GameManager.GameState.Playing || IsInitialized) return;
        //     
        //     Init();
        // }
        public Task Init()
        {
            if (IsInitialized) return Task.CompletedTask;
            
            _pools = new Dictionary<string, ObjectPool<GameObject>>();
            _poolParents = new Dictionary<string, Transform>();

            foreach (PoolData poolData in poolsData)
            {
                string key = poolData.prefab.name;
                
                if (_pools.ContainsKey(key)) continue;

                ObjectPool<GameObject> pool = new(
                    createFunc: () =>
                    {
                        GameObject obj = InstantiateObject(poolData.prefab);
                        AddPoolableComponent(obj);
                        return obj;
                    },
                    actionOnGet: obj =>
                    {
                        var poolable = obj.GetComponent<PoolableObject>();
                        if (poolable != null)
                            poolable.OnGetFromPool();
                        obj.SetActive(true);
                    },
                    actionOnRelease: obj =>
                    {
                        OnRelease(obj);
                        var poolable = obj.GetComponent<PoolableObject>();
                        if (poolable != null)
                            poolable.OnReturnToPool();
                    },
                    actionOnDestroy: Destroy,
                    collectionCheck: false,
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
                    GameObject obj = InstantiateObject(poolData.prefab); // 컴포넌트 부착까지 보장
                    AddPoolableComponent(obj); // PoolableObject 부착 안 되어있으면 부착
                    pool.Release(obj);
                }

                _pools.Add(key, pool);
            }
            
            IsInitialized = true;
            return Task.CompletedTask;
        }

        private void AddPoolableComponent(GameObject obj)
        {
            if (obj.GetComponent<PoolableObject>() == null)
            {
                obj.AddComponent<PoolableObject>();
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

            if (_pools.TryGetValue(key, out var pool))
            {
                return pool.Get();
            }
            else
            {
                Debug.LogError($"Pool not found for key: {key}");
                return null;
            }
        }

        public GameObject GetObject(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool.Get();
            }
            else
            {
                Debug.LogError($"Pool not found for key: {key}");
                return null;
            }
        }

        public GameObject GetObject(GameObject prefab, Transform parent)
        {
            string key = prefab.name;
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogError($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetParent(parent);
            return go;
        }
        
        public GameObject GetObject(string key, Transform parent)
        {
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogError($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetParent(parent);
            return go;
        }
        
        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogError($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            string key = prefab.name;
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogError($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        /// <summary>
        /// 게임 오브젝트 반납하기
        /// </summary>
        public void ReleaseObject(GameObject obj, float delay = 0.0f)
        {
            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable == null)
            {
                // 풀에 없는 오브젝트는 그냥 Destroy
                Destroy(obj, delay);
                return;
            }

            if (poolable.IsInPool)
            {
                // 이미 풀에 반환된 상태면 중복 Release 방지 위해 무시
                Debug.LogWarning($"Attempted to release object '{obj.name}' that is already in pool.");
                return;
            }

            string key = obj.name;

            if (_pools.ContainsKey(key))
            {
                if (delay <= 0f)
                {
                    _pools[key].Release(obj);
                }
                else
                {
                    StartCoroutine(CoReleaseObject(obj, key, delay));
                }
            }
            else
            {
                // 풀에 없는 오브젝트 별도 처리
                Destroy(obj, delay);
            }
        }

        public void OnRelease(GameObject go)
        {
            string key = go.name.Replace("(Clone)", "").Trim();
            if (_poolParents.TryGetValue(key, out Transform parent))
            {
                go.transform.SetParent(parent);
            }
            else
            {
                Debug.LogWarning($"Pool parent not found for key: {key}");
                go.transform.SetParent(null);
            }
            go.SetActive(false);
        }

        public (bool, string) IsPooledObject(GameObject o)
        {
            string key = o.name.Replace("(Clone)", "").Trim();
            return (_pools.ContainsKey(key), key);
        }

        public IEnumerator CoReleaseObject(GameObject go, string key, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!_pools.ContainsKey(key))
            {
                Destroy(go);
                yield break;
            }

            var poolable = go.GetComponent<PoolableObject>();
            if (poolable != null && poolable.IsInPool)
            {
                Debug.LogWarning($"Attempted to release object '{go.name}' that is already in pool (delayed).");
                yield break;
            }

            _pools[key].Release(go);
        }
    }
}
