using _Test.Skills;
using Monster.AI.Blackboard;
using System.Collections.Generic;
using UnityEngine;

public class AmonP2Skill : MonoBehaviour
{
    [SerializeField] private Blackboard blackboard;

    private void Start()
    {
        blackboard.Init();
    }
    
    private void Update()
    {
        // blackboard.UpdateCooldownList();
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 질주
            blackboard.State.SetState("UsingSkill1");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // 날게 휘두르기
            blackboard.State.SetState("UsingSkill2");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // 영혼 구체
            blackboard.State.SetState("UsingSkill3");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // 영혼 보주
            blackboard.State.SetState("UsingSkill4");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // 영혼 흡수
            blackboard.State.SetState("UsingSkill5");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            // 영혼 감금
            blackboard.State.SetState("UsingSkill6");
        }
        else if (Input.GetKey(KeyCode.A))
        {
            blackboard.State.SetState("Chase");
        }
        else
        {
            blackboard.State.SetState("Idle");
        }
    }

    private void LateUpdate()
    {
        string state = blackboard.State.GetStates();
        if (state is null) return;
        // if (_coroutine is not null) return;
        // 최상단에 단 한 번의 O(1) 체크로 안전장치 구현
        if (blackboard.IsAnySkillRunning) return;
    
        // Debug.Log(state);
        
        if (state.Contains("UsingSkill1"))
        {
            if (blackboard.Skills[0].CurrentState is not Skill.SkillState.isReady) return;
            Debug.Log("돌진 스킬 발동");
            
            blackboard.Skills[0].Execute(blackboard);
        }
        else if (state.Contains("UsingSkill2"))
        {
            if (blackboard.Skills[1].CurrentState is not Skill.SkillState.isReady) return;
            
            blackboard.Skills[1].Execute(blackboard);
        }
        else if (state.Contains("UsingSkill3"))
        {
            if (blackboard.Skills[2].CurrentState is not Skill.SkillState.isReady) return;
            
            blackboard.Skills[2].Execute(blackboard);
        }
        else if (state.Contains("UsingSkill4"))
        {
            if (blackboard.Skills[3].CurrentState is not Skill.SkillState.isReady) return;
            
            blackboard.Skills[3].Execute(blackboard);
        }
        else if (state.Contains("UsingSkill5"))
        {
            if (blackboard.Skills[4].CurrentState is not Skill.SkillState.isReady) return;
            blackboard.Skills[4].Execute(blackboard);
        }
        else if (state.Contains("UsingSkill6"))
        {
            if (blackboard.Skills[5].CurrentState is not Skill.SkillState.isReady) return;
            blackboard.Skills[5].Execute(blackboard);
        }
        else if (state.Contains("Chase"))
        {
            ActChase();
        }
        else if (state.Contains("Idle"))
        {
            List<string> array = blackboard.AnimatorParameterSetter.BoolParameterNames;
            Animator animator = blackboard.AnimatorParameterSetter.Animator;
            foreach (string param in array)
            {
                animator.SetBool(param, false);
            }
            
            // blackboard.AgentRigidbody.velocity = Vector3.zero;
        }
    }
    
    public void AnimationEvent_WingSwipe()
    {
        Debug.Log("날개 휘두르기 공격!");
    }

    private void ActChase()
    {
        blackboard.NavMeshAgent.SetDestination(blackboard.Target.transform.position);
        
        float distance = Vector3.Distance(blackboard.Target.transform.position, blackboard.NavMeshAgent.transform.position);
        if (distance <= blackboard.MinDetectionRange)
        {
            blackboard.NavMeshAgent.isStopped = true;
            blackboard.AnimatorParameterSetter.Animator.SetBool("isMoving", false);
        }
        else
        {
            blackboard.NavMeshAgent.isStopped = false;
            blackboard.AnimatorParameterSetter.Animator.SetBool("isMoving", true);
        }
    }
}
