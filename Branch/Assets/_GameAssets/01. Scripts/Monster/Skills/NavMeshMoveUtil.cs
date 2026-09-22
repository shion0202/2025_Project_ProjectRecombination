using UnityEngine;
using UnityEngine.AI;

namespace _Test.Skills
{
    /// <summary>
    /// 스킬로 몬스터를 직접 옮길 때(텔레포트, 돌진 등) 구워둔 NavMesh를 이동 가능 범위로 삼아
    /// 벽 너머나 맵 밖으로 나가지 않게 제한한다.
    /// NavMesh는 벽에서 에이전트 반경만큼 떨어져 구워지므로, 경계 안이면 몸이 벽을 뚫지 않는다.
    /// </summary>
    public static class NavMeshMoveUtil
    {
        // 공중(점프 등)에 있는 플레이어/몬스터도 바닥을 찾을 수 있도록 넉넉히 잡은 탐색 거리
        private const float SampleDistance = 5.0f;

        /// <summary>position에서 가장 가까운 이동 가능 바닥 위 지점. 주변에 NavMesh가 없으면 false.</summary>
        public static bool TrySnap(Vector3 position, out Vector3 result)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, SampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }

            result = position;
            return false;
        }

        /// <summary>
        /// from에서 to로 직선 이동할 때 NavMesh 경계(벽)에 막히면 막힌 지점까지로 자른 도착점을 돌려준다.
        /// from 주변에 NavMesh가 없으면 제한할 기준이 없으므로 false (호출한 쪽에서 이동을 취소할지 정한다).
        /// </summary>
        public static bool TryClampPath(Vector3 from, Vector3 to, out Vector3 result)
        {
            result = from;
            if (!TrySnap(from, out Vector3 start)) return false;

            // to의 높이는 from 바닥 높이로 맞춰 평면 이동으로 검사한다. (돌진/텔레포트 모두 수평 이동)
            Vector3 end = new Vector3(to.x, start.y, to.z);
            result = NavMesh.Raycast(start, end, out NavMeshHit hit, NavMesh.AllAreas) ? hit.position : end;

            // Raycast는 평면 기준이라 경사가 있으면 높이가 어긋날 수 있어 실제 바닥으로 다시 붙인다.
            if (TrySnap(result, out Vector3 snapped)) result = snapped;
            return true;
        }
    }
}
