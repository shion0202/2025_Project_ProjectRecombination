using Monster.AI.Blackboard;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 돌진 (2페이즈)
    /// - 캐스팅: 1.5초
    /// - 효과: 대상을 향해 돌진, 적중 시 넉백 및 대미지
    /// - 예외처리1: 캐스팅 중 스턴, 넉백, 이동 불가 상태가 되면 스킬 취소
    /// - 예외처리2: 대상과 거리가 너무 멀 경우 스킬 취소
    /// - 예외처리3: 돌진 중 장애물에 부딪히면 돌진 취소
    /// </summary>
    
    [CreateAssetMenu(fileName = "SoulAbsorb", menuName = "MonsterSkills/Amon_Phase2/SoulAbsorb")]
    public class AmonSoulAbsorb : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject barrierPrefab;
        [SerializeField] private AnimationClip raiseClip;
        [SerializeField] private Vector3 barrierOffset;
        [SerializeField] private float originalCooldown;                    // 쿨타임 절반 증가를 위한 본래 쿨타임 값
                                                                            // 원래 쿨타임이 변경될 때마다 직접 수정해야하므로 개선 필요
        private GameObject _barrierInstance;
        private bool isShieldRemovedByPlayer = false;

        public bool IsSheldRemovedByPlayer
        {
            get => isShieldRemovedByPlayer;
            set => isShieldRemovedByPlayer = value;
        }

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 흡수 시작");

            // 2. 캐스팅 시간 초과 처리
            Utils.Destroy(_barrierInstance);
            data.AnimatorParameterSetter.Animator.SetBool("isBarrier", false);

            // 캐스팅 성공 시 플레이어 최대 체력의 N% 흡수
            PlayerController target = data.Target.GetComponent<PlayerController>();
            if (target)
            {
                float absorbAmount = target.Stats.MaxHealth * 0.2f;
                target.ApplyDamage(absorbAmount, LayerMask.GetMask("Player"), 1.0f, 1.0f);

                // 보스 체력 회복 처리
                data.CurrentHealth = Mathf.Clamp(data.CurrentHealth + absorbAmount, data.CurrentHealth, data.MaxHealth);
            }

            Debug.Log("[Amon Phase 2] 영혼 흡수 종료");
            yield break;
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 흡수 준비");

            // 1. 캐스팅 시작
            isShieldRemovedByPlayer = false;
            data.AnimatorParameterSetter.Animator.SetBool("isBarrier", true);
            cooldown = originalCooldown;                                        // 쿨타임 초기화

            yield return new WaitForSeconds(raiseClip.length);

            _barrierInstance = Utils.Instantiate(barrierPrefab, data.Agent.transform);
            _barrierInstance.transform.localPosition = barrierOffset;
            _barrierInstance.transform.localRotation = Quaternion.identity;

            // To-do: 실제 보스에 무적 상태를 적용시키고(현재 적용 X), 실제 데미지는 보호막을 통해 계산
            // 보호막에 충돌 판정이 있긴 하나 관통이나 범위 공격을 피할 수가 없어 무적 상태(보호막으로부터의 데미지는 받을 수 있어야함) 고려가 필요한 상황
            // 보호막 체력이 다 될 경우 보스 쪽 변수를 변경하여 보호막 파괴 여부를 확인하고 그에 따른 기믹 처리

            float elapsed = 0f;
            while (elapsed < castTime)
            {
                Debug.Log("Hp: " + data.CurrentHealth + "/" + data.MaxHealth);
                Vector3 direction = data.Target.transform.position - data.Agent.transform.position;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    data.Agent.transform.rotation = Quaternion.Slerp(data.Agent.transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                // 보호막이 파괴되었는지 체크
                if (isShieldRemovedByPlayer)
                {
                    Utils.Destroy(_barrierInstance);
                    data.AnimatorParameterSetter.Animator.SetBool("isBarrier", false);

                    cooldown += originalCooldown * 0.5f;   // 쿨타임 절반 증가
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
