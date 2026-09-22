using Monster.AI.Blackboard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace _Test.Skills
{
    [CreateAssetMenu(fileName = "SoulAbsorb", menuName = "MonsterSkills/Amon_Phase2/SoulAbsorb")]
    public class AmonSoulAbsorb : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject barrierPrefab;
        [SerializeField] private AnimationClip raiseClip;
        [SerializeField] private Vector3 barrierOffset;
        [SerializeField] private float originalCooldown;                    // 쿨타임 절반 증가를 위한 본래 쿨타임 값
                                                                            // 원래 쿨타임이 변경될 때마다 직접 수정해야하므로 개선 필요

        [Tooltip("플레이어가 보호막을 파괴해 패턴을 파훼했을 때 보스가 아무 행동도 하지 않는 시간(초).")]
        [SerializeField] private float groggyDuration = 3.0f;
        private GameObject _barrierInstance;

        public bool IsShieldRemovedByPlayer { get; set; }

        // 시전 중 보스 사망/비활성화로 중단되면 보스에 붙은 보호막이 사망 연출 위에 남으므로 치운다.
        // (흡수 판정은 Activate에서만 일어나므로 중단 시 플레이어 체력은 건드리지 않는다)
        public override void OnInterrupt(Blackboard data)
        {
            data.AnimatorParameterSetter?.Animator?.SetBool("isBarrier", false);
            Utils.Destroy(_barrierInstance);
            _barrierInstance = null;
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 흡수 준비");

            // 파훼 방법을 모르면 대처할 수 없는 패턴이므로 시전 시작과 함께 안내한다.
            ShowCastNotice();

            // 1. 캐스팅 시작
            IsShieldRemovedByPlayer = false;
            data.AnimatorParameterSetter.Animator.SetBool("isBarrier", true);
            cooldown = originalCooldown;                                        // 쿨타임 초기화

            yield return new WaitForSeconds(raiseClip.length);

            _barrierInstance = Utils.Instantiate(barrierPrefab, data.Agent.transform);
            _barrierInstance.transform.localPosition = barrierOffset;
            _barrierInstance.transform.localRotation = Quaternion.identity;
            
            // 보호막 초기화
            var _barrier = _barrierInstance.GetComponent<AmonBarrier>();
            _barrier.Initialize(data.MaxHealth, () => { IsShieldRemovedByPlayer = true; });
            
            // To-do: 실제 보스에 무적 상태를 적용시키고(현재 적용 X), 실제 데미지는 보호막을 통해 계산
            // 보호막에 충돌 판정이 있긴 하나 관통이나 범위 공격을 피할 수가 없어 무적 상태(보호막으로부터의 데미지는 받을 수 있어야함) 고려가 필요한 상황
            // 보호막 체력이 다 될 경우 보스 쪽 변수를 변경하여 보호막 파괴 여부를 확인하고 그에 따른 기믹 처리

            float elapsed = 0f;
            while (elapsed < castTime)
            {
                // Debug.Log("Hp: " + data.CurrentHealth + "/" + data.MaxHealth);
                Vector3 direction = data.Target.transform.position - data.Agent.transform.position;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    data.Agent.transform.rotation = Quaternion.Slerp(data.Agent.transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                // 보호막이 파괴되었는지 체크
                if (IsShieldRemovedByPlayer)
                {
                    // Utils.Destroy(_barrierInstance);
                    data.AnimatorParameterSetter.Animator.SetBool("isBarrier", false);

                    cooldown += originalCooldown * 0.5f;   // 쿨타임 절반 증가
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 흡수 시작");
            
            data.AnimatorParameterSetter.Animator.SetBool("isBarrier", false);

            if (!IsShieldRemovedByPlayer)
            {
                // 2. 캐스팅 시간 초과 처리
                Utils.Destroy(_barrierInstance);
                // 캐스팅 성공 시 플레이어 최대 체력의 N% 흡수
                PlayerController target = data.Target.GetComponent<PlayerController>();
                if (target)
                {
                    float absorbAmount = target.Stats.MaxHealth * 0.2f;
                    target.ApplyDamage(absorbAmount, LayerMask.GetMask("Player"), 1.0f, 1.0f);

                    // 보스 체력 회복 처리
                    data.CurrentHealth = Mathf.Clamp(data.CurrentHealth + absorbAmount, data.CurrentHealth, data.MaxHealth);

                    // 보스 체력바는 AmonPaseTwoFSM.ApplyDamage(피격) 경로에서만 갱신되므로,
                    // 회복처럼 피격 외의 경로로 체력이 변할 때는 여기서 직접 갱신해야 한다.
                    Managers.GUIManager.Instance.GameUIController.UpdateBossHpBar(data.CurrentHealth, data.MaxHealth);
                }
            }
            else
            {
                // 파훼 보상: 일정 시간 동안 보스를 행동 불능으로 만든다.
                // 상태이상 시스템이 없어 AmonPaseTwoFSM.Think()가 패턴 선택을 건너뛰는 방식으로 대체한다.
                // (쿨타임 증가는 이 스킬의 재등장 빈도만 낮출 뿐, 다음 패턴을 막지 못한다)
                data.GroggyEndTime = Time.time + groggyDuration;

                Debug.Log($"[Amon Phase 2] 영혼 흡수가 플레이어에 의해 중단됨 - {groggyDuration}초간 그로기");
            }

            Debug.Log("[Amon Phase 2] 영혼 흡수 종료");
            yield break;
        }
    }
}
