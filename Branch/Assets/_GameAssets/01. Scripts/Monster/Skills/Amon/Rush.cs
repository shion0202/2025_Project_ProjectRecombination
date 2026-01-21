using Monster.AI.Blackboard;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 돌진
    /// - 캐스팅: 2초 (캐스팅 중 이동 불가 상태)
    /// - 효과: 대상을 향해 돌진, 적중 시 넉백 및 대미지
    /// - 예외처리1: 캐스팅 중 스턴, 넉백, 이동 불가 상태가 되면 스킬 취소
    /// - 예외처리2: 대상과 거리가 너무 멀 경우 스킬 취소
    /// - 예외처리3: 돌진 중 장애물에 부딪히면 돌진 취소
    /// </summary>
    
    [CreateAssetMenu(fileName = "Rush", menuName = "MonsterSkills/Amon/Rush")]
    public class Rush : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] public float rushSpeed;
        [SerializeField] public GameObject meleeCollisionPrefab;       // 근접 공격 범위를 판단할 프리팹 오브젝트
        [SerializeField] public Vector3 collisionScale;                // 근접 공격 범위
        [SerializeField] public Vector3 collisionOffset;               // 근접 공격 위치
        [SerializeField] public float maxRushDistance;
        
        private GameObject _meleeCollision;
        

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("돌진!");
            // Vector3 directionToTarget = (data.Target.transform.position - data.Agent.transform.position).normalized;
            yield return null;
            yield return new WaitForSeconds(data.AnimatorParameterSetter.Animator.GetCurrentAnimatorStateInfo(0).length);
            data.Agent.transform.LookAt(data.Target.transform);
            data.AnimatorParameterSetter.Animator.SetTrigger("Rush");
            
            _meleeCollision = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            AmonMeleeCollision meleeCollision = _meleeCollision.GetComponent<AmonMeleeCollision>();
            if (meleeCollision)
            {
                meleeCollision.Init(damage, collisionScale, collisionOffset);
            }
            
            Vector3 lookDir = data.Target.transform.position - data.Agent.transform.position;
            lookDir.y = 0;
            data.Agent.transform.rotation = Quaternion.LookRotation(lookDir);
            
            Vector3 rushStartPos = data.Agent.transform.position;
            Vector3 targetPos = data.Target.transform.position + data.Agent.transform.forward * 2.0f;
            float distance = Vector3.Distance(rushStartPos, targetPos);
            
            // data.NavMeshAgent.enabled = false;
            // data.AgentRigidbody.velocity = data.Agent.transform.forward * rushSpeed;
            
            float rushDuration = distance / rushSpeed; // 돌진 지속 시간
            float elapsed = 0f;
            // Vector3 rushDirection = data.Agent.transform.forward;
        
            while (elapsed < rushDuration)
            {
                Vector3 dir = (targetPos - data.Agent.transform.position).normalized;
                dir.y = 0;
                data.Agent.transform.position += dir * (rushSpeed * Time.deltaTime);

                if (Vector3.Distance(data.Agent.transform.position, targetPos) <= maxRushDistance) break;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            Debug.Log("돌진 완료");
            // data.AgentRigidbody.isKinematic = false;
            data.AnimatorParameterSetter.Animator.SetTrigger("RushEnd");
            yield return null;
            yield return new WaitForSeconds(data.AnimatorParameterSetter.Animator.GetCurrentAnimatorStateInfo(0).length);
            Utils.Destroy(_meleeCollision);
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("돌진 시전 준비!");
            // data.AgentRigidbody.velocity = Vector3.zero;
            // data.AgentRigidbody.isKinematic = true;
            data.AnimatorParameterSetter.Animator.SetTrigger("RushReady");
            return base.Casting(data);
        }
    }
}