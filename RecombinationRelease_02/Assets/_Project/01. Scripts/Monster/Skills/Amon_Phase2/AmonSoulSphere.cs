using Monster.AI.Blackboard;
using System.Collections;
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
    
    [CreateAssetMenu(fileName = "SoulSphere", menuName = "MonsterSkills/Amon_Phase2/SoulSphere")]
    public class AmonSoulSphere : SkillData
    {
        [Header("그 외 스킬 정보")]
        [SerializeField] private Material originMaterial;               // 기본 머터리얼
        [SerializeField] private Material glowMaterial;                 // 안광 머터리얼
        [SerializeField] private GameObject soulSpherePrefab;           // 영혼 구체 프리팹
        [SerializeField] private int sphereCount = 5;                   // 영혼 구체 스폰 개수
        [SerializeField] private float spawnRadius = 10.0f;             // 보스 몬스터를 기준으로 구체를 스폰할 반지름 범위
        private SkinnedMeshRenderer _smr;                               // 머터리얼을 교체할 렌더러

        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("영혼 구체 시작");

            // 3. 영혼 구체 N개 생성 (랜덤 위치)
            for (int i = 0; i < sphereCount; ++i)
            {
                Vector3 randomPos = data.Agent.transform.position + UnityEngine.Random.insideUnitSphere * spawnRadius;
                randomPos.y = data.Agent.transform.position.y + 0.5f; // 높이 고정, 필요 시 조절
                GameObject go = Utils.Instantiate(soulSpherePrefab, randomPos, Quaternion.identity);
                SoulSphereObject soulSphere = go.GetComponent<SoulSphereObject>();
                if (soulSphere)
                {
                    soulSphere.Init(damage);
                }
            }

            // 4. 원래 머터리얼로 복구
            Material[] mats = _smr.materials;
            mats[3] = originMaterial;
            _smr.materials = mats;

            Debug.Log("영혼 구체 종료");
            yield break;
        }

        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("영혼 구체 준비");

            TargetRenderer targetRenderer = data.Agent.GetComponentInChildren<TargetRenderer>();
            if (targetRenderer)
            {
                _smr = targetRenderer.Smr;
            }

            // 1. 안광 머터리얼로 교체
            Material[] mats = _smr.materials;    // 복사본 받기
            mats[3] = glowMaterial;             // 복사본 수정
            _smr.materials = mats;               // 다시 원본에 할당

            return base.Casting(data);
        }
    }
}
