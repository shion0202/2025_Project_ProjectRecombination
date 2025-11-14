using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Monster.AI.Command
{
    public class EvasionCommand : AICommand
    {
        private Vector3 evasionDirection;
        private Vector3 targetPosition;
        private float distanceToTarget;
        const float evasionDistance = 3.0f; // 회피 거리
        const float evasionSpeed = 5.0f; // 회피 속도

        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, processError);
            if (!CheckBlackboard(blackboard))
            {
                OnExit(blackboard);
                processError?.Invoke();
                return;
            }

            // 회피 방향 설정 (좌 또는 우)
            evasionDirection = (Random.value > 0.5f)
                ? blackboard.Agent.transform.right
                : -blackboard.Agent.transform.right;
            targetPosition = blackboard.Agent.transform.position + evasionDirection * evasionDistance;
            targetPosition.y = blackboard.Agent.transform.position.y; // y축 고정

            // 목표 지점과 agent 사이에 장애물이 있는지 확인
            if (Physics.Raycast(blackboard.Agent.transform.position, evasionDirection, out RaycastHit hitInfo, evasionDistance))
            {
                // 장애물이 agent와 가까우면 반대 방향으로 회피
                if (hitInfo.distance < evasionDistance / 2)
                {
                    evasionDirection = -evasionDirection;
                }
                else
                {
                    // 장애물에서 agent 방향으로 약간 떨어진 지점으로 설정
                    targetPosition = hitInfo.point - evasionDirection * 0.5f;
                    targetPosition.y = blackboard.Agent.transform.position.y; // y축 고정
                }
            }

            // TODO: 현재 사용할 회피 애니메이션이 없음으로 직접 Transform을 이동시키는 방식으로 구현
            // 추후 애니메이션 적용 시 로직 수정

            distanceToTarget = Vector3.Distance(blackboard.Agent.transform.position, targetPosition);
        }

        public override void Execute(Blackboard.Blackboard blackboard, System.Action onComplete)
        {
            if (CheckBlackboard(blackboard)) return;

            // if (blackboard.State is MonsterState.Evasion) yield break;

            if (distanceToTarget > 0.1f)
            {
                blackboard.NavMeshAgent.Move(evasionDirection * (evasionSpeed * Time.deltaTime));
                distanceToTarget = Vector3.Distance(blackboard.Agent.transform.position, targetPosition);
            }
            else
            {
                blackboard.NavMeshAgent.ResetPath();
                blackboard.NavMeshAgent.isStopped = true;
                OnExit(blackboard);
                onComplete?.Invoke();
            }
        }

        private static bool CheckBlackboard(Blackboard.Blackboard blackboard)
        {
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot execute EvasionCommand.");
                return true;
            }
            // NavMeshAgent가 유효한지 확인
            if (blackboard.NavMeshAgent is null)
            {
                Debug.LogError("NavMeshAgent is null. Cannot execute EvasionCommand.");
                return true;
            }
            if (blackboard.Agent is null)
            {
                Debug.LogError("Agent is null. Cannot execute EvasionCommand.");
                return true;
            }

            return false;
        }
    }
}