using System.Collections;
using UnityEngine;

namespace _Test.Skills.Ranger
{
    /// <summary>
    /// 스킬 이름: 차징 샷
    /// - 캐스팅 시간 동안 움직이지 않고 플레이어를 향해 사격 자세
    /// - 캐스팅 완료 시 플레이어를 향해 여러발의 총알 발사
    /// </summary>
    [CreateAssetMenu(fileName = "ChargingShot", menuName = "MonsterSkills/Ranger/ChargingShot")]
    public class ChargingShot: SkillData
    {
        // public int bulletCount = 1; // 발사할 총알 수
        // public float bulletInterval = 0.0f; // 총알 발사 간격
        public override IEnumerator Casting(Monster.AI.Blackboard.Blackboard data)
        {
            // 캐스팅 시간 동안 움직이지 않고 플레이어를 향해 사격 자세 취함
            data.AnimatorParameterSetter.Animator.SetTrigger("FireReady");
            yield return base.Casting(data);
        }
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            Debug.Log("차징샷 시전!");
            
            data.AnimatorParameterSetter.Animator.SetTrigger("Fire");
            yield return null;
        }
    }
}