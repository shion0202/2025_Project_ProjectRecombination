using Managers;
using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    [Serializable]
    public abstract class SkillData : ScriptableObject
    {
        [Header("스킬 기본 정보")] 
        public int skillID;
        public string skillName;
        [TextArea] public string skillDescription;
        
        [Header("스킬 효과")]
        public float damage;
        public float range;
        public float cooldown;
        public float castTime;
        public float animSpeed;

        [Header("시전 안내 메시지")]
        [Tooltip("파훼가 필요한 패턴에서 시전 시작 시 플레이어에게 띄울 안내. 비워두면 아무것도 띄우지 않는다.")]
        [TextArea] public string castNotice;
        [TextArea] public string enCastNotice;

        [Tooltip("메시지를 보여주는 시간(초). 앞뒤로 페이드 인/아웃이 각각 1초씩 더 붙는다.")]
        public float castNoticeDuration = 5.0f;

        [Header("패턴 설명")]
        [Tooltip("이 패턴이 처음 시전될 때 게임을 멈추고 띄울 도움말(TutorialDataSO)의 key. " +
                 "비워두면 아무것도 띄우지 않는다. 플레이당 한 번만 뜬다.")]
        public string patternGuideKey;

        /// <summary>
        /// 시전 안내 메시지를 띄운다. 문구가 비어 있으면 아무 일도 하지 않는다.
        /// 튜토리얼의 SetNoticeMessage 노드와 같은 UI 경로를 쓴다.
        /// </summary>
        protected void ShowCastNotice()
        {
            string message = LocalizationManager.IsKorean ? castNotice : enCastNotice;
            if (string.IsNullOrWhiteSpace(message)) return;

            float duration = castNoticeDuration > 0.0f ? castNoticeDuration : 5.0f;
            GUIManager.Instance.GameUIController.ActivateMessage(message, duration);
        }

        public virtual IEnumerator Casting(Blackboard data)
        {
            Debug.Log($"skill {skillName} 캐스팅 시작: {castTime}초");
            yield return new WaitForSeconds(castTime);
            Debug.Log($"skill {skillName} 캐스팅 완료");
        }
        
        public abstract IEnumerator Activate(Blackboard data);

        /// <summary>
        /// 트리거로 공격 애니메이션을 재생하고, 그 상태를 빠져나가기 시작할 때까지 기다린다.
        /// 트리거만 걸고 바로 끝내면 애니메이션이 도는 중에 FSM이 다음 행동(추격 등)을 시작해
        /// 공격 자세로 미끄러지는 문제가 생기므로, 공격 스킬의 Activate는 이걸로 끝을 맞춘다.
        /// timeout은 트리거가 소비되지 않거나 상태가 끝나지 않을 때 스킬이 영영 안 끝나는 것을 막는 안전장치.
        /// </summary>
        protected static IEnumerator PlayAnimationAndWait(Animator animator, string trigger, float timeout = 5.0f)
        {
            animator.SetTrigger(trigger);
            float elapsed = 0.0f;

            // 1. 트리거가 소비되어 공격 상태로 전환이 시작될 때까지
            while (animator.GetBool(trigger))
            {
                elapsed += Time.deltaTime;
                if (elapsed > timeout)
                {
                    animator.ResetTrigger(trigger);
                    yield break;
                }
                yield return null;
            }

            int attackStateHash = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0).fullPathHash
                : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

            // 2. 공격 상태에서 다른 상태로 넘어가기 시작할 때까지
            while (elapsed < timeout)
            {
                int stateHash = animator.IsInTransition(0)
                    ? animator.GetNextAnimatorStateInfo(0).fullPathHash
                    : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
                if (stateHash != attackStateHash) yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 시전 중(Casting/Activate) 스킬이 외부에서 강제 중단될 때 호출된다.
        /// (예: 몬스터 피격으로 MonsterFSM이 StopCoroutine 하는 경우)
        /// StopCoroutine은 코루틴의 finally를 실행하지 않으므로, 생성한 이펙트/오브젝트 등
        /// 잔여 상태가 있는 스킬은 이 메서드를 override 하여 직접 정리해야 한다. 기본은 no-op.
        /// </summary>
        public virtual void OnInterrupt(Blackboard data) { }
        
        private bool _isChasting = false;
        private float _chastingTime;
        private float _chastingStartTime;
        
        [ContextMenu("Set Parameters")]
        private void SetParameters()
        {
            RowData skillData = DataManager.Instance.SheetData.GetRow("MonsterSkill", skillID);
            skillName = skillData.GetStringStat(EStatType.Name);
            range = skillData.GetStat(EStatType.Range);
            damage = skillData.GetStat(EStatType.Damage);
            cooldown = skillData.GetStat(EStatType.CooldownTime);
            castTime = skillData.GetStat(EStatType.CastTime);
            animSpeed = skillData.GetStat(EStatType.AnimSpeed);
            skillDescription = skillData.GetStringStat(EStatType.Description);
        }
    }
}