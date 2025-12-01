using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public class WanderCommand : AICommand
    {
        // private static bool _animationRunning;
        private static bool CheckBlackboard(Blackboard.Blackboard blackboard)
        {
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot execute Wander Command.");
                return false;
            }

            // Wander 정보가 유효한지 확인
            if (blackboard.WanderInfo is null)
            {
                Debug.LogWarning("WanderInfo is not valid for WanderCommand.");
                return false;
            }

            if (blackboard.WanderInfo.wanderAreaRadius <= 0f)
            {
                Debug.LogWarning("Wander area radius is not set or invalid.");
                return false;
            }

            // NavMeshAgent가 유효한지 확인
            if (blackboard.NavMeshAgent is null)
            {
                Debug.LogError("NavMeshAgent is null. Cannot execute Wander Command.");
                return false;
            }

            return true;
        }

        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, processError);
            if (!CheckBlackboard(blackboard))
            {
                OnExit(blackboard);
                processError?.Invoke();
                return;
            }
            Debug.Log("Entering WanderCommand.");
            blackboard.WanderInfo.StartWanderTime = Time.time;
            blackboard.WanderInfo.CurrentWanderTime = blackboard.WanderInfo.GetRandomWanderTime();
            blackboard.WanderInfo.CurrentWanderPoint = blackboard.WanderInfo.GetRandomWanderPoint();
            blackboard.NavMeshAgent.destination = blackboard.WanderInfo.CurrentWanderPoint;
            blackboard.NavMeshAgent.isStopped = false; // 이동을 시작
            Debug.Log("AI is now wandering to a new point.");

            // NavMesh Speed 설정
            blackboard.NavMeshAgent.speed = blackboard.TryGet(new BBKey<float>("walkSpeed"), out float speed) ? speed : 0;

            // Wander 애니메이션 재생
            if (CheckAnimator(blackboard, "Run"))
            {
                // _animationRunning = true;
                // blackboard.Animator.SetTrigger("Run");
            }
        }

        public override void Execute(Blackboard.Blackboard blackboard, Action onComplete)
        {
            if (CheckBlackboard(blackboard))
            {
                OnEnter(blackboard);
                // 명령어 완료 콜백 호출
                onComplete?.Invoke();
                return;
            }

            // Wander 진행

            // 현재 Wander 시간이 초과되었는지 확인
            if (Time.time - blackboard.WanderInfo.StartWanderTime >= blackboard.WanderInfo.CurrentWanderTime)
            {
                OnEnter(blackboard);
                // 명령어 완료 콜백 호출
                onComplete?.Invoke();
                return;
            }

            // 현재 Wander 지점으로 이동 중
            if (blackboard.NavMeshAgent.remainingDistance <= blackboard.NavMeshAgent.stoppingDistance)
            {
                // Debug.Log("Reached current wander point. Continuing to wander.");
                // blackboard.WanderInfo.IsWandering = false; // 현재 Wander를 종료하고 새로운 Wander를 시작
                blackboard.WanderInfo.CurrentWanderPoint = blackboard.WanderInfo.GetRandomWanderPoint();
                blackboard.NavMeshAgent.destination = blackboard.WanderInfo.CurrentWanderPoint;
                blackboard.NavMeshAgent.isStopped = false; // 이동을 계속
            }
        }

        public override void OnExit(Blackboard.Blackboard blackboard)
        {
            base.OnExit(blackboard);
            // Wander 상태 종료 처리
            // blackboard.State = MonsterState.Idle;
            // _animationRunning = false;
            blackboard.NavMeshAgent.isStopped = true; // 이동을 멈춤
            blackboard.NavMeshAgent.ResetPath(); // 경로를 초기화
        }
    }
}