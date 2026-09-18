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

        public Coroutine CUseSkill;

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
                CUseSkill = _owner.StartCoroutine(C_Execute(blackboard));
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
        
        /// <summary>
        /// 시전/실행 중인 스킬을 강제로 중단하고 쿨타임으로 넘긴다.
        /// 코루틴은 시작한 MonoBehaviour(_owner)에서 멈춰야 하고, StopCoroutine은 finally를 실행하지 않으므로
        /// C_Execute의 finally가 하던 정리(OnDeactivate, 쿨타임)를 여기서 대신한다.
        /// </summary>
        public void Interrupt(Blackboard blackboard)
        {
            if (CurrentState is not (SkillState.isCasting or SkillState.isRunning)) return;

            skillData.OnInterrupt(blackboard);
            if (CUseSkill != null) _owner.StopCoroutine(CUseSkill);
            CUseSkill = null;

            OnDeactivate?.Invoke();

            // 보스가 비활성화되는 중이면 코루틴을 시작할 수 없으므로 바로 사용 가능 상태로 둔다.
            if (_owner.isActiveAndEnabled)
            {
                CurrentState = SkillState.isCooltime;
                _owner.StartCoroutine(ApplyCooldown());
            }
            else
            {
                CurrentState = SkillState.isReady;
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