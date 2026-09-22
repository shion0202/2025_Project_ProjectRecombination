using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public sealed class PoolableObject : MonoBehaviour
    {
        public bool IsInPool { get; private set; }

        // 초기화가 필요하면 여기에 구현 가능
        public void OnGetFromPool()
        {
            IsInPool = false;
        }

        public void OnReturnToPool()
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
        
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// PoolManager 초기화
        /// </summary>
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
                        // 풀이 비어 새로 만든 오브젝트도 GetObject에서 위치를 정한 뒤 활성화되도록 비활성 상태로 넘긴다.
                        // (미리 생성하는 오브젝트와 같은 상태)
                        obj.SetActive(false);
                        return obj;
                    },
                    actionOnGet: obj =>
                    {
                        var poolable = obj.GetComponent<PoolableObject>();
                        if (poolable != null)
                            poolable.OnGetFromPool();
                        // 활성화는 여기서 하지 않고 GetObject에서 위치를 정한 뒤에 한다. (Activate 주석 참고)
                    },
                    actionOnRelease: obj =>
                    {
                        OnRelease(obj);
                        obj.name = poolData.prefab.name;
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

                for (int i = 0; i < poolData.defaultSize; ++i)
                {
                    GameObject obj = InstantiateObject(poolData.prefab);
                    AddPoolableComponent(obj);

                    // 유니티 풀의 Release를 거치지 않고 수동으로 세팅
                    obj.transform.SetParent(_poolParents[key]);
                    obj.name = poolData.prefab.name;

                    var poolable = obj.GetComponent<PoolableObject>();
                    if (poolable != null)
                    {
                        poolable.OnReturnToPool();
                    }

                    obj.SetActive(false);

                    // pool.Release(obj) 대신 내장 풀 내부 버퍼에 안전하게 수동 주입하거나, 
                    // 사실 가장 깔끔한 것은 그냥 이 상태로 둔 뒤, 처음 Get()이 일어날 때 풀이 알아서 
                    // 개수를 채우거나 createFunc를 타게 만드는 것입니다.
                    // 만약 유니티 내장 풀에 '정석'으로 미리 집어넣고 싶다면 아래 한 줄만 호출합니다.
                    pool.Release(obj);
                }

                _pools.Add(key, pool);
            }
            
            IsInitialized = true;
            return Task.CompletedTask;
        }

        private void AddPoolableComponent(GameObject obj)
        {
            if (!obj.GetComponent<PoolableObject>())
            {
                obj.AddComponent<PoolableObject>();
            }
        }

        public GameObject InstantiateObject(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, _poolParents[prefab.name], false);
            go.name = prefab.name;
            return go;
        }

        /// <summary>
        /// 게임 오브젝트 가져오기
        /// </summary>
        /// <summary>
        /// 풀에서 꺼낸 오브젝트를 활성화한다. 반드시 위치/부모를 정한 "뒤에" 호출한다.
        ///
        /// 반납된 오브젝트는 마지막 위치(탄환이면 벽에 맞은 지점)에 비활성 상태로 남아 있다.
        /// 먼저 활성화하고 위치를 옮기면, 활성화되는 순간 콜라이더가 이전 위치에서 물리에 등록되어
        /// 그곳의 벽과 겹친 것으로 판정된다. 탄환이 발사 직후 멀리 있는 벽에 맞은 것처럼 처리되어
        /// 플레이어 바로 앞에서 터지던 원인이다.
        /// </summary>
        private static GameObject Activate(GameObject go)
        {
            go.SetActive(true);
            return go;
        }

        public GameObject GetObject(GameObject prefab)
        {
            string key = GetOriginalKey(prefab.name);

            if (_pools.TryGetValue(key, out var pool))
            {
                return Activate(pool.Get());
            }
            else
            {
                Debug.LogWarning($"Pool not found for key: {key}");
                return null;
            }
        }

        public GameObject GetObject(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return Activate(pool.Get());
            }
            
            Debug.LogWarning($"Pool not found for key: {key}");
            return null;
        }

        public GameObject GetObject(GameObject prefab, Transform parent)
        {
            string key = GetOriginalKey(prefab.name);
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogWarning($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetParent(parent);
            return Activate(go);
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
                Debug.LogWarning($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetParent(parent);
            return Activate(go);
        }

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = GetOriginalKey(prefab.name);
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogWarning($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetPositionAndRotation(position, rotation);
            return Activate(go);
        }

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            string key = GetOriginalKey(prefab.name);
            GameObject go = null;

            if (_pools.TryGetValue(key, out var pool))
            {
                go = pool.Get();
            }
            else
            {
                Debug.LogWarning($"Pool not found for key: {key}");
                return null;
            }

            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return Activate(go);
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

            string key = GetOriginalKey(obj.name);

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
            string key = GetOriginalKey(go.name);
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
            string key = GetOriginalKey(o.name);
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

            PoolableObject poolable = go.GetComponent<PoolableObject>();
            if (poolable && poolable.IsInPool)
            {
                Debug.LogWarning($"Attempted to release object '{go.name}' that is already in pool (delayed).");
                yield break;
            }

            _pools[key].Release(go);
        }

        /// <summary>
        /// 풀 모두 초기화하기
        /// </summary>
        public void ClearPools()
        {
            if (!IsInitialized) return;
            
            foreach (ObjectPool<GameObject> pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();

            IsInitialized = false;
        }

        private string GetOriginalKey(string name)
        {
            return name.Replace("(Clone)", "").Trim();
        }
    }
}
