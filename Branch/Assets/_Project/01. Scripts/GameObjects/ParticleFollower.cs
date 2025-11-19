using LaserAssetPackage.Tests.LaserAssetPackage.Tests.Runtime.BasicActorTest;
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

        // NavMeshAgent 잠깐 비활성화
        agent.enabled = false;

        // playerPosition을 NavMesh 위 위치로 보정
        NavMeshHit hit;
        Vector3 startPos = Managers.MonsterManager.Instance.Player.transform.position;
        if (NavMesh.SamplePosition(startPos, out hit, 1f, NavMesh.AllAreas))
            startPos = hit.position;

        // 직접 위치 이동
        transform.position = startPos + Managers.MonsterManager.Instance.Player.transform.forward * 1.0f;

        // NavMeshAgent 다시 활성화
        agent.enabled = true;

        // 타겟 위치도 NavMesh 위로 보정
        Vector3 targetPos = target.position;
        if (NavMesh.SamplePosition(targetPos, out hit, 1f, NavMesh.AllAreas))
            targetPos = hit.position;

        agent.isStopped = false;

        transform.rotation = Quaternion.LookRotation(Managers.MonsterManager.Instance.Player.transform.forward);
        agent.SetDestination(targetPos);
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

        foreach (var particle in ps)
        {
            particle.Stop();
        }

        agent.isStopped = true;

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
