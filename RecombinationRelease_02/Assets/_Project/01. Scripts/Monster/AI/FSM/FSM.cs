using System.Collections.Generic;
using UnityEngine;

namespace Monster.AI.FSM
{
    public abstract class FSM : MonoBehaviour, IDamagable
    {
        public Blackboard.Blackboard blackboard;
        public bool isInit;
        public bool isEnabled;
        public RigMonster ikAnimaton;
        
        public void OnHit(float damage)
        {
            if (blackboard is null) return;
            blackboard.CurrentHealth -= damage;
            // 피격 시 추적 상태로 즉시 전환하는 로직 추가 가능
            // if (blackboard.CurrentHealth > 0) ChangeState("Chase");
        }
        
        public void ApplyDamage(float inDamage, LayerMask targetMask = default, float unitOfTime = 1.0f, float defenceIgnoreRate = 0.0f)
        {
            // To-do: Target Mask 로직 필요
            OnHit(inDamage);
            // ikAnimaton.TakeHit(Vector3.back); // TODO: 피격 IK RnD
        }
        
        // 상태 전환 메서드 (기존 코드와 거의 동일)
        protected void ChangeState(string stateName)
        {
            if (blackboard.State.GetStates() == stateName) return; // 같은 상태로의 변경 방지

            int mask = DynamicState.GetStateMask(stateName);
            if (mask == 0) return;
            
            blackboard.State.SetState(mask);
            EnterState(stateName);
        }
        
        protected void InitAnimationFlags()
        {
            List<string> array = blackboard.AnimatorParameterSetter.BoolParameterNames;
            Animator animator = blackboard.AnimatorParameterSetter.Animator;
            foreach (string param in array)
            {
                animator.SetBool(param, false);
            }
        }
        
        #region Unity Methods

        private void Start()
        {
            Init();
        }
        
        private void OnEnable()
        {
            if (!isInit)
                Init();
        }

        // 1. 판단(Think)은 Update에서 처리합니다.
        private void Update()
        {
            if (!isInit) Init();
            if (blackboard is null) return;

            // blackboard.UpdateCooldownList();
            Think(); // 매 프레임 '무슨 상태가 될지'를 결정합니다.
        }

        // 2. 행동(Act)은 Update 이후에 처리합니다. (LateUpdate도 좋고, Update 마지막에 호출해도 좋습니다)
        // 여기서는 명확한 분리를 위해 LateUpdate를 사용하겠습니다.
        private void LateUpdate()
        {
            if (!isInit || blackboard is null) return;
            // Debug.Log("Current State: " + blackboard.State.GetStates());
            Act(); // 결정된 현재 상태에 따라 '무엇을 할지'를 실행합니다.
        }
        
        private void OnDrawGizmos()
        {
            if (blackboard is null) return;
            // 몬스터의 위치를 기준으로 인식 범위를 원으로 표시
            if (blackboard.Agent is not null && blackboard.MaxDetectionRange > 0f)
            {
                // Gizmos의 색상을 설정
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(blackboard.Agent.transform.position, blackboard.MaxDetectionRange);
            }

            if (blackboard.Agent is not null &&
                blackboard.MinDetectionRange > 0f)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(blackboard.Agent.transform.position, blackboard.MinDetectionRange);
            }
            
            // 몬스터의 스킬 사거리를 시각적으로 표시
            if (blackboard.Skills is not null && blackboard.Skills.Length > 0)
            {
                foreach (var skill in blackboard.Skills)
                {
                    var skillRange = skill.skillData.range;
                    if (skillRange <= 0f) continue;

                    Gizmos.color = Color.blue;
                    if (blackboard.Agent is not null)
                        Gizmos.DrawWireSphere(blackboard.Agent.transform.position, skillRange);
                }
            }
        
            // Patrol Waypoints를 시각적으로 표시
            if (blackboard.PatrolInfo.wayPoints is { Length: > 0 })
            {
                Gizmos.color = Color.yellow;
                foreach (var t in blackboard.PatrolInfo.wayPoints)
                {
                    Gizmos.DrawSphere(t, 0.1f);
                }
            }
            
            // 몬스터의 배회 범위를 시각적으로 표시
            if (blackboard.WanderInfo.wanderAreaRadius > 0f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(blackboard.WanderInfo.wanderAreaCenter, blackboard.WanderInfo.wanderAreaRadius);
            }
            
            // 추가 정보는 아래에 추가
        }

        #endregion

        protected virtual void Init()
        {
            blackboard.Init();
            isInit = true;
        }

        protected abstract void Think();

        protected abstract void Act();
        
        protected abstract void EnterState(string stateName);
    }
}