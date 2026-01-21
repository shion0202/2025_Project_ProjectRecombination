using Monster;
using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public class IdleCommand : AICommand
    {
        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, processError);
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot enter IdleCommand.");
                processError?.Invoke();
                return;
            }

            Debug.Log("Entering IdleCommand.");
            // NavMeshAgent를 정지시키고, 이동을 중지
            if (blackboard.NavMeshAgent is not null)
            {
                blackboard.NavMeshAgent.isStopped = true;
                blackboard.NavMeshAgent.ResetPath();
                Debug.Log("AI has stopped moving.");
            }
            
            var animatorSetter = blackboard.AnimatorParameterSetter;
            if (animatorSetter is not null && animatorSetter.Animator is not null)
            // 대기 애니메이션 재생
            // if (blackboard.Animator is not null)
            {
                animatorSetter.Animator.SetBool("IsRun", false);
                animatorSetter.Animator.SetBool("IsWalk", false);
                animatorSetter.Animator.SetBool("IsFire", false);
            }
        }

        public override void Execute(Blackboard.Blackboard blackboard, Action onComplete)
        {
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot execute IdleCommand.");
            }

            OnExit(blackboard);
            // 명령어 완료 콜백 호출
            onComplete?.Invoke();
        }

        public override void OnExit(Blackboard.Blackboard blackboard)
        {
            base.OnExit(blackboard);
            Debug.Log("Exiting IdleCommand.");
        }
    }
}