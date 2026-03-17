using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class ParticleFollower : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] ps;
    [SerializeField] private float minAliveTime = 0.2f;
    [SerializeField] private float arrivalThreshold = 1.0f; // 목적지와 이 거리 안에 들어오면 도착으로 간주

    [Header("Wait Time Settings")]
    [SerializeField] private float arrivalStopDelay = 0.5f; // 목적지 도착 시 대기 시간
    [SerializeField] private float blockedStopDelay = 1.0f; // 길이 막혔을 때 대기 시간

    private NavMeshAgent agent;
    private Transform target;
    private bool _isActive;             // 현재 네비게이터 이펙트가 동작 중인지
    private bool _isWaitingToStop;      // 정지 대기 중인지 여부
    private float _aliveTimer;          // 활성화 후 경과 시간
    private float _stopTimer;
    private float _currentWaitTime; // 현재 적용 중인 대기 시간

    public bool IsParticleActivate => ps != null && ps.Length > 0 && ps[0] != null && ps[0].isPlaying;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!_isActive || agent == null || target == null) return;

        // 대기 및 재경로 인식 로직
        if (_isWaitingToStop)
        {
            // 대기 중이라도 다시 길이 뚫리면 복귀 (막혔을 때만 해당)
            if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                Vector3 currentDiff = target.position - transform.position;
                currentDiff.y = 0;

                if (currentDiff.sqrMagnitude > arrivalThreshold * arrivalThreshold)
                {
                    ResumeMovement();
                    return;
                }
            }

            _stopTimer += Time.deltaTime;
            if (_stopTimer >= _currentWaitTime)
            {
                ExecuteStop();
            }
            return;
        }

        // 최소 생존 시간 체크
        _aliveTimer += Time.deltaTime;
        if (_aliveTimer < minAliveTime) return;

        if (!agent.isOnNavMesh) return;

        // 상황별 판정 및 시간 설정
        Vector3 diff = target.position - transform.position;
        diff.y = 0;

        // 목적지 도착 판정 (우선순위 높음)
        if (diff.sqrMagnitude < arrivalThreshold * arrivalThreshold)
        {
            StartStopSequence(arrivalStopDelay);
            return;
        }

        // 경로 차단 판정
        if (!agent.pathPending)
        {
            if (agent.pathStatus == NavMeshPathStatus.PathPartial ||
                agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                (!agent.hasPath && agent.velocity.sqrMagnitude < 0.01f))
            {
                StartStopSequence(blockedStopDelay);
            }
        }
    }

    private void OnDisable()
    {
        _isActive = false;
        _isWaitingToStop = false;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        if (agent == null || target == null || !agent.isOnNavMesh) return;
        if (!gameObject.activeInHierarchy) return;

        if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // NavMesh 밖이라도 일단 좌표로 시도
            agent.SetDestination(target.position);
        }
    }

    public void MoveToTarget()
    {
        if (agent == null || target == null) return;
        if (!gameObject.activeInHierarchy) return;

        // 상태 초기화
        _isActive = true;
        _isWaitingToStop = false;
        _aliveTimer = 0f;
        _stopTimer = 0f;

        // 에이전트 워프 및 경로 설정
        Vector3 startPos = Managers.MonsterManager.Instance.Player.transform.position;
        if (NavMesh.SamplePosition(startPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            startPos = hit.position;

        if (agent.isOnNavMesh) agent.ResetPath();
        agent.Warp(startPos);
        SetTarget(target);

        if (ps != null)
        {
            foreach (var particle in ps)
                if (particle != null) particle.Play();
        }
    }

    // 외부에서 즉시 끄고 싶을 때 호출하는 공개 메서드
    public void StopEffect()
    {
        if (!_isActive) return;
        if (!gameObject.activeSelf) return;

        ExecuteStop();
    }

    private void StartStopSequence(float waitTime)
    {
        _isWaitingToStop = true;
        _stopTimer = 0f;
        _currentWaitTime = waitTime; // 상황에 맞는 시간을 할당

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void ExecuteStop()
    {
        _isActive = false;
        _isWaitingToStop = false;

        if (ps != null)
        {
            foreach (var particle in ps)
                if (particle != null) particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }

    private void ResumeMovement()
    {
        _isWaitingToStop = false;
        _stopTimer = 0f;

        // 다시 목적지 설정 (SetDestination을 호출해 경로를 갱신)
        SetTarget(target);
    }
}
