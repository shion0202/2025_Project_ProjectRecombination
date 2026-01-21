using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class ParticleFollower : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] ps;
    [SerializeField] private float minAliveTime = 0.2f;

    private NavMeshAgent agent;
    private Transform target;
    private bool _isActive;          // 현재 네비게이터 이펙트가 동작 중인지
    private float _aliveTimer;       // 활성화 후 경과 시간

    public bool IsParticleActivate
    {
        get
        {
            if (ps == null || ps.Length == 0 || ps[0] == null)
                return false;

            return !ps[0].isStopped;
        }
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!_isActive) return;
        if (agent == null) return;

        // 최소 노출 시간 확보
        _aliveTimer += Time.deltaTime;
        if (_aliveTimer < minAliveTime) return;

        // 에이전트가 유효하게 NavMesh 위에 있을 때만 검사
        if (!agent.isOnNavMesh) return;

        // 도착 판정: 경로 계산 완료 + 남은 거리 적음 + 거의 안 움직이는 상태
        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= agent.stoppingDistance &&
            agent.velocity.sqrMagnitude < 0.01f)
        {
            StopEffect();
        }
    }

    private void OnDisable()
    {
        // 비활성화시 깔끔하게 정리
        _isActive = false;
    }

    public void MoveToTarget()
    {
        if (agent == null) return;
        if (target == null) return;
        if (!gameObject.activeInHierarchy) return;

        // playerPosition을 NavMesh 위 위치로 보정
        NavMeshHit hit;
        Vector3 startPos = Managers.MonsterManager.Instance.Player.transform.position;
        if (NavMesh.SamplePosition(startPos, out hit, 1f, NavMesh.AllAreas))
            startPos = hit.position;

        // 직접 위치 이동
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(startPos);
        }

        // 타겟 위치도 NavMesh 위로 보정
        Vector3 targetPos = target.position;
        if (NavMesh.SamplePosition(targetPos, out hit, 1f, NavMesh.AllAreas))
            targetPos = hit.position;

        // 목적지 설정
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(targetPos);

            // 회전
            Vector3 moveDirection = (agent.destination - transform.position);
            moveDirection.y = 0f;
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
            }
        }

        // 상태 초기화
        _isActive = true;
        _aliveTimer = 0f;

        // 파티클 시작
        if (ps != null)
            foreach (var particle in ps)
                if (particle != null)
                    particle.Play();
    }

    public void StopEffect()
    {
        if (!_isActive) return;
        if (!gameObject.activeSelf) return;

        _isActive = false;

        if (ps != null && ps.Length > 0 && ps[0] != null && ps[0].isStopped) return;

        if (ps != null)
            foreach (var particle in ps)
                if (particle != null)
                    particle.Stop();

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        if (agent == null) return;
        if (!gameObject.activeInHierarchy) return;
        if (target == null) return;
        if (!agent.isOnNavMesh) return;

        NavMeshHit hit;
        Vector3 targetPos = target.position;
        if (NavMesh.SamplePosition(targetPos, out hit, 1f, NavMesh.AllAreas))
            targetPos = hit.position;

        agent.SetDestination(targetPos);
    }
}