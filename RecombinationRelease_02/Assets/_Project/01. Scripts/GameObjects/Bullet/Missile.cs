using Managers;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class Missile : Bullet
{
    [SerializeField] protected GameObject collisionBulletPrefab;
    [SerializeField] protected float alignSpeed = 1f;
    [SerializeField] protected float explodeDistanceThreshold = 1.5f;
    protected Transform _target = null;
    protected Vector3 _step;
    protected Vector3 _targetLastPos;
    protected Vector3 _hitPos;
    protected float _explodeDistanceSqr;
    protected Coroutine _explodeRoutine = null;

    protected override void Start()
    {
        base.Start();

        _step = Vector3.zero;
        _targetLastPos = Vector3.zero;
        _hitPos = Vector3.zero;
        _explodeDistanceSqr = explodeDistanceThreshold * explodeDistanceThreshold;
    }
    
    protected override void Update()
    {
        base.Update();

        // Navigate
        if (_target != null)
        {
            _hitPos = Predict(transform.position, _target.position, _targetLastPos, bulletSpeed);
            _targetLastPos = _target.position;
        }
        else
        {
            _hitPos = _to;
        }

        transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(_hitPos - transform.position), Time.deltaTime * alignSpeed);

        // Missile step per frame based on velocity and time
        _step = transform.forward * Time.deltaTime * bulletSpeed;

        // Advances missile forward
        transform.position += _step;

        // 타겟 혹은 목표 지점과 미사일 위치 간 거리 계산
        float distanceToTargetSqr = (_hitPos - transform.position).sqrMagnitude;

        if (distanceToTargetSqr <= _explodeDistanceSqr)
        {
            DestroyBullet();
        }
    }

    protected override void SetBulletLogic(Transform target, Vector3 direction, Vector3 start)
    {
        _target = target;
    }

    protected override void ShootByPlayer(Collider other)
    {
        DestroyBullet(other.transform);
    }

    protected override void ShootByEnemy(Collider other)
    {
        DestroyBullet(other.transform);
    }

    protected override void ImpactObstacle(Collider other)
    {
        DestroyBullet(other.transform);
    }

    protected override void DestroyBullet(Transform parent = null)
    {
        if (_isCollided) return;
        _isCollided = true;

        // 풀링 전 총알의 상태를 초기화
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        Timer = LifeTime;
        muzzleParticle = null;
        impactP = null;

        if (explosionEffectPrefab)
        {
            Explode();
        }
        if (impactParticle)
        {
            CreateImpaceEffect();
        }
    }

    protected override void DestroyBullet(Collision collision)
    {
        if (_isCollided) return;
        _isCollided = true;

        // 풀링 전 총알의 상태를 초기화
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        Timer = LifeTime;
        muzzleParticle = null;
        impactP = null;

        if (explosionEffectPrefab)
        {
            Explode();
        }
        if (impactParticle)
        {
            CreateImpaceEffect(collision);
        }
    }

    protected override void Explode()
    {
        base.Explode();

        // 1. OverlapSphere로 폭발 범위 내의 콜라이더를 찾습니다.
        //    (OverlapSphere 자체는 비용이 들지만, 루프 횟수를 줄이는 것이 더 중요합니다.)
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // 2. 동시에 수많은 오브젝트가 충돌 이펙트를 생성/파괴하는 것을 막기 위해,
        //    코루틴을 사용하여 매 프레임마다 하나씩 처리하도록 분산시킵니다.
        //    **주의: 이 Missile 오브젝트는 곧 파괴되므로, 코루틴은 Missile이 아닌
        //    Scene에 상주하는 다른 MonoBehaviour (예: GameManager, PoolManager)에서
        //    호출하는 것이 안전합니다.**

        // 예시로 StartCoroutine을 사용하지만, 실제로는 Missile이 파괴되기 전에 
        // 코루틴을 안전하게 실행할 수 있는 중앙 관리자(Manager)가 필요합니다.

        // 이 코드에서는 일단 현재 객체에서 코루틴을 실행하며, 
        // DestroyBullet()이 Utils.Destroy(gameObject)를 호출하기 직전에 
        // StartCoroutine이 실행된다고 가정합니다.
        // 만약 DestroyBullet()이 즉시 실행되어 이 코루틴이 시작도 못하고 죽는다면, 
        // 이 코루틴은 **Manager** 클래스 등에서 시작해야 합니다.

        StartCoroutine(ProcessExplosionColliders(colliders));
    }

    protected Vector3 Predict(Vector3 sPos, Vector3 tPos, Vector3 tLastPos, float pSpeed)
    {
        // Target velocity
        var tVel = (tPos - tLastPos) / Time.deltaTime;

        // Time to reach the target
        var flyTime = GetProjFlightTime(tPos - sPos, tVel, pSpeed);

        if (flyTime > 0)
            return tPos + flyTime * tVel;
        return tPos;
    }

    protected float GetProjFlightTime(Vector3 dist, Vector3 tVel, float pSpeed)
    {
        var a = Vector3.Dot(tVel, tVel) - pSpeed * pSpeed;
        var b = 2.0f * Vector3.Dot(tVel, dist);
        var c = Vector3.Dot(dist, dist);

        var det = b * b - 4 * a * c;

        if (det > 0)
            return 2 * c / (Mathf.Sqrt(det) - b);
        return -1;
    }

    public override string ToString()
    {
        string baseLog = base.ToString();
        string targetName = _target != null ? _target.name : "None";

        string log = $"{baseLog}\n" + $"Target: {targetName}, Predicted Hit Position: {_hitPos}, Bullet Align Speed: {alignSpeed}";
        return log;
    }

    /// <summary>
    /// 폭발 범위 내의 콜라이더들을 매 프레임/간격으로 분할 처리하는 코루틴
    /// </summary>
    /// <param name="colliders">OverlapSphere로 찾은 콜라이더 배열</param>
    protected IEnumerator ProcessExplosionColliders(Collider[] colliders)
    {
        // 12개의 미사일이 동시에 터져도, 이 코루틴은 각 미사일별로 실행되며,
        // yield return null; 덕분에 매 프레임마다 하나의 대상만 처리합니다.
        foreach (Collider collider in colliders)
        {
            // 데미지 적용
            if (collider)
            {
                TakeDamage(collider.transform);
                GameObject tBullet = Utils.Instantiate(collisionBulletPrefab, collider.transform.position, Quaternion.identity);
                Utils.Destroy(tBullet, 0.1f);
            }

            // 매 반복마다 다음 프레임을 기다려 CPU 부하를 분산시킵니다.
            yield return null;
        }

        Utils.Destroy(gameObject);
    }
}
