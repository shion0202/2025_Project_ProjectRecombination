using Monster.AI.Blackboard;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;

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
    
    [CreateAssetMenu(fileName = "Dash", menuName = "MonsterSkills/Amon_Phase2/Dash")]
    public class AmonDash : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject meleeCollisionPrefab;       // 근접 공격 범위를 판단할 프리팹 오브젝트
        [SerializeField] private Vector3 collisionScale;                // 근접 공격 범위
        [SerializeField] private Vector3 collisionOffset;               // 근접 공격 위치
        [SerializeField] private AnimationClip clip;                    // 애니메이션 속도 조절을 위한 캐스팅 애니메이션 클립 (Move_Up)
        [SerializeField] private float dashSpeed = 30.0f;               // 대시 속도
        [SerializeField] private float forwardDistanceOffset = 4.0f;    // 대시할 때 타겟 위치보다 더 나아갈 거리 (0일 경우 타겟 위치까지만 대시)
        [SerializeField] private float arrivalDistance = 1.0f;          // 도착으로 간주할 수 있는 타겟 위치 거리 (0일 경우 도착 지점일 때 정지)
        private GameObject meleeCollisionObject;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 돌진 시작");

            // 3. 돌진 애니메이션 재생
            data.AnimatorParameterSetter.Animator.speed = 1.0f;
            data.AnimatorParameterSetter.Animator.SetTrigger("DashTrigger");

            //meleeCollision.gameObject.SetActive(true);
            meleeCollisionObject = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            AmonMeleeCollision meleeCollision = meleeCollisionObject.GetComponent<AmonMeleeCollision>();
            if (meleeCollision)
            {
                meleeCollision.Init(damage, collisionScale, collisionOffset);
            }

            // 4. NavMeshAgent는 바닥에 고정 상태고, 본과 스킨 메시만 움직이므로
            //   agent는 위치 갱신 안하고, 실제 움직임은 Transform.Translate 활용
            Vector3 lookDir = data.Target.transform.position - data.Agent.transform.position;
            lookDir.y = 0;
            data.Agent.transform.rotation = Quaternion.LookRotation(lookDir);

            Vector3 dashStartPos = data.Agent.transform.position;
            Vector3 targetPos = data.Target.transform.position + data.Agent.transform.forward * 4.0f;
            float distance = Vector3.Distance(dashStartPos, targetPos);

            float elapsed = 0f;
            float dashDuration = distance / dashSpeed; // 돌진 시간 계산
            while (elapsed < dashDuration)
            {
                // 방향 업데이트
                Vector3 dir = (targetPos - data.Agent.transform.position).normalized;
                dir.y = 0;

                data.Agent.transform.position += dir * dashSpeed * Time.deltaTime;

                // 도착 체크
                if (Vector3.Distance(data.Agent.transform.position, targetPos) <= arrivalDistance) break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 5. Idle 애니메이션으로 전환
            data.AnimatorParameterSetter.Animator.SetTrigger("IdleTrigger");

            //meleeCollision.gameObject.SetActive(false);
            Utils.Destroy(meleeCollisionObject);

            Debug.Log("[Amon Phase 2] 돌진 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 돌진 준비");

            // 1. 플레이어 방향 쳐다보기 (Y축 회전만)
            Vector3 lookDir = data.Target.transform.position - data.Agent.transform.position;
            lookDir.y = 0;
            data.Agent.transform.rotation = Quaternion.LookRotation(lookDir);

            // 2. 상승 애니메이션 재생
            data.AnimatorParameterSetter.Animator.SetTrigger("RaiseTrigger");
            data.AnimatorParameterSetter.Animator.speed = animSpeed;
            // castTime = clip.length / animSpeed;

            return base.Casting(data);
        }
    }
}
