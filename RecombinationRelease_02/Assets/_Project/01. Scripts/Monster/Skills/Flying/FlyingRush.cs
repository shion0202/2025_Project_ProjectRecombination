using Monster.AI.Blackboard;
using System.Collections;
using UnityEngine;

// 빠른 속도로 돌진해 플레이어를 통과해 지나는 스킬
// trigger 콜라이더를 생성해 충돌 판정을 처리한다.

namespace _Test.Skills.Flying
{
    [CreateAssetMenu(fileName = "FlyingRush", menuName = "MonsterSkills/Flying/FlyingRush")]
    public class FlyingRush : SkillData
    {
        [Header("그 외 스킬 정보")] 
        [SerializeField] public float rushSpeed;
        [SerializeField] public GameObject meleeCollisionPrefab;       // 근접 공격 범위를 판단할 프리팹 오브젝트
        [SerializeField] public Vector3 collisionScale;                // 근접 공격 범위
        [SerializeField] public Vector3 collisionOffset;               // 근접 공격 위치
        [SerializeField] public float maxRushDistance;
        
        [Header("오디오 클립")]
        [SerializeField] private AudioClip rushAudioClip;
        
        private GameObject _meleeCollision;
        
        public override IEnumerator Casting(Blackboard data)
        {
            data.Agent.transform.LookAt(data.Target.transform);
            data.AnimatorParameterSetter.Animator.SetTrigger("RushReady");
            
            yield return new WaitForSeconds(castTime);
        }
        public override IEnumerator Activate(Blackboard data)
        {
            yield return null;
            // yield return new WaitForSeconds(data.AnimatorParameterSetter.Animator.GetCurrentAnimatorStateInfo(0).length);
            data.Agent.transform.LookAt(data.Target.transform);
            
            // 돌진 애니메이션 재생
            data.AudioSource.PlayOneShot(rushAudioClip);
            data.AnimatorParameterSetter.Animator.SetTrigger("Rush");
            
            // 근접 공격 콜라이더 생성
            _meleeCollision = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            AmonMeleeCollision meleeCollision = _meleeCollision.GetComponent<AmonMeleeCollision>();
            if (meleeCollision)
            {
                meleeCollision.Init(damage, collisionScale, collisionOffset);
            }
            
            // 돌진 설정 (타겟의 뒤로 이동)
            Vector3 rushStartPos = data.Agent.transform.position;
            Vector3 targetPos = data.Target.transform.position - data.Target.transform.forward * maxRushDistance; // 타겟 뒤쪽 지점
            
            float distance = Vector3.Distance(rushStartPos, targetPos);
            float rushDuration = distance / rushSpeed; // 돌진 지속 시간
            float elapsed = 0f;
            
            // 돌진 루프
            while (elapsed < rushDuration)
            {
                Vector3 dir = targetPos - data.Agent.transform.position;
                dir.y = 0;
                // float distToTarget = dir.magnitude;
                // if (distToTarget <= 0.1f) break; // 목표 지점 근접 시 종료
            
                dir = dir.normalized;
                data.Agent.transform.position += dir * (rushSpeed * Time.deltaTime);
            
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 돌진 종료
            // data.AnimatorParameterSetter.Animator.SetTrigger("RushEnd");
            // yield return null;
            yield return new WaitForSeconds(data.AnimatorParameterSetter.Animator.GetCurrentAnimatorStateInfo(0).length - 1f);
            Utils.Destroy(_meleeCollision);
        }
    }
}