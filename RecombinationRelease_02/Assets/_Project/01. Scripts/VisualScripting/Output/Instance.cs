using Managers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace _Project.Scripts.VisualScripting
{
    public class Instance : ProcessBase
    {
        [Tooltip("생성할 게임 오브젝트")] [SerializeField]
        private GameObject instancePrefab; // Prefab to instantiate

        [Tooltip("한번에 생성할 수량")] [SerializeField]
        private int count = 1;

        [Tooltip("생성 범위")] [SerializeField] private Vector3 spawnArea;

        [FormerlySerializedAs("_destroyTrigger")]
        [Tooltip("생성한 객체가 파괴 되었음을 감지할 객체")]
        [SerializeField] private IsDestroy destroyTrigger;
        [Tooltip("생성한 객체가 비활성화 되었음을 감지할 객체")]
        [SerializeField] private IsDisable disableTrigger;
        [Tooltip("생성한 객체가 사망했음을 감지할 객체")]
        [SerializeField] private IsDead deadTrigger;

        public override void Execute()
        {
            if (IsOn) return;

            if (instancePrefab is null)
            {
                Debug.LogError("Instance prefab is not assigned.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Debug.Log("유니티야아프지마");
                
                // 부모 오브젝트의 위치를 기준으로 무작위 위치 계산
                float randomX = Random.Range(-spawnArea.x / 2, spawnArea.x / 2);
                float randomY = Random.Range(-spawnArea.y / 2, spawnArea.y / 2); // 2D라면 0 또는 고정값, 3D라면 필요한 범위
                float randomZ = Random.Range(-spawnArea.z / 2, spawnArea.z / 2);

                Vector3 randomPosition = transform.position + new Vector3(randomX, randomY, randomZ);

                // GameObject obj = Instantiate(instancePrefab, transform);
                GameObject obj = PoolManager.Instance.GetObject(instancePrefab);
                // obj.transform.position = randomPosition;
                
                // 네비매시 에이전트가 있을 경우 Warp 사용
                NavMeshAgent agent = obj.GetComponentInChildren<NavMeshAgent>();
                if (agent is not null) agent.Warp(randomPosition);
                else obj.transform.position = randomPosition;
                
                destroyTrigger?.AddObject(obj.GetComponent<GlobalGameObject>());
                disableTrigger?.AddObject(obj.GetComponent<GlobalGameObject>());
                deadTrigger?.AddObject(obj.GetComponent<DamagableObject>());
            }
            IsOn = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(gameObject.transform.position, spawnArea);
        }
    }
}