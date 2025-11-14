using _Test.Skills;
using Managers;
using System.Collections;
using UnityEngine;

namespace Monster.AI.FSM
{
    public class AmonPaseOneFSM : FSM
    {
        [SerializeField] private GameObject body;
        [SerializeField] private GameObject deathBody;
        [SerializeField] private float deathAnimationDurationChar = 1.5f;
        
        protected override void Think()
        {
            // Debug.Log(isEnabled);
            if (!isEnabled) return; // FSM이 활성화되지 않은 경우 아무 작업도 수행하지 않음(매니저에 의해 활성화 됨)

            if (blackboard?.Target is null)
            {
                Debug.Log("Target is null");
                return;
            }

            if (blackboard.State.GetStates() == "Death")
            {
                Debug.Log("State is Death");
                return;
            }
            
            if (blackboard.CurrentHealth <= 0)
            {
                ChangeState("Death");
                return;
            }
            
            if (blackboard.IsAnySkillRunning) 
            {
                // Debug.Log(blackboard.IsAnySkillRunning);
                return; // 스킬이 실행 중이면 상태 전환을 하지 않음
            }
            
            // 1페이즈 보스는 직접 플레이어를 추적하지 않는다.
            // 스킬 쿨타임 마다 텔레포트와 돌진 스킬을 사용해 플레이어 주변으로 이동한다.
            // 영혼 구체는 현재 체력의 80%, 50%, 20% 이하로 떨어질 때마다 한 번씩 사용
            if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.2f && !blackboard.HasUsedSoulOrbAt20Percent)
            {
                blackboard.HasUsedSoulOrbAt20Percent = true;
                ChangeState("UsingSkill4"); // 영혼 구체
                return;
            }
            
            if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.5f && !blackboard.HasUsedSoulOrbAt50Percent)
            {
                blackboard.HasUsedSoulOrbAt50Percent = true;
                ChangeState("UsingSkill4"); // 영혼 구체
                return;
            }
            
            if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.8f && !blackboard.HasUsedSoulOrbAt80Percent)
            {
                blackboard.HasUsedSoulOrbAt80Percent = true;
                ChangeState("UsingSkill4"); // 영혼 구체
                return;
            }
            
            // 플레이어와의 거리가 일정 거리 이상 멀어지면 텔레포트 사용
            float distanceToPlayer = Vector3.Distance(blackboard.transform.position, blackboard.Target.transform.position);
            if (distanceToPlayer > 15f && blackboard.Skills[3].CurrentState == Skill.SkillState.isReady)
            {
                ChangeState("UsingSkill3"); // 텔레포트
                return;
            }
            
            // 플레이어와 거리가 일정 거리 이내로 가까워지면 돌진 스킬 사용
            if (distanceToPlayer < 10f && blackboard.Skills[0].CurrentState == Skill.SkillState.isReady)
            {
                ChangeState("UsingSkill1"); // 돌진
                return;
            }
            
            // 그 외의 경우에는 난사 스킬 사용
            if (blackboard.Skills[1].CurrentState == Skill.SkillState.isReady)
            {
                ChangeState("UsingSkill2"); // 난사
                return;
            }
            
            ChangeState("Idle"); // 모든 스킬이 쿨타임 중일 때 대기 상태 유지
        }

        protected override void Act()
        {
            if (!isEnabled) return;
            if (blackboard?.Target is null) return;
            
            string state = blackboard.State.GetStates();
            if (state is null) return;
            
            if (blackboard.IsAnySkillRunning) 
            {
                Debug.Log(blackboard.IsAnySkillRunning);
                return; // 스킬이 실행 중이면 상태 전환을 하지 않음
            }
            
            switch (state)
            {
                case "Idle":
                    // 대기 상태에서 특별한 행동이 필요하지 않음
                    break;
                case "UsingSkill1":
                    // 돌진 스킬 
                    blackboard.Skills[0].Execute(blackboard);
                    break;
                case "UsingSkill2":
                    // 난사 스킬 사용 로직은 애니메이션 이벤트나 별도의 코루틴에서 처리
                    blackboard.Skills[1].Execute(blackboard);
                    break;
                case "UsingSkill3":
                    // 영혼 구체 스킬 사용 로직은 애니메이션 이벤트나 별도의 코루틴에서 처리
                    blackboard.Skills[2].Execute(blackboard);
                    break;
                case "UsingSkill4":
                    // 텔레포트
                    blackboard.Skills[3].Execute(blackboard);
                    break;
                case "Death":
                    ActDeath();
                    break;
                default:
                    // 알 수 없는 상태에 대한 처리 (예: 로그 출력)
                    Debug.LogWarning($"Unknown state: {state}");
                    break;
            }
        }

        private void ActDeath()
        {
            // 사망 처리 로직
            blackboard.NavMeshAgent.isStopped = true;
            
            isEnabled = false; // FSM 비활성화

            StartCoroutine(WaitForDeathAnimation());
        }

        private IEnumerator WaitForDeathAnimation()
        {
            yield return null;
            body.SetActive(false);
            deathBody.SetActive(true);
            yield return new WaitForSeconds(deathAnimationDurationChar);
            
            DungeonManager.Instance.AmonSecondPhase();
        }

        protected override void EnterState(string stateName)
        {
            // 상태에 진입할 때 필요한 초기화 작업 수행
            Debug.Log($"{blackboard.State.GetStates()}");
            
            // InitAnimationFlags();
            switch (stateName)
            {
                case "Idle":
                    // blackboard.AnimatorParameterSetter.SetBool("isIdle", true);
                    // blackboard.AgentRigidbody.velocity = Vector3.zero; // 물리 정지
                    // blackboard.NavMeshAgent.isStopped = true;
                    break;
                case "UsingSkill1":
                    // blackboard.AnimatorParameterSetter.SetBool("isUsingSkill1", true);
                    // blackboard.NavMeshAgent.isStopped = true;
                    // blackboard.AgentRigidbody.velocity = Vector3.zero;
                    break;
                default:
                    break;
            }
        }
    }
}