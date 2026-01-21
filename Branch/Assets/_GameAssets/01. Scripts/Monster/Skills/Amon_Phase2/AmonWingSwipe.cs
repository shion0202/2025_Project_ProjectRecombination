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
    
    [CreateAssetMenu(fileName = "WingSwipe", menuName = "MonsterSkills/Amon_Phase2/WingSwipe")]
    public class AmonWingSwipe : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject meleeCollisionPrefab;               // 근접 공격 범위를 판단할 프리팹 오브젝트
        [SerializeField] private Vector3 collisionScale;                        // 근접 공격 범위
        [SerializeField] private Vector3 collisionOffset;                       // 근접 공격 위치
        [SerializeField] private List<AnimationClip> wingAttackclips = new();   // 근접 공격 캐스팅 및 공격 애니메이션
                                                                                // 0: 왼쪽 캐스팅, 1: 왼쪽 공격, 2: 오른쪽 캐스팅, 3: 오른쪽 공격
        private GameObject meleeCollisionObject;
        private bool isLeftAttack = false;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 근접 공격 시작");

            // 3. 공격 애니메이션 재생 및 공격 판정 활성화
            data.AnimatorParameterSetter.Animator.speed = 1.0f;
            if (isLeftAttack)
            {
                data.AnimatorParameterSetter.Animator.SetTrigger("AttackLeftTrigger");
            }
            else
            {
                data.AnimatorParameterSetter.Animator.SetTrigger("AttackRightTrigger");
            }

            //meleeCollision.gameObject.SetActive(true);
            meleeCollisionObject = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            AmonMeleeCollision meleeCollision = meleeCollisionObject.GetComponent<AmonMeleeCollision>();
            if (meleeCollision)
            {
                meleeCollision.Init(damage, collisionScale, collisionOffset);
            }

            // Attack Time
            yield return new WaitForSeconds(wingAttackclips[isLeftAttack ? 2 : 3].length);

            // 4. 공격 판정 비활성화 및 Idle 상태 전환
            //meleeCollision.gameObject.SetActive(false);
            Utils.Destroy(meleeCollisionObject);
            data.AnimatorParameterSetter.Animator.SetTrigger("IdleTrigger");

            Debug.Log("[Amon Phase 2] 근접 공격 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 근접 공격 준비");

            // 0. 플레이어 방향 쳐다보기 (Y축 회전만)
            // Target Point
            Vector3 lookDir = data.Target.transform.position - data.Agent.transform.position;
            lookDir.y = 0;
            data.Agent.transform.rotation = Quaternion.LookRotation(lookDir);

            // 1. 왼쪽 or 오른쪽 공격 결정 (50% 확률)
            isLeftAttack = UnityEngine.Random.value < 0.5f;

            // 2. 캐스팅 애니메이션 재생
            if (isLeftAttack)
            {
                data.AnimatorParameterSetter.Animator.SetTrigger("CastLeftTrigger");
            }
            else
            {
                data.AnimatorParameterSetter.Animator.SetTrigger("CastRightTrigger");
            }
            data.AnimatorParameterSetter.Animator.speed = animSpeed;
            //castTime = wingAttackclips[isLeftAttack ? 0 : 1].length / animSpeed;

            return base.Casting(data);
        }
    }
}
