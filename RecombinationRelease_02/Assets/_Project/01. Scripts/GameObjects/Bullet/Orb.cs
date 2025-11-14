using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : Bullet
{
    [SerializeField] private GameObject bladePrefab;    // 칼날 투사체 프리팹
    [SerializeField] private GameObject collisionBulletPrefab; // 충돌 이펙트 프리팹
    [SerializeField] private float bladeInterval = 0.2f;  // 칼날 발사 주기
    [SerializeField] private int bladesOnDeath = 14;    // 소멸 시 발사 칼날 수
    private float bladeTimer = 0f;

    protected override void Start()
    {
        base.Start();
        bladeTimer = 0.0f;
    }

    protected override void Update()
    {
        base.Update();

        // 주기적으로 랜덤 방향 칼날 발사
        if (bladeTimer > 0)
        {
            bladeTimer -= Time.deltaTime;
        }
        else
        {
            bladeTimer = bladeInterval;
            FireBladeProjectile();
        }
    }

    protected override void ShootByPlayer(Collider other)
    {
        TakeDamage(other.transform);
    }

    protected override void ShootByEnemy(Collider other)
    {
        TakeDamage(other.transform);
    }

    protected override void ImpactObstacle(Collider other)
    {
        DestroyBullet();
    }

    protected override void DestroyBullet(Transform parent = null)
    {
        ExplodeBlades();
        base.DestroyBullet(parent);
    }

    protected override void Explode()
    {
        if (explosionEffectPrefab)
        {
            // 필요할 경우 Pooling
            Utils.Instantiate(explosionEffectPrefab, transform.position, Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f)));
        }
    }

    private void ExplodeBlades()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in colliders)
        {
            //TakeDamage(collider.transform);
            Utils.Destroy(Utils.Instantiate(collisionBulletPrefab, collider.transform.position, Quaternion.identity), 0.1f);
        }

        // 구체 소멸 시 다방향 칼날 일제 발사
        FireBladeProjectile(bladesOnDeath);
    }

    private void FireBladeProjectile(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Random.onUnitSphere;

            GameObject blade = Utils.Instantiate(bladePrefab);
            blade.transform.position = transform.position;
            blade.transform.rotation = Quaternion.LookRotation(direction);
            ProjectileBlade bladeComp = blade.GetComponent<ProjectileBlade>();
            if (bladeComp != null)
            {
                bladeComp.Init(gameObject, null, transform.position, Vector3.zero, direction.normalized, Damage * 0.5f);
            }
        }
    }

    public override string ToString()
    {
        string baseLog = base.ToString();
        string log = $"{baseLog}\n" + $"Blade Interval: {bladeInterval}, Blades On Death: {bladesOnDeath}, Blade Timer: {bladeTimer}";
        return log;
    }
}
