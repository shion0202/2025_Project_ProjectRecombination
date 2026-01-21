using Monster.AI.Blackboard;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 날개 휘두르기
    /// - 아몬 2페이즈 날개 휘두르기 스킬
    /// - 기획서에서는 5미터 범위 내 적에게 한쪽 날개를 휘둘러 대미지를 주는 스킬이라 함
    /// - 부체꼴 범위 공격으로 몬스터 전방에 넓은 범위 공격
    /// - 추후 기획이 확정되면 내용을 추가하여 구현 필요
    /// </summary>
    
    [CreateAssetMenu(fileName = "WingSwipe", menuName = "MonsterSkills/Amon/WingSwipe")]
    public class WingSwipe: SkillData
    {
        private int _animationNumber;

        public override IEnumerator Casting(Blackboard data)
        {
            _animationNumber = Random.Range(1, 4); // 1,2,3 중 하나 랜덤
            // return base.Casting(data);
            
            // 해당 하는 번호의 애니메이션 재생
            switch (_animationNumber)
            {
                case 1:
                    // data.Agent.Animator.SetTrigger("WingSwipe1");
                    // data.AnimatorParameterSetter.Animator.SetInteger("WingSwipeReady", 1);
                    data.AnimatorParameterSetter.Animator.SetTrigger("LWingAttackReady");
                    break;
                case 2:
                    // data.Agent.Animator.SetTrigger("WingSwipe2");
                    // data.AnimatorParameterSetter.Animator.SetInteger("WingSwipeReady", 2);
                    data.AnimatorParameterSetter.Animator.SetTrigger("RWingAttackReady");
                    break;
                case 3:
                    // data.Agent.Animator.SetTrigger("WingSwipe3");
                    // data.AnimatorParameterSetter.Animator.SetInteger("WingSwipeReady", 3);
                    data.AnimatorParameterSetter.Animator.SetTrigger("DWingAttackReady");
                    break;
            }

            yield return new WaitForSeconds(castTime);
        }

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("날개 휘두르기 시전!");
            
            // 해당 하는 번호의 애니메이션 재생
            switch (_animationNumber)
            {
                case 1:
                    // data.Agent.Animator.SetTrigger("WingSwipe1");
                    // data.AnimatorParameterSetter.Animator.SetInteger("WingSwipe", 1);
                    data.AnimatorParameterSetter.Animator.SetTrigger("LWingAttack");
                    break;
                case 2:
                    // data.Agent.Animator.SetTrigger("WingSwipe2");
                    // data.AnimatorParameterSetter.Animator.SetInteger("WingSwipe", 2);
                    data.AnimatorParameterSetter.Animator.SetTrigger("RWingAttack");
                    break;
                case 3:
                    // data.Agent.Animator.SetTrigger("WingSwipe3");
                    // data.AnimatorParameterSetter.Animator.SetInteger("WingSwipe", 3);
                    data.AnimatorParameterSetter.Animator.SetTrigger("DWingAttack");
                    break;
            }
            
            yield return null;
        }
    }
}