using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Test.Skills
{
    [CreateAssetMenu(fileName = "Teleport", menuName = "MonsterSkills/Amon/Teleport")]
    public class Teleport : SkillData
    {
        /// <summary>
        /// 스킬 이름: 순간 이동
        /// - 캐스팅: 0.2초 (캐스팅 중 이동 불가 상태)
        /// - 효과: 시전 시 플레이어 주변 n미터 내 랜덤 위치로 순간 이동
        /// </summary>
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            Debug.Log("순간 이동!");
            Vector3 randomDirection = Random.insideUnitSphere * 5f; // 플레이어 주변 5미터 내 랜덤 위치
            randomDirection.y = 0; // 수평면에서만 이동
            Vector3 targetPosition = data.Target.transform.position + randomDirection;
            data.Agent.transform.position = targetPosition;
            yield return new WaitForSeconds(0.1f); // 잠시 대기
            Debug.Log("순간 이동 완료");
            data.CurrentState = "Idle"; // 상태를 Idle로 강제 변경 (이후에 더 나은 방법을 찾아볼 것)
        }
    }
}