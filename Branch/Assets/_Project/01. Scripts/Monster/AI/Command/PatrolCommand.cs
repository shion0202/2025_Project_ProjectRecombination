using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public class PatrolCommand : AICommand
    {
        // private static bool _animationRunning;
        private static bool CheckBlackboard(Blackboard.Blackboard blackboard)
        {
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot execute Wander Command.");
                return false;
            }

            // Patrol 정보가 유효한지 확인
            if (blackboard.PatrolInfo == null)
            {
                Debug.LogWarning("PatrolInfo is not valid for PatrolCommand.");
                return false;
            }

            if (blackboard.PatrolInfo.wayPoints == null || blackboard.PatrolInfo.wayPoints.Length == 0)
            {
                Debug.LogWarning("Patrol waypoints are not set or invalid.");
                return false;
            }

            // NavMeshAgent가 유효한지 확인
            if (blackboard.NavMeshAgent is null)
            {
                Debug.LogError("NavMeshAgent is null. Cannot execute Patrol Command.");
                return false;
            }

            return true;
        }

        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, () => { });
            if (!CheckBlackboard(blackboard))
            {
                OnExit(blackboard);
                processError?.Invoke();
                return;
            }
            Debug.Log("Entering PatrolCommand.");

            blackboard.PatrolInfo.StartPatrolTime = Time.time;
            blackboard.PatrolInfo.CurrentPatrolTime = blackboard.PatrolInfo.GetRandomPatrolTime();
            blackboard.PatrolInfo.CurrentWayPointIndex = blackboard.PatrolInfo.GetNextWayPointIndex();
            blackboard.NavMeshAgent.destination = blackboard.PatrolInfo.GetCurrentWayPoint();
            blackboard.NavMeshAgent.isStopped = false; // 이동을 시작
            Debug.Log("AI is now wandering to a new point.");

            // NavMesh Speed 설정
            blackboard.NavMeshAgent.speed = blackboard.TryGet(new BBKey<float>("walkSpeed"), out float speed) ? speed : 0;

            // Patrol 애니메이션 재생
            if (CheckAnimator(blackboard, "Run"))
            {
                // _animationRunning = true;
                // blackboard.Animator.SetTrigger("Run");
            }
        }

        public override void Execute(Blackboard.Blackboard blackboard, Action onComplete)
        {
            if (!CheckBlackboard(blackboard))
            {
                OnExit(blackboard);
                onComplete?.Invoke();
                return;
            }

            // 현재 Patrol 시간이 초과되었는지 확인
            if (Time.time - blackboard.PatrolInfo.StartPatrolTime >= blackboard.PatrolInfo.CurrentPatrolTime)
            {
                OnExit(blackboard);
                // 명령어 완료 콜백 호출
                onComplete?.Invoke();
                return;
            }

            // 현재 Patrol 지점으로 이동 중
            if (blackboard.NavMeshAgent.remainingDistance <= blackboard.NavMeshAgent.stoppingDistance)
            {
                // Debug.Log("Reached current wander point. Continuing to wander.");
                // blackboard.PatrolInfo.IsPatrolling = false; // 현재 Patrol를 종료하고 새로운 Patrol를 시작

                // 다음 Patrol 지점으로 이동
                blackboard.PatrolInfo.CurrentWayPointIndex = blackboard.PatrolInfo.GetNextWayPointIndex();
                blackboard.NavMeshAgent.destination = blackboard.PatrolInfo.GetCurrentWayPoint();
            }
        }

        public override void OnExit(Blackboard.Blackboard blackboard)
        {
            base.OnExit(blackboard);
            // Patrol 상태 종료 처리
            // blackboard.State = MonsterState.Idle;
            blackboard.NavMeshAgent.isStopped = true; // 이동을 멈춤
            blackboard.NavMeshAgent.ResetPath(); // 경로를 초기화

            // Patrol 애니메이션 종료
            if (CheckAnimator(blackboard, "Run"))
            {
                // _animationRunning = false;
                // blackboard.Animator.ResetTrigger("Run");
                // blackboard.Animator.SetTrigger("Idle");
            }
            Debug.Log("Exiting PatrolCommand.");
        }
    }
}