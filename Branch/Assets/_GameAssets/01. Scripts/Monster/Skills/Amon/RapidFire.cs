using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 난사(?)
    /// 캐스팅 없음
    /// - 일정 시간 동안 대상을 향해 탄 발사
    /// </summary>
    
    [CreateAssetMenu(fileName = "RapidFire", menuName = "MonsterSkills/Amon/RapidFire")]
    public class RapidFire : SkillData
    {
        public override IEnumerator Activate(Blackboard data)
        {
            // n초간 캐스팅 후 대상을 주변으로 탄을 분사
            Debug.Log("난사 시작!");
            float fireDuration = 3f; // 난사 지속 시간
            float elapsed = 0f;
            data.AnimatorParameterSetter.Animator.SetTrigger("Fire");
            while (elapsed < fireDuration)
            {
                // 탄 분사 로직 구현 (예: Instantiate(탄, 위치, 회전))
                Vector3 startPos = data.AttackInfo.firePoint.position;
                Vector3 targetPos = data.Target.transform.position + Vector3.up * 1.5f; // 타겟의 중심을 향하도록 약간 위로 조정
                Vector3 direction = (targetPos - startPos).normalized;
                
                data.AttackInfo.Fire(0, data.Agent, data.AttackInfo.firePoint.position, Vector3.zero, direction, 10f);
                
                elapsed += 0.2f; // 예시로 0.2초마다 탄 발사
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log("난사 종료");
            data.CurrentState = "Idle"; // 상태를 Idle로 강제 변경 (이후에 더 나은 방법을 찾아볼 것)
        }
    }
}