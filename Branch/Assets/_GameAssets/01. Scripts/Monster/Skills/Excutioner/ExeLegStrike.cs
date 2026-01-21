using FIMSpace.FProceduralAnimation;
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
    
    [CreateAssetMenu(fileName = "LegStrike", menuName = "MonsterSkills/Executioner/LegStrike")]
    public class ExeLegStrike : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject meleeCollisionPrefab;
        [SerializeField] private Vector3 collisionScale;
        [SerializeField] private Vector3 collisionOffset;
        [SerializeField] private AnimationClip legClip;
        [SerializeField] private float rotateSpeed = 4.0f;
        private GameObject _meleeCollisionObject;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip legStrikeAudioClip;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Executioner] Leg Strike 시작");

            // LegsAnimator를 적용 중일 경우, Glue 옵션으로 인해 다리를 드는 애니메이션이 뭉개지는 문제가 있으므로 접근 필요
            // data.LegAnimator;
            float mainGlueBlend = 0;
            if (data.LegAnimator)
            {
                mainGlueBlend = data.LegAnimator.MainGlueBlend;
                data.LegAnimator.MainGlueBlend = 0.0f;
            }
            data.AudioSource.PlayOneShot(legStrikeAudioClip);
            data.AnimatorParameterSetter.Animator.SetBool("isLegStrike", true);

            _meleeCollisionObject = Utils.Instantiate(meleeCollisionPrefab, data.Agent.transform);
            _meleeCollisionObject.transform.localPosition = Vector3.zero;
            _meleeCollisionObject.transform.localRotation = Quaternion.identity;

            var meleeComp = _meleeCollisionObject.GetComponent<AmonMeleeCollision>();
            if (meleeComp != null)
            {
                meleeComp.Init(damage, collisionScale, collisionOffset);
            }

            yield return new WaitForSeconds(legClip.length);

            if (data.LegAnimator)
            {
                data.LegAnimator.MainGlueBlend = mainGlueBlend;
            }
            data.AnimatorParameterSetter.Animator.SetBool("isLegStrike", false);
            Utils.Destroy(_meleeCollisionObject);

            Debug.Log("[Executioner] Leg Strike 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Executioner] Leg Strike 준비");

            float elapsed = 0f;
            while (elapsed < castTime)
            {
                Vector3 lookDir = data.Target.transform.position - data.Agent.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion now = data.Agent.transform.rotation;
                    Quaternion target = Quaternion.LookRotation(lookDir);
                    data.Agent.transform.rotation = Quaternion.Slerp(now, target, Time.deltaTime * rotateSpeed);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
