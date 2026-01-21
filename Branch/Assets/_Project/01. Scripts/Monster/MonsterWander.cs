using UnityEngine;

namespace Monster
{
    [System.Serializable]
    public class MonsterWander
    {
        public float minWanderTime = 5f;    // 최소 방황 시간
        public float maxWanderTime = 10f;   // 최대 방황 시간
        public Vector3 wanderAreaCenter;    // 방황 영역 중심
        public float wanderAreaRadius;      // 방황 영역 반경
        
        private Vector3 _currentWanderPoint;  // 현재 방황 지점
        private float _currentWanderTime;     // 현재 방황 시간
        private float _startWanderTime;       // 방황 시작 시간
        // private bool _isWandering;            // 방황 중인지 여부
        
        public MonsterWander(Vector3 center, float radius)
        {
            wanderAreaCenter = center;
            wanderAreaRadius = radius;
            _currentWanderPoint = GetRandomWanderPoint();
        }
        
        public Vector3 CurrentWanderPoint
        {
            get => _currentWanderPoint;
            set => SetWanderPoint(value);
        }
        public float CurrentWanderTime
        {
            get => _currentWanderTime;
            set => _currentWanderTime = value;
        }
        public float StartWanderTime
        {
            get => _startWanderTime;
            set => _startWanderTime = value;
        }
        // public bool IsWandering
        // {
        //     get => _isWandering;
        //     set => _isWandering = value;
        // }
        
        public Vector3 GetRandomWanderPoint()
        {
            // 방황 영역 내에서 랜덤한 위치를 생성
            var randomPoint = wanderAreaCenter + Random.insideUnitSphere * wanderAreaRadius;
            randomPoint.y = wanderAreaCenter.y; // y 좌표를 중심의 y 좌
            return randomPoint;
        }
        
        public void SetWanderPoint(Vector3 point)
        {
            // 방황 지점을 설정하고, 해당 지점이 방황 영역 내에 있는지 확인
            if (Vector3.Distance(point, wanderAreaCenter) <= wanderAreaRadius)
            {
                _currentWanderPoint = point;
            }
            else
            {
                Debug.LogError("Wander point is outside the defined area.");
            }
        }
        
        public float GetRandomWanderTime()
        {
            // 방황 시간을 랜덤하게 반환
            return Random.Range(minWanderTime, maxWanderTime);
        }
    }
}