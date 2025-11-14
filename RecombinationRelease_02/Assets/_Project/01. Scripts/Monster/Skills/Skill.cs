using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    [Serializable]
    public class Skill
    {
        public enum SkillState
        {
            isReady,
            isCasting,
            isRunning,
            isCooltime,
            isEnded
        }
        
        public SkillState CurrentState { get; private set; }
        public SkillData skillData;
        private MonoBehaviour _owner;

        public event Action OnActivate;
        public event Action OnCast;
        public event Action OnDeactivate;
        public event Action OnReady;

        public Skill(SkillData skillData, MonoBehaviour owner)
        {
            this.skillData = skillData;
            _owner = owner;
            CurrentState = SkillState.isReady;
            // useSkill = ; // Blackboard 데이터를 넣어주려면 어떻게 해야 할까
        }

        public void Execute(Blackboard blackboard)
        {
            if (CurrentState == SkillState.isReady)
                _owner.StartCoroutine(C_Execute(blackboard));
        }

        private IEnumerator C_Execute(Blackboard blackboard)
        {
            CurrentState = SkillState.isCasting;
            OnCast?.Invoke();
            yield return skillData.Casting(blackboard);
            
            try
            {
                CurrentState = SkillState.isRunning;
                OnActivate?.Invoke(); // 실제 스킬 로직 실행
                yield return skillData.Activate(blackboard);
            }
            finally
            {
                CurrentState = SkillState.isCooltime;
                OnDeactivate?.Invoke();
                _owner.StartCoroutine(ApplyCooldown());
            }
        }
        
        private IEnumerator ApplyCooldown()
        {
            Debug.Log($"스킬 {skillData.skillName} 쿨타임 시작: {skillData.cooldown}초");
            yield return new WaitForSeconds(skillData.cooldown);
            CurrentState = SkillState.isReady;
            OnReady?.Invoke();
        }
    }
}