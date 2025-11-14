using _Test.Skills;
using Monster.AI.Blackboard;
using UnityEngine;

public class AmonP1Skill : MonoBehaviour
{
    [SerializeField] private Blackboard blackboard;

    private void Start()
    {
        blackboard.Init();
    }
    
    private void Update()
    {
        // blackboard.UpdateCooldownList();
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 돌진
            // StartCoroutine(Rush());
            // Debug.Log("돌진 스킬 사용");
            blackboard.State.SetState("UsingSkill1");
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            // 난사
            // StartCoroutine(RapidFire());
            blackboard.State.SetState("UsingSkill2");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // 영혼 구체
            blackboard.State.SetState("UsingSkill3");
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            // 페이즈 전환
            blackboard.State.SetState("UsingSkill4");
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
            if (blackboard.Skills[0].CurrentState is not Skill.SkillState.isReady)
            {
                // Debug.Log($"Rush State: {blackboard.Skills[0].skillState}");
                // Debug.Log("남은 쿨타임: " + (blackboard.GetSkillCooldown(blackboard.Skills[0]) - Time.time) + "초");
                return;
            }
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
        else if (state.Contains("Idle"))
        {
            
        }
    }
}
