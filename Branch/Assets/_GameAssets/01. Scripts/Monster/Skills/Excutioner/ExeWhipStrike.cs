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
    
    [CreateAssetMenu(fileName = "WhipStrike", menuName = "MonsterSkills/Executioner/WhipStrike")]
    public class ExeWhipStrike : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject attackRangePrefab;      // 부채꼴 공격 범위 시각화 프리팹
        [SerializeField] private Vector3 attackRangeOffset;         // 공격 범위 프리팹 오프셋
        [SerializeField] private GameObject meleeCollisionPrefab;   // 실제 공격 판정 프리팹
        [SerializeField] private Vector3 collisionScale;            // 공격 판정 크기 (attackRangePrefab과 공유)
        [SerializeField] private Vector3 collisionOffset;           // 공격 판정 오프셋
        [SerializeField] private AnimationClip attackClip;          // 공격 애니메이션(chainsword) 클립
        [SerializeField] private float rotateSpeed = 1.0f;          // 망나니 회전 속도
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip whipStrikeAudioClip;     // 채찍 공격 사운드 클립
        
        private GameObject _attackRangeInstance;
        private GameObject _meleeCollisionObject;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Executioner] Whip Strike 시작");

            // 2. 캐스팅 종료 후 공격 범위 오브젝트 제거
            Utils.Destroy(_attackRangeInstance);

            // 3. 공격 애니메이션 재생
            data.AudioSource.PlayOneShot(whipStrikeAudioClip);
            data.AnimatorParameterSetter.Animator.SetBool("isChainsword", true);

            // 4. 공격 판정 오브젝트 생성 후 초기화
            _meleeCollisionObject = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            _attackRangeInstance.transform.localPosition = Vector3.zero;
            _attackRangeInstance.transform.localRotation = Quaternion.identity;

            var meleeComp = _meleeCollisionObject.GetComponent<AmonMeleeCollision>();
            if (meleeComp != null)
            {
                collisionScale = attackRangePrefab.transform.localScale;
                meleeComp.Init(100.0f, collisionScale, collisionOffset);
            }

            // 5. 애니메이션 재생 종료 시까지 대기
            yield return new WaitForSeconds(attackClip.length);

            // 6. 패턴 종료 후 초기화 로직
            data.AnimatorParameterSetter.Animator.SetBool("isChainsword", false);
            Utils.Destroy(_meleeCollisionObject);

            Debug.Log("[Executioner] Whip Strike 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Executioner] Whip Strike 준비");

            // 1. 현재 플레이어 방향에 공격 범위 표시
            // targetDirection(몬스터가 플레이어를 바라보는 방향)을 기준으로 인스턴스 생성
            Vector3 targetDir = data.Target.transform.position - data.Agent.transform.position;
            targetDir.y = 0.0f;
            _attackRangeInstance = Utils.Instantiate(attackRangePrefab, data.Agent.transform.position, Quaternion.identity);
            _attackRangeInstance.transform.localPosition += targetDir.normalized * attackRangeOffset.z;
            _attackRangeInstance.transform.localRotation = Quaternion.LookRotation(targetDir);

            // 캐스팅 시간 동안 targetDirection까지 회전
            float elapsed = 0f;
            while (elapsed < castTime)
            {
                // 타겟까지의 방향 계산 (Y축만 회전: 수평 회전)
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    Quaternion current = data.Agent.transform.rotation;
                    Quaternion target = Quaternion.LookRotation(targetDir);

                    // Slerp로 부드럽게 회전
                    data.Agent.transform.rotation = Quaternion.Slerp(current, target, Time.deltaTime * rotateSpeed);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
