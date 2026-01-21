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
    
    [CreateAssetMenu(fileName = "OneInThePipe", menuName = "MonsterSkills/Executioner/OneInThePipe")]
    public class ExeOneInThePipe : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Vector3 shootOffset;
        [SerializeField] private AnimationClip shootClip;
        [SerializeField] private float rotateSpeed = 2.0f;
        [SerializeField] private AudioClip loadAudioClip;
        [SerializeField] private AudioClip shootAudioClip;
        private Transform _shootPoint;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Executioner] One In The Pipe 시작");

            // 2. 사격 애니메이션 재생
            data.AudioSource.PlayOneShot(shootAudioClip);
            data.AnimatorParameterSetter.Animator.SetBool("isShoot", true);

            // 3. 불렛(원거리 공격체) 생성 및 발사
            GameObject shootObject = new GameObject();
            _shootPoint = shootObject.transform;
            _shootPoint.transform.SetParent(data.Agent.transform);
            _shootPoint.transform.localPosition = shootOffset;
            _shootPoint.transform.localRotation = Quaternion.identity;
            
            Vector3 targetPos = data.Target.transform.position;
            Vector3 shootDir = (targetPos - _shootPoint.position).normalized;
            GameObject bulleObject = Utils.Instantiate(bulletPrefab, _shootPoint.position, Quaternion.LookRotation(shootDir));
            Bullet bullet = bulleObject.GetComponent<Bullet>();
            if (bullet)
            {
                bullet.Init(data.Agent, data.Target.transform, _shootPoint.position, targetPos, shootDir, damage);
            }

            yield return new WaitForSeconds(shootClip.length);
            data.AnimatorParameterSetter.Animator.SetBool("isShoot", false);
            Destroy(shootObject);
            _shootPoint = null;

            Debug.Log("[Executioner] One In The Pipe 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Executioner] One In The Pipe 준비");

            // To-do: 팔 IK 적용 필요 (Animation Rigging Package의 Multi-Aim Constraint 컴포넌트)
            // 1. 팔 들기(캐스팅) 애니메이션 재생
            data.AudioSource.PlayOneShot(loadAudioClip);
            data.AnimatorParameterSetter.Animator.SetTrigger("shootCastingTrigger");

            float elapsed = 0f;
            while (elapsed < castTime)
            {
                // 캐스팅 중에도 플레이어 방향으로 회전 가능하게 하려면 이 부분에서 회전
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
