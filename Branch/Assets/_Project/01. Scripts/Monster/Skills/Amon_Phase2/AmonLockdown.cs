using Monster.AI.Blackboard;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using static UnityEditor.Experimental.GraphView.GraphView;

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
    
    [CreateAssetMenu(fileName = "Lockdown", menuName = "MonsterSkills/Amon_Phase2/Lockdown")]
    public class AmonLockdown : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private Vector3 defaultPosition;
        [SerializeField] private Vector3 warpPosition;
        [SerializeField] private GameObject chargeEffectPrefab;
        [SerializeField] private Vector3 chargeEffectRotation;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private Vector3 effectSpawnOffset;
        [SerializeField] private List<GameObject> monsterPrefabs = new();
        [SerializeField] private Vector2 spawnTargetPositionX;
        [SerializeField] private Vector2 spawnTargetPositionZ;
        [SerializeField] private float spawnTargetPositionY;
        [SerializeField] private float monsterSpawnInterval = 1.0f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private GameObject safeZonePrefab;
        private GameObject _chargeEffect;
        private GameObject _safeZone;
        private List<IDamagable> _targets = new();

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 감옥 시작");

            // 5.기 모으기 이펙트 제거 및 폭발 이펙트 생성
            if (_chargeEffect != null)
            {
                Utils.Destroy(_chargeEffect);
            }
            Utils.Destroy(Utils.Instantiate(explosionEffectPrefab, data.Agent.transform.position + effectSpawnOffset, Quaternion.identity), 2.0f);
            Utils.Destroy(_safeZone);

            // 6. 플레이어 및 스폰된 몬스터 전체에게 데미지 처리 (임의 함수 호출 예시)
            data.AnimatorParameterSetter.Animator.SetBool("isCharging", false);
            IDamagable target = Managers.MonsterManager.Instance.Player.GetComponent<IDamagable>();
            if (target != null)
            {
                target.ApplyDamage(500.0f, targetMask);
            }
            PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.SetPlayerState(EPlayerState.Invincibility, false);
            }

            // 기믹 종료 후 몬스터를 치우는 걸로 생각하고 있으나 추후 수정 가능
            for (int i = 0; i < _targets.Count; ++i)
            {
                _targets[i].ApplyDamage(10000.0f, targetMask);
            }

            data.NavMeshAgent.Warp(defaultPosition);

            Debug.Log("[Amon Phase 2] 영혼 감옥 종료");
            yield break;
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 감옥 준비");

            // 1. 보스 특정 위치로 이동
            data.NavMeshAgent.Warp(warpPosition);

            // 2. 기 모으기 애니메이션 재생
            data.AnimatorParameterSetter.Animator.SetBool("isCharging", true);

            // 3. 기 모으기 이펙트 생성 (본체 위)
            _chargeEffect = Utils.Instantiate(chargeEffectPrefab, data.Agent.transform.position + effectSpawnOffset, Quaternion.Euler(chargeEffectRotation), data.Agent.transform);

            float x = UnityEngine.Random.Range(spawnTargetPositionX.x, spawnTargetPositionX.y);
            float z = UnityEngine.Random.Range(spawnTargetPositionZ.x, spawnTargetPositionZ.y);
            Vector3 spawnPos = new (x, spawnTargetPositionY + 10.0f, z);
            _safeZone = Utils.Instantiate(safeZonePrefab, spawnPos, Quaternion.identity);

            float elapsed = 0f;
            float spawnTimer = 0f;
            // 4. 캐스팅 시간 동안 몬스터 지속 생성 및 대기
            while (elapsed < castTime)
            {
                elapsed += Time.deltaTime;
                spawnTimer += Time.deltaTime;

                if (spawnTimer >= monsterSpawnInterval)
                {
                    spawnTimer = 0f;

                    // 필드 내 랜덤 위치 계산
                    x = UnityEngine.Random.Range(spawnTargetPositionX.x, spawnTargetPositionX.y);
                    z = UnityEngine.Random.Range(spawnTargetPositionZ.x, spawnTargetPositionZ.y);
                    spawnPos = new Vector3(x, spawnTargetPositionY, z); // 높이는 0 고정, 필요시 수정

                    int rand = UnityEngine.Random.Range(0, monsterPrefabs.Count);
                    GameObject go = Utils.Instantiate(monsterPrefabs[rand], spawnPos, Quaternion.identity);
                    IDamagable damagable = go.GetComponent<IDamagable>();
                    if (damagable != null)
                    {
                        _targets.Add(damagable);
                    }
                }

                yield return null;
            }
        }
    }
}
