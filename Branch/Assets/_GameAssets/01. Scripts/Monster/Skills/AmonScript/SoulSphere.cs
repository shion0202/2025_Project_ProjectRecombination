using UnityEngine;

namespace _Test
{
    public class SoulSphere: MonoBehaviour, IDamagable
    {
        // 영혼 구체는 타입이 2 종류로 나뉜다.
        // 1. 구체 주변에 플레이어에게 슬로우와 대미지를 주는 영역을 생성하는 타입
        // 2. 1번 타입의 구체에 일정 시간 후 폭발하여 넉백과 대미지를 주는 타입
        // 구체는 보스 몬스터의 스킬에 의해 소환된다.
        // 구체는 플레이어의 공격에 의해 파괴될 수 있다.
        [SerializeField] private int soulSphereType; // 1 or 2
        [SerializeField] private float HP = 100f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float slowAmount = 0.5f; // 50% 느려짐
        [SerializeField] private float slowRadius = 3f;
        [SerializeField] private float explosionRadius = 5f; // 폭발 반경
        [SerializeField] private float explosionForce = 500f; // 폭발 힘

        private void Update()
        {
            // HP가 0 이하가 되면 구체 파괴
            if (HP <= 0)
            {
                // 구체 파괴 효과와 사운드 재생
                Destroy(gameObject);
                return;
            }
            
            // 구체 타입에 따른 행동 구현
            if (soulSphereType == 1)
            {
                
            }
            else if (soulSphereType == 2)
            {
                
            }
        }
        
        private void SoulSphereTypeOneEffect()
        {
            // 플레이어 주변에 슬로우와 대미지를 주는 영역 생성
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, slowRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    // 플레이어에게 슬로우와 대미지 적용
                    // PlayerController player = hitCollider.GetComponent<PlayerController>();
                    // if (player != null)
                    // {
                    //     player.ApplySlow(slowAmount);
                    //     player.ApplyDamage(damage);
                    // }
                }
            }
        }
        
        private void SoulSphereTypeTwoEffect()
        {
            // 일정 시간 후 폭발하여 넉백과 대미지를 주는 효과
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    // 플레이어에게 넉백과 대미지 적용
                    Rigidbody playerRb = hitCollider.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                        playerRb.AddForce(direction * explosionForce);
                        // PlayerController player = hitCollider.GetComponent<PlayerController>();
                        // if (player != null)
                        // {
                        //     player.ApplyDamage(damage);
                        // }
                    }
                }
            }
        }

        public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1, float defenceIgnoreRate = 0)
        {
            HP -= damage;
        }
    }
}