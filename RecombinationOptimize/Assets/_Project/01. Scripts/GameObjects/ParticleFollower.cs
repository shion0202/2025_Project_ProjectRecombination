using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ParticleFollower : MonoBehaviour
{
    public NavMeshAgent agent;
    public ParticleSystem[] ps;
    public Transform target;
    private Coroutine _deactiveRoutine = null;

    public void MoveToTarget()
    {
        if (target == null) return;

        //if (_deactiveRoutine != null)
        //{
        //    StopCoroutine(_deactiveRoutine);
        //    _deactiveRoutine = null;
        //}

        // playerPosition을 NavMesh 위 위치로 보정
        NavMeshHit hit;
        Vector3 startPos = Managers.MonsterManager.Instance.Player.transform.position;
        if (NavMesh.SamplePosition(startPos, out hit, 1f, NavMesh.AllAreas))
            startPos = hit.position;


        // 직접 위치 이동
        agent.enabled = false;
        transform.position = startPos + Managers.MonsterManager.Instance.Player.transform.forward * 1.0f;
        agent.enabled = true;

        // 타겟 위치도 NavMesh 위로 보정
        Vector3 targetPos = target.position;
        if (NavMesh.SamplePosition(targetPos, out hit, 1f, NavMesh.AllAreas))
            targetPos = hit.position;

        agent.SetDestination(targetPos);
        Vector3 moveDirection = (agent.destination - transform.position).normalized;
        if (moveDirection.sqrMagnitude > 0.001f) // 아주 가까운 경우 예외 처리
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        foreach (var particle in ps)
        {
            particle.Play();
        }
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StopEffect();
        }
    }

    public void StopEffect()
    {
        if (!gameObject.activeSelf) return;
        if (ps[0].isStopped) return;

        foreach (var particle in ps)
        {
            particle.Stop();
        }

        agent.ResetPath();

        //if (_deactiveRoutine != null)
        //{
        //    StopCoroutine(_deactiveRoutine);
        //    _deactiveRoutine = null;
        //}
        //_deactiveRoutine = StartCoroutine(CoDeactive());
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        if (!gameObject.activeSelf) return;

        NavMeshHit hit;
        Vector3 targetPos = target.position;
        if (NavMesh.SamplePosition(targetPos, out hit, 1f, NavMesh.AllAreas))
            targetPos = hit.position;

        agent.SetDestination(targetPos);
    }

    private IEnumerator CoDeactive()
    {
        yield return new WaitForSeconds(1.0f);

        gameObject.SetActive(false);
        _deactiveRoutine = null;
    }
}