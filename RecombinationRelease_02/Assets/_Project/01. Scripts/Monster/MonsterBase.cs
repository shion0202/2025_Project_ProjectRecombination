using UnityEngine;

namespace Monster
{
    public class MonsterBase : MonoBehaviour
    {
        // 테스트용 스탯
        public float Hp = 100.0f;

        public void TakeDamage(int damage)
        {
            Hp = Mathf.Clamp(Hp - damage, 0, Hp);
            Debug.Log(gameObject.name + " damaged " + damage + ", (남은 체력: " + Hp + ")");

            if (Hp <= 0)
            {
                Die();
            }
        }
        
        // 테스트용 사망 함수
        public void Die()
        {
            Destroy(gameObject);
        }
    }
}