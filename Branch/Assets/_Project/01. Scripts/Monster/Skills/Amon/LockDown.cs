using System.Collections;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬 이름: 영혼 감금
    /// - 캐스팅 시간 동안 전투 필드 중 임의의 위치에 안전 구역 생성
    /// - 캐스팅 시간 동안 대량의 잡몹 소환
    /// - 캐스팅 완료 시 소환된 잡몹들이 자폭하며 주변 범위 내 플레이어에게 대미지 적용
    /// - 안전 구역에 있을 경우 자폭 대미지를 받지 않음
    /// - 잡몹 자폭 직전에 플레이어가 안전 구역 안에 있을 경우 무적 상태를 적용 ( 캐스팅 시간 동안에는 무적 아님 )
    /// - 잡몹 자폭 후 무적 상태 해제
    /// - 파회법: 캐스팅 시간 동안 스폰 되는 잡몹 들을 처치하고 안전 구역에서 벗어나지 않아야 함
    /// </summary>
    [UnityEngine.CreateAssetMenu(fileName = "LockDown", menuName = "MonsterSkills/Amon/LockDown")]
    public class LockDown: SkillData
    {
        public override IEnumerator Activate(Monster.AI.Blackboard.Blackboard data)
        {
            // Debug.Log("LockDown Activate");
            yield return null;
        }
    }
}