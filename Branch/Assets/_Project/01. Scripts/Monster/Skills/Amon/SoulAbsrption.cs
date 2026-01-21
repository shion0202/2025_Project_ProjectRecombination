using Monster.AI.Blackboard;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 영혼 흡수
    /// - 캐스팅 시간 동안 움직이지 않고 플레이어를 응시
    /// - 캐스팅 시작 시 보호막 활성화 되며 보호막이 활성화 되는 동안 받는 모든 대미지 50% 감소
    /// - 캐스팅 시간 동안 보호막 이 유지되며 캐스팅이 완료되면 보호막 비활성화
    /// - 캐스팅 시간 동안 플레이어가 보호막을 제거하지 못하면 캐스팅 완료 후 플레이어 최대 체력의 20%를 흡수하여 자신의 체력 회복
    /// - 플레이어가 캐스팅 시간 동안 보호막을 제거하면 캐스팅이 취소되고 보호막이 비활성화 되며 쿨타임이 절반으로 증가
    /// - 보호막은 플레이어가 일정 시간 공격하지 않으면 자동으로 비활성화 됨
    /// - 보호막이 활성화 된 상태에서 플레이어가 공격하면 보호막이 일정량 대미지를 흡수함
    /// - 보호막이 흡수할 수 있는 대미지의 양은 몬스터의 최대 체력의 10%이며 보호막이 흡수할 수 있는 대미지의 양이 0이 되면 보호막이 비활성화 되고 캐스팅이 취소됨
    /// - 보호막이 활성화 된 상태에서 플레이어가 공격할 때마다 보호막이 흡수할 수 있는 대미지의 양이 감소함
    /// </summary>
    [CreateAssetMenu(fileName = "SoulAbsrption", menuName = "MonsterSkills/Amon/SoulAbsrption")]
    public class SoulAbsrption: SkillData
    {
        public float shieldHealth; // 보호막이 흡수할 수 있는 대미지의 양
        public bool isShieldActive = false; // 보호막 활성화 여부
        public float shieldHealthRatio = 0.2f; // 보호막이 흡수할 수 있는 대미지의 양 비율 (몬스터 최대 체력의 10%)
        public float damageReductionRatio = 0.5f; // 보호막이 활성화 되었을 때 받는 대미지 감소 비율 (50%)
        
        public override IEnumerator Casting(Blackboard data)
        {
            data.Agent.transform.LookAt(data.Target.transform);
            
            // 보호막 활성화
            data.StartCoroutine(ActivateShield(data));
            
            yield return new WaitForSeconds(castTime);
        }

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("영혼 흡수 시전!");
            /// - 보호막이 파괴되면 캐스팅 취소
            /// - 캐스팅 시간 동안 보호막이 유지되면 캐스팅 완료 후 플레이어 최대 체력의 20%를 흡수하여 자신의 체력 회복
            if (isShieldActive)
            {
                // 쉴드 비활성화
                isShieldActive = false;
                
                // 플레이어 최대 체력의 20% 흡수하여 자신의 체력 회복
                
            }
            else
            {
                
            }
            // TODO: 현재 여건상 쿨타임 시간을 동적으로 제어하는 것은 추후에 진행
            yield return null;
        }

        private IEnumerator ActivateShield(Blackboard data)
        {
            isShieldActive = true;
            float shieldHealth = data.MaxHealth * shieldHealthRatio;
            
            // 보호막 활성화
            data.AmonBody.SetActive(false);
            data.AmonShield.gameObject.SetActive(true);

            yield return null;
        }
    }
}