using Monster.AI.Blackboard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    
    [CreateAssetMenu(fileName = "LaunchFullStrike", menuName = "MonsterSkills/Executioner/LaunchFullStrike")]
    public class ExeLaunchFullStrike : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private GameObject missilePrefab;          // 미사일 프리팹
        [SerializeField] private Vector3 missileOffset;           // 미사일 발사 오프셋
        [SerializeField] private GameObject aoeFieldPrefab;         // 원형 장판 프리팹
        [SerializeField] private int missileCount;                  // 미사일 수
        [SerializeField] private float aoeRadius = 10.0f;           // 장판 반경
        [SerializeField] private float missileDropHeight = 30.0f;
        [SerializeField] private float missileDropTime = 1.0f;
        [SerializeField] private float missileInterval = 0.1f;
        private GameObject[] aoeFields;
        private GameObject[] activeMissiles;

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Executioner] Launch Full Strike 시작");

            // 4. 장판 위치에 낙하 미사일 생성 및 낙하 처리
            activeMissiles = new GameObject[missileCount];
            Vector3 missileSpawnPos = data.Agent.transform.position + Vector3.up * missileDropHeight;
            for (int i = 0; i < missileCount; i++)
            {
                if (aoeFields[i] == null) continue;
                GameObject fallMissile = Utils.Instantiate(missilePrefab, missileSpawnPos, Quaternion.identity);
                activeMissiles[i] = fallMissile;

                ExeMissile em = fallMissile.GetComponent<ExeMissile>();
                if (em)
                {
                    em.Init(aoeFields[i].transform.position, true, missileDropTime);
                }
                yield return new WaitForSeconds(0.1f);
            }

            // 6. 장판 제거
            yield return new WaitForSeconds(missileDropTime);

            foreach (GameObject aoe in aoeFields)
            {
                if (aoe != null)
                {
                    Utils.Destroy(aoe);
                }
            }

            Debug.Log("[Executioner] Launch Full Strike 종료");
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Executioner] Launch Full Strike 준비");

            // 1. 상승 연출용 미사일 여러 개 생성 및 발사
            data.AnimatorParameterSetter.Animator.SetBool("isMissile", true);
            GameObject shootObject = null;
            for (int i = 0; i < missileCount; i++)
            {
                // 기본 상승 방향
                Vector3 baseDir = Vector3.up;

                // 랜덤 각도(예: 최대 ±15도 정도로 퍼뜨림)
                float maxAngle = 15f;

                // Vector3.up을 기준으로 랜덤 회전 생성 (X/Z축 기준으로 살짝 기울이기)
                Vector3 randomAxis = UnityEngine.Random.onUnitSphere;
                Quaternion randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(-maxAngle, maxAngle), randomAxis);

                // 회전된 방향 적용
                Vector3 randomDir = randomRot * baseDir;

                // 목표 지점: 랜덤 방향으로 일정 높이 상승
                shootObject = new GameObject();
                Transform shootPoint = shootObject.transform;
                shootPoint.transform.SetParent(data.Agent.transform);
                shootPoint.transform.localPosition = missileOffset;
                shootPoint.transform.localRotation = Quaternion.identity;

                Vector3 start = shootPoint.position;
                Vector3 target = start + randomDir * missileDropHeight;
                GameObject spawnedMissile = Utils.Instantiate(missilePrefab, shootPoint.position, Quaternion.identity);
                ExeMissile em = spawnedMissile.GetComponent<ExeMissile>();
                if (em)
                {
                    em.Init(target, false, missileDropTime);
                }

                yield return new WaitForSeconds(0.1f);
            }
            data.AnimatorParameterSetter.Animator.SetBool("isMissile", false);

            // 2. 플레이어 주변에 공격 장판 생성
            aoeFields = new GameObject[missileCount];
            Vector3 spawnPos = new Vector3(
                    data.Target.transform.position.x,
                    data.Target.transform.position.y + 0.1f,
                    data.Target.transform.position.z
                );
            aoeFields[0] = Utils.Instantiate(aoeFieldPrefab, spawnPos, Quaternion.identity);
            for (int i = 1; i < missileCount; i++)
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * aoeRadius;
                spawnPos = new Vector3(
                    data.Target.transform.position.x + randomOffset.x,
                    data.Target.transform.position.y + 0.1f,
                    data.Target.transform.position.z + randomOffset.y
                );
                aoeFields[i] = Utils.Instantiate(aoeFieldPrefab, spawnPos, Quaternion.identity);
            }

            if (shootObject != null)
            {
                Utils.Destroy(shootObject);
            }

            yield return new WaitForSeconds(castTime);
        }
    }
}
