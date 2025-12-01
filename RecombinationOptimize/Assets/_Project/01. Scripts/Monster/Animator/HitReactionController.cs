using System.Collections;
using UnityEngine;

namespace _Project._01._Scripts.Monster.Animator
{
    public class HitReactionController : MonoBehaviour
    {
        [SerializeField] private float reactionDuration = 0.2f;
        
        // 피격 반응 재생
        public void TakeHit(Vector3 attackDirection, Vector3 hitPoint, Rigidbody hitRigidbody)
        {
            if (hitRigidbody != null)
            {
                StartCoroutine(ApplyHitReaction(hitRigidbody, attackDirection, hitPoint));
            }
        }

        private IEnumerator ApplyHitReaction(Rigidbody hitRigidbody, Vector3 attackDirection, Vector3 hitPoint)
        {
            hitRigidbody.isKinematic = false;   // 물리 효과 활성화

            float forceAmount = 100f;
            
            hitRigidbody.AddForceAtPosition(attackDirection * forceAmount, hitPoint, ForceMode.Impulse);
            
            yield return new WaitForSeconds(reactionDuration);
            
            hitRigidbody.isKinematic = true;    // 물리 효과 비활성화
        }
    }
}