using UnityEngine;

public class Missile : Bullet
{
    [SerializeField] protected GameObject collisionBulletPrefab;
    [SerializeField] protected float velocity = 300f;     // Missile velocity
    [SerializeField] protected float alignSpeed = 1f;
    protected Transform _target = null;
    protected Vector3 _step;
    protected Vector3 _targetLastPos;
    protected Vector3 _hitPos;

    protected override void Start()
    {
        base.Start();

        _step = Vector3.zero;
        _targetLastPos = Vector3.zero;
        _hitPos = Vector3.zero;
    }
    
    protected override void Update()
    {
        base.Update();

        // Navigate
        if (_target != null)
        {
            _hitPos = Predict(transform.position, _target.position, _targetLastPos, velocity);
            _targetLastPos = _target.position;
        }
        else
        {
            _hitPos = _to;
        }

        transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(_hitPos - transform.position), Time.deltaTime * alignSpeed);

        // Missile step per frame based on velocity and time
        _step = transform.forward * Time.deltaTime * velocity;

        // Advances missile forward
        transform.position += _step;
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

    protected override void Explode()
    {
        base.Explode();

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in colliders)
        {
            TakeDamage(collider.transform);
            Destroy(Instantiate(collisionBulletPrefab, collider.transform.position, Quaternion.identity), 0.1f);
        }
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

        string log = $"{baseLog}\n" + $"Target: {targetName}, Predicted Hit Position: {_hitPos}, Bullet Velocity: {velocity}, Bullet Align Speed: {alignSpeed}";
        return log;
    }

    //IEnumerator CoMissileRoutine(Vector3 initialDir, Vector3 fromPos)
    //{
    //    float elapsed = 0f;
    //    transform.forward = initialDir;

    //    float distance = Vector3.Distance(fromPos, _to);     // 목표까지 거리
    //    float straightDistance = distance * 0.3f;                   // 직선 비행 거리: 거리의 절반 사용
    //    float initialFlightTime = straightDistance / bulletSpeed;   // 직선 비행 시간 = 거리 / 속도

    //    // 1. 초기 직선 비행 (거리 기반 시간)
    //    while (elapsed < initialFlightTime)
    //    {
    //        transform.position += transform.forward * bulletSpeed * Time.deltaTime;
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    // 2. 타겟 방향으로 부드럽게 선회하며 이동
    //    bool reached = false;
    //    while (!reached)
    //    {
    //        Vector3 dirToTarget = (_to - transform.position).normalized;
    //        transform.forward = Vector3.RotateTowards(
    //            transform.forward,
    //            dirToTarget,
    //            Mathf.Deg2Rad * turnSpeed * Time.deltaTime,
    //            0f
    //        );

    //        transform.position += transform.forward * bulletSpeed * Time.deltaTime;

    //        if (Vector3.Distance(transform.position, _to) < 1.0f)
    //        {
    //            DestroyBullet();
    //            reached = true;
    //        }

    //        yield return null;
    //    }
    //}
}
