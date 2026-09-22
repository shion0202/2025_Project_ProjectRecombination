using Monster.AI.Blackboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Test.Skills
{
    [CreateAssetMenu(fileName = "SoulOrb", menuName = "MonsterSkills/Amon_Phase2/SoulOrb")]
    public class AmonSoulOrb : SkillData
    {
        private static readonly int IsCharging = Animator.StringToHash("isCharging");
        [Header("그 외 스킬 정보")] [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private List<Vector3> bulletSpawnOffset = new();

        // ReSharper disable Unity.PerformanceAnalysis
        public override IEnumerator Activate(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 보주 시작");

            // 2. 총알 생성 및 발사
            foreach (Vector3 t in bulletSpawnOffset)
            {
                // 오프셋을 월드 좌표로 더하면 발사 위치가 아몬의 방향을 따라가지 않아,
                // 아몬이 월드 X축을 보고 쏠 때 두 구체가 진행 방향으로 일렬이 되어 하나로 겹쳐 보인다.
                Vector3 startPosition = data.Agent.transform.TransformPoint(t);
                Vector3 direction = data.Agent.transform.forward;
                direction.y = 0.0f;
                direction.Normalize();
                GameObject orb = Utils.Instantiate(bulletPrefab, startPosition, Quaternion.LookRotation(direction));
                Bullet bullet = orb.GetComponent<Bullet>();
                if (bullet)
                {
                    bullet.Init(data.Agent, data.Target.transform, startPosition, Vector3.zero, direction, damage);
                }
            }

            data.AnimatorParameterSetter.Animator.SetBool(IsCharging, false);

            Debug.Log("[Amon Phase 2] 영혼 보주 종료");
            yield break;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public override IEnumerator Casting(Blackboard data)
        {
            Debug.Log("[Amon Phase 2] 영혼 보주 준비");

            // 1. 캐스팅 애니메이션 재생
            data.AnimatorParameterSetter.Animator.SetBool(IsCharging, true);

            float elapsed = 0f;
            while (elapsed < castTime)
            {
                Vector3 direction = data.Target.transform.position - data.Agent.transform.position;
                direction.y = 0; // y축 회전만 적용

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    data.Agent.transform.rotation = Quaternion.Slerp(data.Agent.transform.rotation, targetRotation,
                        Time.deltaTime * 5f);
                }

                elapsed += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }
        }
    }
}