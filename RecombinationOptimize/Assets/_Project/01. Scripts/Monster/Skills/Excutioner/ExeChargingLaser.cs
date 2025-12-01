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
    
    [CreateAssetMenu(fileName = "ChargingLaser", menuName = "MonsterSkills/Executioner/ChargingLaser")]
    public class ExeChargingLaser : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private Vector3 laserOffset;
        [SerializeField] private float attackDuration;
        [SerializeField] private float rotateSpeed = 2.0f;
        private Transform _shootPoint;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip chargingLaserAudioClip;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Executioner] Charging Laser 시작");

            /*
             * To-do: 현재 TargetPos를 정확히 추적하고 있어 패턴을 피할 수 없는 상태
             * 이전에 플레이어가 이동할 경우 서서히 플레이어의 위치로 이동하는 Follow target point에 대한 얘기가 나왔었는데,
             * Monster Manager에서 Player를 관리하는 것처럼 Target Point 정보도 함께 관리하고, 몬스터가 조준할 Target Point를 해당 오브젝트의 좌표 값으로 설정하는 건 어떤지?
             * 빌드 전 얘기해봐야 하는데 까먹을 수 있어서 일단 주석으로도 기록함
             */

            // 2. 캐스팅 완료 후, 타겟 방향으로 레이저 발사
            GameObject shootObject = new GameObject();
            _shootPoint = shootObject.transform;
            _shootPoint.transform.SetParent(data.Agent.transform);
            _shootPoint.transform.localPosition = laserOffset;
            _shootPoint.transform.localRotation = Quaternion.identity;

            GameObject laser = Utils.Instantiate(laserPrefab, _shootPoint);
            laser.transform.localPosition = Vector3.zero;
            laser.transform.localRotation = Quaternion.identity;
            
            data.AudioSource.PlayOneShot(chargingLaserAudioClip);
            data.AnimatorParameterSetter.Animator.SetBool("isLaser", true);

            float elapsed = 0f;
            while (elapsed < attackDuration)
            {
                laser.transform.rotation = Quaternion.LookRotation(data.Target.transform.position - _shootPoint.position);

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

            data.AnimatorParameterSetter.Animator.SetBool("isLaser", false);
            Utils.Destroy(laser);
            Destroy(shootObject);
            _shootPoint = null;

            Debug.Log("[Executioner] Charging Laser 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Executioner] Charging Laser 준비");

            // To-do: 레이저 패턴임을 식별하기 쉽도록, laserOffset 위치에 차지 또는 발광 이펙트 생성 필요
            // 1. 캐스팅 중 레이저 본이 느리게 플레이어를 따라감
            // 애니메이션이 적용된 상태에서 본 회전 구현이 어려워, transform 전체를 회전시키도록 구현한 상태
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
