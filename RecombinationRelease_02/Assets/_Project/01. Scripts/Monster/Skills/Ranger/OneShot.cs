using Monster.AI.Blackboard;
using System.Collections;
using UnityEngine;

namespace _Test.Skills.Ranger
{
    [CreateAssetMenu(fileName = "OneShot", menuName = "MonsterSkills/Ranger/OneShot")]
    public class OneShot: SkillData
    {
        public override IEnumerator Casting(Blackboard data)
        {
            // 캐스팅 시간 동안 움직이지 않고 플레이어를 향해 사격 자세 취함
            data.AnimatorParameterSetter.Animator.SetTrigger("FireReady");
            yield return base.Casting(data);
        }

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("원샷 시전!");
            data.AnimatorParameterSetter.Animator.SetTrigger("Fire");
            yield return null;
        }
    }
}