using Managers;
using Monster;
using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public class DeathCommand : AICommand
    {
        private static readonly int Death = Animator.StringToHash("Death");
        private Collider _collider;
        private Rigidbody _rigidbody;
        private float _timer = 2.0f; // 애니메이션이 없을 때 대기 시간

        private bool CheckBlackboard(Blackboard.Blackboard blackboard)
        {
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot execute DeathCommand.");
                return false;
            }
            // NavMeshAgent가 유효한지 확인
            if (blackboard.NavMeshAgent is null)
            {
                Debug.LogError("NavMeshAgent is null. Cannot execute DeathCommand.");
                return false;
            }
            if (blackboard.Agent is null)
            {
                Debug.LogError("Agent is null. Cannot execute DeathCommand.");
                return false;
            }

            // _collider = blackboard.AgentCollider;
            // if (_collider is null)
            // {
            //     Debug.LogError("Collider is null. Cannot execute DeathCommand.");
            //     return false;
            // }
            //
            // _rigidbody = blackboard.AgentRigidbody;
            // if (_rigidbody is null)
            // {
            //     Debug.LogError("Rigidbody is null. Cannot execute DeathCommand.");
            //     return false;
            // }

            return true;
        }

        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, () => { });
            Debug.Log("DeathCommand OnEnter");
            if (!CheckBlackboard(blackboard))
            {
                OnExit(blackboard);
                processError?.Invoke();
                return;
            }
            // 사망 처리
            _collider.enabled = false;
            _rigidbody.isKinematic = true;
            blackboard.NavMeshAgent.isStopped = true;
            blackboard.NavMeshAgent.ResetPath();
            blackboard.DeathEffect.SetActive(true);

            // // 재생 중인 모든 애니메이션 초기화
            // blackboard.Animator.Rebind();
            // blackboard.Animator.Update(0f);
            //
            // // Death 애니메이션 재생
            // blackboard.Animator.SetTrigger("Death");
        }
        public override void Execute(Blackboard.Blackboard blackboard, Action onComplete)
        {
            if (!CheckBlackboard(blackboard)) return;

            // Animator가 유효한지 확인
            if (CheckAnimator(blackboard, "Death"))
            {
                // AnimatorStateInfo animaState = blackboard.Animator.GetCurrentAnimatorStateInfo(0);

                // if (animaState.normalizedTime < 1.0f)
                // {
                //     OnExit(blackboard);
                //     onComplete?.Invoke();
                //     return;
                // }
            }
            else
            {
                // TODO: 재생할 애니메이션이 없을 경우 2초 후에 처리
                if (_timer > 0f) _timer -= Time.deltaTime;
                else
                {
                    OnExit(blackboard);
                    // 명령어 완료 콜백 호출
                    onComplete?.Invoke();
                }
            }
        }

        public override void OnExit(Blackboard.Blackboard blackboard)
        {
            base.OnExit(blackboard);
            // Death 상태 종료 처리
            // blackboard.State = MonsterState.Dead;
            // blackboard.NavMeshAgent.isStopped = true; // 이동을 멈춤
            // blackboard.NavMeshAgent.ResetPath(); // 경로를 초기화

            // 사망 이펙트 비활성화
            blackboard.DeathEffect.SetActive(false);

            // 몬스터 오브젝트 풀로 반환
            // PoolManager.Instance.Push(blackboard.MonsterType.ToString(), blackboard.Agent);
        }
    }
}