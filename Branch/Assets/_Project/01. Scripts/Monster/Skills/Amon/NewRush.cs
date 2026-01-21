using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 돌진
    /// - 캐스팅: 2초 (캐스팅 중 이동 불가 상태)
    /// - 효과: 대상을 향해 돌진, 적중 시 넉백 및 대미지
    /// - 예외처리1: 캐스팅 중 스턴, 넉백, 이동 불가 상태가 되면 스킬 취소
    /// - 예외처리2: 대상과 거리가 너무 멀 경우 스킬 취소
    /// - 예외처리3: 돌진 중 장애물에 부딪히면 돌진 취소
    /// </summary>
    
    [CreateAssetMenu(fileName = "New_Rush", menuName = "MonsterSkills/Amon/New_Rush")]
    public class NewRush : SkillData
    {
        [SerializeField] private GameObject meleeCollisionPrefab;       // 근접 공격 범위를 판단할 프리팹 오브젝트
        [SerializeField] private Vector3 collisionScale;                // 근접 공격 범위
        [SerializeField] private Vector3 collisionOffset;               // 근접 공격 위치
        [SerializeField] private float rushSpeed;
        [SerializeField] private float rushDuration;
        private GameObject meleeCollisionObject;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 1] 돌진 시작");

            // 돌진 방향 설정
            Vector3 directionToTarget = (data.Target.transform.position - data.Agent.transform.position).normalized;
            data.Agent.transform.LookAt(data.Target.transform);
            data.AnimatorParameterSetter.Animator.SetTrigger("Rush");

            // 피해를 입힐 근접 공격 범위 오브젝트 생성
            meleeCollisionObject = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            AmonMeleeCollision meleeCollision = meleeCollisionObject.GetComponent<AmonMeleeCollision>();
            if (meleeCollision)
            {
                meleeCollision.Init(damage, collisionScale, collisionOffset);
            }

            float elapsed = 0f;
            // Sphere 반경 (몬스터 크기에 맞게 조절)
            float sphereRadius = 0.5f;

            while (elapsed < rushDuration)
            {
                // SphereCast로 돌진 경로상의 장애물 충돌 체크
                if (Physics.SphereCast(data.Agent.transform.position, sphereRadius, directionToTarget, out RaycastHit hit, rushSpeed * Time.deltaTime))
                {
                    Debug.Log("장애물에 부딪혀 돌진 취소: " + hit.collider.name);
                    break;
                }

                // 이동
                data.Agent.transform.position += directionToTarget * rushSpeed * Time.deltaTime;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 돌진 종료
            Debug.Log("[Amon Phase 1] 돌진 종료");
            data.AnimatorParameterSetter.Animator.SetTrigger("RushEnd");
            Utils.Destroy(meleeCollisionObject);
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 1] 돌진 준비");

            // 러시 준비 애니메이션 재생
            data.AnimatorParameterSetter.Animator.SetTrigger("RushReady");

            // 지정된 캐스팅 시간 동안 계속 회전
            float elapsed = 0f;
            while (elapsed < castTime)
            {
                Vector3 lookDir = data.Target.transform.position - data.Agent.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    lookDir.Normalize();
                    data.Agent.transform.rotation = Quaternion.LookRotation(lookDir);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}

// TODO: 리지드바디를 이용해 돌진을 구현하였으나 물체와 충돌 후 멈추는 것이 자연스럽지 않음 (오히려 관성으로 더 멀리 감)
// 제자리에 멈출 수 있는 방법이 무었이 있을까?