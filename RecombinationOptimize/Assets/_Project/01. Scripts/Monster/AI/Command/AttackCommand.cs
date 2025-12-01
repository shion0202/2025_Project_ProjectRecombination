using System;
using System.Collections;
using UnityEngine;

namespace Monster.AI.Command
{
    public class AttackCommand : AICommand
    {
        #region Skill Base Data
        private Blackboard.Blackboard _blackboard;
        private readonly RowData _skillRowData;
        private int _skillID;
        private float _coolTime;
        #endregion

        #region 생성자

        public AttackCommand(Blackboard.Blackboard blackboard, int skillID)
        {
            _blackboard = blackboard;
            // _skillRowData = skillRowData;
            _skillID = (int)_skillRowData.Stats[EStatType.ID].value;
            _coolTime = _skillRowData.Stats[EStatType.CooldownTime].value;
        }

        #endregion

        #region Validation

        private static bool IsValid(Blackboard.Blackboard blackboard)
        {
            if (blackboard is null)
            {
                Debug.LogError("Blackboard is null. Cannot execute AttackCommand.");
                return false;
            }
            // NavMeshAgent가 유효한지 확인
            if (blackboard.NavMeshAgent is null)
            {
                Debug.LogError("NavMeshAgent is null. Cannot execute AttackCommand.");
                return false;
            }
            if (blackboard.Agent is null)
            {
                Debug.LogError("Agent is null. Cannot execute AttackCommand.");
                return false;
            }
            if (blackboard.Target is null)
            {
                Debug.LogError("Target is null. Cannot execute AttackCommand.");
                return false;
            }
            
            return true;
        }

        #endregion

        public override void OnEnter(Blackboard.Blackboard blackboard, Action processError = null)
        {
            base.OnEnter(blackboard, () => { });
            if (!IsValid(blackboard) || _skillRowData == null)
            {
                OnExit(blackboard);
                processError?.Invoke();
                return;
            }
            
            Debug.Log("AttackCommand OnEnter");
            blackboard.Agent.transform.LookAt(blackboard.Target.transform);
            
            // Agent 움직임 정지
            blackboard.NavMeshAgent.isStopped = true;
            blackboard.NavMeshAgent.ResetPath();
            
            // // 공격 애니메이션 재생
            
            // 각 스킬별 시전 초기 행동 설정
            switch (_skillID)
            {
                case 4001:
                    {
                        // 단발 총알 발사 준비
                        // blackboard.Animator.SetBool("IsFire", true);
                        break;
                    }
                case 4002:
                    {
                        // 연발 총알 발사 준비
                        // blackboard.Animator.SetBool("IsFire", true);
                        break;
                    }
                case 4003:
                    {
                        // 강력한 단발 총알 발사 준비
                        // blackboard.Animator.SetBool("IsFire", true);
                        break;
                    }
            }
            
        }
        
        public override void Execute(Blackboard.Blackboard blackboard, Action onComplete)
        {
            if (!IsValid(blackboard) || _skillRowData == null)
            {
                OnExit(blackboard);
                onComplete?.Invoke();
                return;
            }
            
            OnExit(blackboard);
            
            // 명령어 완료 콜백 호출
            onComplete?.Invoke();
        }
        
        public override void OnExit(Blackboard.Blackboard blackboard)
        {
            base.OnExit(blackboard);
            Debug.Log("AttackCommand OnExit");
            // blackboard.State = MonsterState.Idle;
            // blackboard.Action.RemoveAction(EAction.Attacking);
            // Agent 움직임 재개
            if (blackboard.NavMeshAgent is not null)
            {
                blackboard.NavMeshAgent.isStopped = false;
            }
            // 스킬 쿨타임 적용
            // if (_coolTime > 0f) blackboard.ApplyCooldown();
            
            // 공격 애니메이션 정지
            // blackboard.Animator.SetBool("IsFire", false);
        }
        
        // private void FireBullet(Blackboard.Blackboard blackboard, int bulletType = 0)
        // {
        //     // 총알 발사 로직 구현
        //     Vector3 startPos = blackboard.AttackInfo.firePoint.position;
        //     Vector3 targetPos = blackboard.Target.transform.position + Vector3.up * 1.5f; // 타겟의 중심을 향하도록 약간 위로 조정
        //     Vector3 direction = (targetPos - startPos).normalized;
        //     
        //     // blackboard.Animator.SetBool("IsFire", true);
        //     blackboard.AttackInfo.Fire(bulletType, blackboard.Agent, blackboard.AttackInfo.firePoint.position, Vector3.zero, direction, _skillRowData.GetStat(EStatType.Damage));
        //     // blackboard.Animator.SetBool("IsFire", false);
        // }

        // public void AnimationEvent_Fire()
        // {
        //     switch (_skillID)
        //     {
        //         case 4001:
        //             FireBullet(_blackboard);
        //             break;
        //         case 4002:
        //             FireBullet(_blackboard);
        //             break;
        //         case 4003:
        //             FireBullet(_blackboard, 1);
        //             break;
        //     }
        //     
        // }
    }
}