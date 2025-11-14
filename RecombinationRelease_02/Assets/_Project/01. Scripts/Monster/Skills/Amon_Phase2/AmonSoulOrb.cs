using Monster.AI.Blackboard;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 돌진 (2페이즈)
    /// - 캐스팅: 1.5초
    /// - 효과: 대상을 향해 돌진, 적중 시 넉백 및 대미지
    /// - 예외처리1: 캐스팅 중 스턴, 넉백, 이동 불가 상태가 되면 스킬 취소
    /// - 예외처리2: 대상과 거리가 너무 멀 경우 스킬 취소
    /// - 예외처리3: 돌진 중 장애물에 부딪히면 돌진 취소
    /// </summary>
    
    [CreateAssetMenu(fileName = "SoulOrb", menuName = "MonsterSkills/Amon_Phase2/SoulOrb")]
    public class AmonSoulOrb : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private List<Vector3> bulletSpawnOffset = new();

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 보주 시작");

            // 2. 총알 생성 및 발사
            for (int i = 0; i < bulletSpawnOffset.Count; ++i)
            {
                Vector3 startPosition = data.Agent.transform.position + bulletSpawnOffset[i];
                Vector3 direction = data.Agent.transform.forward;
                direction.y = 0.0f;
                direction.Normalize();
                GameObject orb = Utils.Instantiate(bulletPrefab, startPosition, Quaternion.LookRotation(direction));
                Bullet bullet = orb.GetComponent<Bullet>();
                if (bullet)
                {
                    bullet.Init(data.Agent, data.Target.transform, startPosition, Vector3.zero, direction, damage);
                }
            }
            data.AnimatorParameterSetter.Animator.SetBool("isCharging", false);

            Debug.Log("[Amon Phase 2] 영혼 보주 종료");
            yield break;
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 보주 준비");

            // 1. 캐스팅 애니메이션 재생
            data.AnimatorParameterSetter.Animator.SetBool("isCharging", true);

            Vector3 direction = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < castTime)
            {
                direction = data.Target.transform.position - data.Agent.transform.position;
                direction.y = 0; // y축 회전만 적용

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    data.Agent.transform.rotation = Quaternion.Slerp(data.Agent.transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                elapsed += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }

            //return base.Casting(data);
        }
    }
}

// TODO: 리지드바디를 이용해 돌진을 구현하였으나 물체와 충돌 후 멈추는 것이 자연스럽지 않음 (오히려 관성으로 더 멀리 감)
// 제자리에 멈출 수 있는 방법이 무었이 있을까?