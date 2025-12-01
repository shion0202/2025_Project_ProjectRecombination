using Monster.AI.Blackboard;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 질주
    /// - 아몬 1페이즈 돌진 스킬과 동일하게 타겟을 향해 돌진하는 스킬
    /// - 돌진과 달리 속도가 더 빠르고 플레이어와 충돌 시 멈추지 않고 지나간다.
    /// - 장애물에 부딪히면 멈춘다.
    /// </summary>
    [CreateAssetMenu(fileName = "Dash", menuName = "MonsterSkills/Amon/Dash")]
    public class Dash : SkillData
    {
        public float dashSpeed = 25f; // 질주 속도 설정
        
        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("질주 시전!");
            // 목표 지점 설정 (플레이어 위치의 바닥)
            // data.AgentRigidbody.isKinematic = true;
            Vector3 targetPosition = new (data.Target.transform.position.x, data.Target.transform.position.y, data.Target.transform.position.z);
            
            // 질주 방향 설정
            Vector3 dashDirection = (targetPosition - data.Agent.transform.position).normalized;
            Debug.Log(dashDirection);
            data.Agent.transform.LookAt(targetPosition);
            // data.AgentRigidbody.velocity = dashDirection * dashSpeed; // 질주 속도 설정
            float dashDuration = 3.0f; // 질주 지속 시간
            float elapsed = 0f;
            
            while (elapsed < dashDuration)
            {
                data.Agent.transform.position += dashDirection * (dashSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Debug.Log("질주 완료");
            // data.AgentRigidbody.velocity = Vector3.zero;
            // data.AgentRigidbody.isKinematic = false;
            // data.LWing.SetActive(true);
            // data.RWing.SetActive(true);
            // data.Body.SetActive(true);
            // data.EnergyBall.SetActive(false);
            data.AmonBody.SetActive(true);
            data.AmonEnergyBall.gameObject.SetActive(false);
            
            yield return null;
        }
        
        /// <summary>
        /// 캐스팅 시간 동안 이동 불가, 단 공중으로 부유하는 연출
        /// - 부유하는 동안, 보스가 구체화 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("질주 시전 준비!");
            
            yield return null;
            
            float floatHeight = 3f; // 떠오르는 높이
            float floatDuration = castTime;
            float elapsed = 0f;
            
            Vector3 startPos = data.Agent.transform.position;
            Vector3 targetPos = startPos + Vector3.up * floatHeight;
            // data.AgentRigidbody.isKinematic = true;
            // TODO: 기모으는 애니메이션 재생
            
            while (elapsed < floatDuration)
            {
                float t = elapsed / floatDuration;
                data.Agent.transform.position = Vector3.Lerp(startPos, targetPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            data.Agent.transform.position = targetPos; // 정확한 위치 보정
            
            // 캐스팅 완료 후 구체 모드로 변경
            // data.LWing.SetActive(false);
            // data.RWing.SetActive(false);
            // data.Body.SetActive(false);
            // data.EnergyBall.SetActive(true);
            data.AmonBody.SetActive(false);
            // data.AgentCollider.isTrigger = true; // 충돌체를 트리거로 변경
            data.AmonEnergyBall.gameObject.SetActive(true);
            data.AmonEnergyBall.skillData = this;
            Debug.Log("질주 시전 준비 완료!");
        }
    }
}