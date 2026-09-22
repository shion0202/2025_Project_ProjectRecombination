using Managers;
using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : Bullet
{
    [SerializeField] private GameObject bladePrefab;    // 칼날 투사체 프리팹
    [SerializeField] private GameObject collisionBulletPrefab; // 충돌 이펙트 프리팹
    [SerializeField] private float explosionDamage = 300.0f;
    [SerializeField] private float bladeDamage = 50.0f;
    [SerializeField] private float bladeInterval = 0.2f;  // 칼날 발사 주기
    [SerializeField] private int bladesOnDeath = 14;    // 소멸 시 발사 칼날 수
    private float bladeTimer = 0f;

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
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, targetMask);
        foreach (Collider collider in colliders)
        {
            _damagedTargets.Clear();

            TakeDamage(collider.transform);
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
            if (blade == null) continue;

            blade.transform.position = transform.position;
            blade.transform.rotation = Quaternion.LookRotation(direction);
            ProjectileBlade bladeComp = blade.GetComponent<ProjectileBlade>();
            if (bladeComp != null)
            {
                bladeComp.Init(From, null, transform.position, Vector3.zero, direction.normalized, bladeDamage);
            }
        }
    }

    public override void ResetBullet()
    {
        base.ResetBullet();

        bladeTimer = 0.0f;
    }

    public override string ToString()
    {
        string baseLog = base.ToString();
        string log = $"{baseLog}\n" + $"Blade Interval: {bladeInterval}, Blades On Death: {bladesOnDeath}, Blade Timer: {bladeTimer}";
        return log;
    }

    protected override void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        IDamagable enemy = target.GetComponent<IDamagable>();
        if (enemy != null)
        {
            Transform otherParent = target.transform;
            if (_damagedTargets.Contains(otherParent)) return;
            _damagedTargets.Add(otherParent);

            enemy.ApplyDamage(Damage * coefficient, targetMask);

            // 쏜 주체가 이미 파괴된 뒤에 탄이 명중할 수 있으므로 null을 확인한다.
            if (From != null && From.CompareTag("Player"))
            {
                GUIManager.Instance.GameUIController.StartHitCrosshair();
            }
        }
        else
        {
            enemy = target.transform.GetComponentInParent<IDamagable>();
            if (enemy != null)
            {
                // 몬스터는 FSM 루트를 중복 판정 기준으로 쓰지만, 플레이어처럼 FSM이 없는 대상도 맞을 수 있다.
                // 예전처럼 null.transform을 바로 부르면 보스 오브가 플레이어를 맞히는 순간 예외가 나고,
                // ExplodeBlades의 순회가 중단되어 소멸 시 칼날 발사까지 통째로 건너뛰어진다.
                FSM fsm = target.transform.GetComponentInParent<FSM>();
                Transform otherParent = fsm != null ? fsm.transform : (enemy as Component)?.transform;
                if (otherParent == null) otherParent = target;

                if (_damagedTargets.Contains(otherParent)) return;
                _damagedTargets.Add(otherParent);

                enemy.ApplyDamage(Damage * coefficient, targetMask);

                if (From != null && From.CompareTag("Player"))
                {
                    GUIManager.Instance.GameUIController.StartHitCrosshair();
                }
            }
        }
    }

//#if UNITY_EDITOR
//    private void OnDrawGizmosSelected()
//    {
//        // 폭발 반경을 빨간색 반투명 구로 표시
//        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
//        Gizmos.DrawSphere(transform.position, explosionRadius);

//        // 폭발 반경 외곽선(선택사항)
//        Gizmos.color = Color.red;
//        Gizmos.DrawWireSphere(transform.position, explosionRadius);
//    }
//#endif
}
