using _Test.Skills;
using Managers;
using Monster.AI.FSM;
using System.Collections;
using UnityEngine;

public class AmonPaseTwoFSM : FSM
{
    [SerializeField] private GameObject spawnModel;
    [SerializeField] private GameObject deathModel;
    [SerializeField] private GameObject amonBody;
    
    // 연출대기 시간
    [Header("연출 대기 시간")]
    [SerializeField] private float spawnWaitTime = 5f;
    [SerializeField] private float deathWaitTime = 5f;

    private bool _isSpawned;
    
    protected override void Think()
    {
        if (!isEnabled) return; // FSM이 활성화되지 않은 경우 아무 작업도 수행하지 않음(매니저에 의해 활성화 됨)

        if (!_isSpawned) return;
        
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
            Debug.Log(blackboard.IsAnySkillRunning);
            return; // 스킬이 실행 중이면 상태 전환을 하지 않음
        }
        
        // TODO: 영혼 흡수 기믹 추가
        {
            if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.5f && !blackboard.HasUsedSoulAbsorptionAt50Percent)
            {
                blackboard.HasUsedSoulAbsorptionAt50Percent = true;
                ChangeState("UsingSkill5"); // 영혼 흡수
                return;
            }
            if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.2f && !blackboard.HasUsedSoulAbsorptionAt20Percent)
            {
                blackboard.HasUsedSoulAbsorptionAt20Percent = true;
                ChangeState("UsingSkill5"); // 영혼 흡수
                return;
            }
        }
        
        // 체력이 50% 이하로 떨어지면 락다운 스킬을 쿨타임 마다 사용
        if (blackboard.CurrentHealth <= blackboard.MaxHealth * 0.5f && blackboard.Skills[5].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill6"); // 락다운
            return;
        }
        
        // 2페이즈 보스는 플레이어를 직접 추적하며 근접 공격 스킬과 원거리 공격 스킬을 번갈아 사용
        float distanceToTarget = Vector3.Distance(blackboard.transform.position, blackboard.Target.transform.position);

        if (distanceToTarget > blackboard.Skills[6].skillData.range && blackboard.Skills[6].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill7"); // 텔레포트
        }
        else if (distanceToTarget <= blackboard.Skills[0].skillData.range && blackboard.Skills[0].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill1"); // 질주 공격
        }
        else if (distanceToTarget <= blackboard.Skills[1].skillData.range && blackboard.Skills[1].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill2"); // 날개 공격
        }
        else if (distanceToTarget <= blackboard.Skills[3].skillData.range && blackboard.Skills[3].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill4"); // 소울 오브
        }
        else if (blackboard.Skills[2].CurrentState == Skill.SkillState.isReady)
        {
            ChangeState("UsingSkill3");
        }
        else if (distanceToTarget > blackboard.MinDetectionRange)
        {
            ChangeState("Chase");
        }
        else
        {
            ChangeState("Idle");
        }
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
            case "Chase":
                ActChase();
                break;
            case "UsingSkill1":
                // 질주 공격
                blackboard.Skills[0].Execute(blackboard);
                break;
            case "UsingSkill2":
                // 날개 공격
                blackboard.Skills[1].Execute(blackboard);
                break;
            case "UsingSkill3":
                // 소울 스피어
                blackboard.Skills[2].Execute(blackboard);
                break;
            case "UsingSkill4":
                // 소울 오브
                blackboard.Skills[3].Execute(blackboard);
                break;
            case "UsingSkill7":
                // 텔레포트
                blackboard.Skills[6].Execute(blackboard);
                break;
            case "Spawn":
                ActSpawn();
                break;
            case "Death":
                // 사망 처리 로직
                ActDeath();
                break;
        }
    }

    private void ActSpawn()
    {
        Debug.Log("Amon Pase Two has spawned.");
        
        spawnModel.SetActive(true);
        amonBody.SetActive(false);
        StartCoroutine(WaitAmonSpawnAnimation());
    }

    private IEnumerator WaitAmonSpawnAnimation()
    {
        yield return new WaitForSeconds(spawnWaitTime);
        
        spawnModel.SetActive(false);
        amonBody.SetActive(true);
        _isSpawned = true;
        ChangeState("Idle");
    }

    private void ActDeath()
    {
        // 사망 처리 로직 구현
        Debug.Log("Amon Pase Two has died.");
        // 예: 애니메이션 재생, 콜라이더 비활성화, 아이템 드랍 등
        amonBody.SetActive(false);
        deathModel.SetActive(true);
        StartCoroutine(WaitAmonDeathAnimation());
    }
    
    private IEnumerator WaitAmonDeathAnimation()
    {
        // 사망 애니메이션이 재생되는 동안 대기
        yield return new WaitForSeconds(deathWaitTime); // 예: 3초 대기
        
        DungeonManager.Instance.AmonEndPhase();
        Destroy(this);
    }

    private void ActChase()
    {
        if (blackboard.Target is null) return;

        Vector3 direction = (blackboard.Target.transform.position - blackboard.transform.position).normalized;
        Vector3 chasePosition = blackboard.transform.position + direction * (blackboard.RunSpeed * Time.deltaTime);

        // NavMeshAgent를 사용하여 이동
        if (blackboard.NavMeshAgent != null)
        {
            blackboard.NavMeshAgent.isStopped = false;
            blackboard.NavMeshAgent.SetDestination(chasePosition);
        }

        // 애니메이션 설정
        blackboard.AnimatorParameterSetter.Animator.SetBool("isMoving", true);
    }

    protected override void EnterState(string stateName)
    {
        switch (stateName)
        {
            default:
                // 아무 작업도 수행하지 않음
                break;
        }
    }

    protected override void Init()
    {
        base.Init();
        
        ChangeState("Spawn");

        _isSpawned = false;
    }
}
