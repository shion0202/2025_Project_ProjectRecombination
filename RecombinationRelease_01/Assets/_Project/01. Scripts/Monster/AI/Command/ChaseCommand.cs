using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public class ChaseCommand : AICommand
    {
        private static readonly int IsRun = Animator.StringToHash("IsRun");
        
        private static bool IsValid(Blackboard.Blackboard blackboard)
        {
            if (blackboard?.NavMeshAgent is null)
            {
                Debug.LogError("NavMeshAgent is null. Cannot execute ChaseCommand.");
                return false;
            }
            
            // 타겟의 위치가 유효한지 확인
            if (blackboard.Target is null || !blackboard.Target.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("Target is not valid for ChaseCommand.");
                return false;
            }
            return true;
        }
        
        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, () => { });
            // Debug.Log("ChaseCommand OnEnter");
            if (!IsValid(blackboard))
            {
                OnExit(blackboard);
                processError?.Invoke();
                return;
            }
            float speed = blackboard.TryGet(new BBKey<float>("runSpeed"), out float s) ? s : 0;   // NavMesh Speed 설정
            blackboard.NavMeshAgent.speed = speed;

            SetDestination(blackboard); // 이동을 시작
            
            // 이동 애니메이션 재생
            // Debug.Log("Run Animation Play");
            // if (!blackboard.Animator.GetBool(IsRun))
            //     blackboard.Animator.SetBool(IsRun, true);
        }
        
        // Chase 상태 처리
        public override void Execute(Blackboard.Blackboard blackboard, Action onComplete)
        {
            Debug.Log("ChaseCommand Execute");
            if (!IsValid(blackboard)) return;
            
            // 이미 Chase 중인 경우, 타겟과 충분히 가까운지 확인
            float distance = Vector3.Distance(blackboard.Agent.transform.position, blackboard.Target.transform.position);
            float stopDistance = blackboard.TryGet(new BBKey<float>("minDetectionRange"), out float value) ? value : 0f;
            blackboard.NavMeshAgent.stoppingDistance = stopDistance;
            if (distance > stopDistance)
            {
                // 타겟과 충분히 멀리 있는 경우 계속 추적
                SetDestination(blackboard);
            }
            else
            {
                Debug.Log("ChaseCommand OnExit");
                OnExit(blackboard);
                onComplete?.Invoke();                          // 명령어 완료 콜백 호출
            }
        }

        // Chase 상태 종료 처리
        public override void OnExit(Blackboard.Blackboard blackboard)
        {
            base.OnExit(blackboard);
            
            // if (blackboard.Animator.GetBool(IsRun))
            //     blackboard.Animator.SetBool(IsRun, false);
            
            blackboard.NavMeshAgent.isStopped = true;       // 이동을 멈춤
            blackboard.NavMeshAgent.ResetPath();            // 경로를 초기화
            
            Debug.Log("ChaseCommand OnExit");
        }
        
        private static void SetDestination(Blackboard.Blackboard blackboard)
        {
            if (blackboard.Target is null) return;
            Vector3 targetPosition = blackboard.Target.transform.position;
            blackboard.NavMeshAgent.SetDestination(targetPosition);
            
            blackboard.NavMeshAgent.isStopped = false;    
        }
        
        private static bool HasReachedDestination(Blackboard.Blackboard blackboard)
        {
            if (blackboard.NavMeshAgent.pathPending) return false;
            if (!(blackboard.NavMeshAgent.remainingDistance <= blackboard.NavMeshAgent.stoppingDistance)) return false;
            return !blackboard.NavMeshAgent.hasPath || blackboard.NavMeshAgent.velocity.sqrMagnitude == 0f;
        }
    }
}