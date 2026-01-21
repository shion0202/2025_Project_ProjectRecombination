using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 영혼 보주
    /// - 캐스팅 시간 동안 움직이지 않고 플레이어를 응시하며 에너지를 모으는 연출 재생
    /// - 캐스팅 완료 시 플레이어를 향해 레이저를 4초간 발사
    /// - 레이저가 플레이어를 맞출 때마다 지속 대미지 적용
    /// - 레이저가 플레이어를 추적할지 아니면 고정될지는 기획이 확정되지 않아 일단 고정 레이저로 구현
    /// - 추후 기획이 확정되면 내용을 추가하여 구현 필요
    /// </summary>
    
    [CreateAssetMenu(fileName = "SoulOrb", menuName = "MonsterSkills/Amon/SoulOrb")]
    public class SoulOrb: SkillData
    {
        public override IEnumerator Casting(Monster.AI.Blackboard.Blackboard data)
        {
            // return base.Casting(data);
            data.AnimatorParameterSetter.Animator.SetTrigger("Charge");
            yield return new WaitForSeconds(castTime);
        }
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            Debug.Log("영혼 보주 시전!");
            yield return null;
        }
    }
}