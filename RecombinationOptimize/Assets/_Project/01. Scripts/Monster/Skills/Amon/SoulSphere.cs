using Managers;
using System.Collections;
using UnityEngine;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 영혼 구체
    /// 던전 내 랜덤 위치에 영혼 구체 생성
    /// </summary>
    [CreateAssetMenu(fileName = "SoulSphere", menuName = "MonsterSkills/Amon/SoulSphere")]
    public class SoulSphere : SkillData
    {
        [SerializeField] private GameObject soulSpherePrefab;
        [SerializeField] private int numberOfSpheres = 1; // 생성할 영혼 구체의 수
        
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            Debug.Log("영혼 구체 생성!");
            
            for (int i = 0; i < numberOfSpheres; i++)
            {
                // 직접 생성 ( 이때 풀 메니저를 사용 )
                // 스킬 데이터가 가진 범위 정보를 사용하여 범위 내 랜덤 위치 계산
                var randomPos = range != null ? GetRandomPos() : data.transform.position;
                // 몬스터의 현재 위치를 기준으로 랜덤 위치 설정
                GameObject sphere = PoolManager.Instance.GetObject(soulSpherePrefab, randomPos, Quaternion.identity);
            }
            yield return new WaitForSeconds(0.5f); // 잠시 대기
            
            data.CurrentState = "Idle"; // 상태를 Idle로 강제 변경 (이후에 더 나은 방법을 찾아볼 것)
        }
        
        private Vector3 GetRandomPos()
        {
            // 범위 내 랜덤 위치 계산 (예: 원형 범위 내)
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * range;
            Vector3 randomPos = new(randomCircle.x, 0, randomCircle.y);
            return randomPos;
        }
    }
}