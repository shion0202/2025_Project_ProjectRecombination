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
            Vector3 playerPosition = data.Target.transform.position;

            // 플레이어 발밑에서 랜덤 지점까지 NavMesh 위로 이어지는 곳까지만 인정한다.
            // 랜덤 지점이 벽 너머/맵 밖이면 벽 앞에서 멈춘 지점으로 옮겨진다.
            if (!NavMeshMoveUtil.TryClampPath(playerPosition, playerPosition + randomDirection, out Vector3 targetPosition))
            {
                // 플레이어 주변에 이동 가능한 바닥이 없으면(맵 밖 등) 엉뚱한 곳에 떨어지지 않도록 이동을 취소한다.
                Debug.LogWarning("[Amon] 플레이어 주변에 NavMesh가 없어 순간 이동을 취소합니다.");
                yield break;
            }

            if (data.NavMeshAgent is not null)
            {
                // Warp가 트랜스폼과 에이전트 위치를 함께 옮긴다
                data.NavMeshAgent.Warp(targetPosition);
                data.NavMeshAgent.isStopped = true;
            }
            else
            {
                data.Agent.transform.position = targetPosition;
            }

            yield return null;
            Debug.Log("순간 이동 완료");
        }
    }
}