using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace _Project._01._Scripts.Monster.Animator
{
    public class HitReactionController : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private MultiParentConstraint[] rigConstraints;
        [SerializeField] private float blendSpeed = 10f; // 블렌딩 속도
        [SerializeField] private float targetWeight; // 0: 애니메이션, 1: 래그돌
        [SerializeField] private float reactionDuration = 0.2f;
        
        // 피격 시 호출될 함수
        public void TakeHit(Vector3 attackDirection, Rigidbody hitGhostRb)
        {
            hitGhostRb.isKinematic = false; // 물리 효과 활성화
            hitGhostRb.AddForce(attackDirection * 100f, ForceMode.Impulse);
            
            targetWeight = 1.0f;
            
            StartCoroutine(BlendBackToAnimation(reactionDuration)); 
        }

        private IEnumerator BlendBackToAnimation(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            targetWeight = 0.0f;
        }

        private IEnumerator ApplyHitReaction(Rigidbody hitRigidbody, Vector3 attackDirection, Vector3 hitPoint)
        {
            hitRigidbody.isKinematic = false;   // 물리 효과 활성화

            float forceAmount = 100f;
            
            hitRigidbody.AddForceAtPosition(attackDirection * forceAmount, hitPoint, ForceMode.Impulse);
            
            yield return new WaitForSeconds(reactionDuration);
            
            hitRigidbody.isKinematic = true;    // 물리 효과 비활성화
        }

        private void Update()
        {
            if (rigConstraints == null || rigConstraints.Length == 0) return;
            
            // 현재 Weight를 목표 Weight로 매끄럽게 변경 (Lerp)
            float currentWeight = rigConstraints[0].weight; // 첫 번째 값으로 현재 값 확인
            float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * blendSpeed);

            // 모든 Constraint의 Weight를 한 번에 업데이트
            foreach (MultiParentConstraint constraint in rigConstraints)
            {
                constraint.weight = newWeight;
            }
        }
    }
}