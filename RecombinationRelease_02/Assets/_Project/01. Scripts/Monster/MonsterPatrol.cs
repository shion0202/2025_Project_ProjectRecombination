using UnityEngine;

namespace Monster
{
    [System.Serializable]
    public class MonsterPatrol
    {
        public bool isPatrol;
        public float minPatrolTime = 5f;    // 최소 순찰 시간
        public float maxPatrolTime = 10f;   // 최대 순찰 시간
        public Vector3[] wayPoints; // 순찰 경로 포인트 배열
        
        private float _startPatrolTime; // 순찰 시작 시간
        private float _currentPatrolTime; // 현재 순찰 시간
        // private bool _isPatrolling; // 순찰 중인지 여부
        private int _currentWayPointIndex; // 현재 순찰 경로 포인트 인덱스
        
        public float StartPatrolTime
        {
            get => _startPatrolTime;
            set => _startPatrolTime = value;
        }
        public float CurrentPatrolTime
        {
            get => _currentPatrolTime;
            set => _currentPatrolTime = value;
        }

        // public bool IsPatrolling
        // {
        //     get => _isPatrolling;
        //     set => _isPatrolling = value;
        // }
        
        public int CurrentWayPointIndex
        {
            get => _currentWayPointIndex;
            set
            {
                if (value < 0 || value >= wayPoints.Length)
                {
                    Debug.LogError("Invalid waypoint index.");
                    return;
                }
                _currentWayPointIndex = value;
            }
        }
        
        public MonsterPatrol(Vector3[] wayPoints)
        {
            this.wayPoints = wayPoints;
            _currentWayPointIndex = 0;
        }
        
        // public Vector3 GetNextWayPoint()
        // {
        //     if (wayPoints == null || wayPoints.Length == 0)
        //         return Vector3.zero;
        //
        //     Vector3 nextWayPoint = wayPoints[_currentWayPointIndex];
        //     _currentWayPointIndex = (_currentWayPointIndex + 1) % wayPoints.Length; // 순환
        //     return nextWayPoint;
        // }
        
        public int GetNextWayPointIndex()
        {
            if (wayPoints == null || wayPoints.Length == 0)
                return -1;

            int nextIndex  = (_currentWayPointIndex + 1) % wayPoints.Length; // 순환
            return nextIndex;
        }
        
        public Vector3 GetCurrentWayPoint()
        {
            if (wayPoints == null || wayPoints.Length == 0)
                return Vector3.zero;

            return wayPoints[_currentWayPointIndex];
        }
        
        public void SetWayPointIndex(int index)
        {
            if (index < 0 || index >= wayPoints.Length)
            {
                Debug.LogError("Invalid waypoint index.");
                return;
            }
            _currentWayPointIndex = index;
        }
        
        public float GetRandomPatrolTime()
        {
            return Random.Range(minPatrolTime, maxPatrolTime);
        }
    }
}