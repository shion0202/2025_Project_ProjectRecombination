using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Monster.AI;
using Managers;

public class Bullet : MonoBehaviour
{
    #region Variables
    [Header("Components")]
    [SerializeField] protected Rigidbody _rb;

    [Header("Bullet Information")]
    [SerializeField] protected float bulletSpeed = 30.0f;
    [SerializeField] protected float explosionRadius = 2.0f;
    private float _damage;
    private GameObject _from; // 발사 주체
    protected Vector3 _to; // 타겟 위치
    protected Transform _parent; // Spawn Point (Muzzle Flash 등의 Position을 위한 변수)
    protected Vector3 _targetDirection;

    [SerializeField] private float lifeTime = 5f; // 총알의 생명 시간
    private float _timer;

    [Header("Effects")]
    [SerializeField] protected GameObject muzzleParticlePrefab;
    protected GameObject muzzleParticle;
    [SerializeField] protected GameObject impactParticle;
    protected GameObject impactP = null;
    [SerializeField] protected GameObject explosionEffectPrefab;
    #endregion

    #region Properties
    protected float LifeTime
    {
        get => lifeTime;
        set => lifeTime = value;
    }

    protected float Timer
    {
        get => _timer;
        set => _timer = value;
    }

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }

    public GameObject From
    {
        get => _from;
        set => _from = value;
    }

    public Transform Parent
    {
        get => _parent;
        set => _parent = value;
    }
    #endregion

    #region Unity Methods
    protected void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = GetComponentInChildren<Rigidbody>();
        }
    }

    protected virtual void Start()
    {
        _timer = lifeTime;
    }

    protected virtual void Update()
    {
        if (_timer > 0)
        {
            _timer -= Time.deltaTime;
        }
        else
        {
            DestroyBullet();
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // 총알의 규칙
        // 1. 플레이어가 발사한 총알은 적에게만 데미지를 입힌다.
        // 2. 적이 발사한 총알은 플레이어에게만 데미지를 입힌다.
        // 3. 총알은 벽(또는 기타 오브젝트)에 닿으면 파괴된다.

        // 플레이어가 발사한 총알
        if (From.CompareTag("Player") && other.CompareTag("Enemy"))
        {
            ShootByPlayer(other);
            return;
        }
        
        // 적이 발사한 총알
        if (From.CompareTag("Enemy") && other.CompareTag("Player"))
        {
            ShootByEnemy(other);
            return;
        }
        
        // 벽(또는 기타 오브젝트)에 닿은 경우
        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            ImpactObstacle(other);
            return;
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // 총알의 규칙
        // 1. 플레이어가 발사한 총알은 적에게만 데미지를 입힌다.
        // 2. 적이 발사한 총알은 플레이어에게만 데미지를 입힌다.
        // 3. 총알은 벽(또는 기타 오브젝트)에 닿으면 파괴된다.

        // 플레이어가 발사한 총알
        if (From.CompareTag("Player") && collision.gameObject.CompareTag("Enemy"))
        {
            ShootByPlayer(collision);
            return;
        }

        // 적이 발사한 총알
        if (From.CompareTag("Enemy") && collision.gameObject.CompareTag("Player"))
        {
            ShootByEnemy(collision);
            return;
        }

        // 벽(또는 기타 오브젝트)에 닿은 경우
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
        {
            ImpactObstacle(collision);
            return;
        }
    }

#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
        // Explosion이 존재할 경우 폭발 범위를 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_to, explosionRadius);
    }
#endif
    #endregion

    #region Public Methods
    public void Init(GameObject shooter, Transform target, Vector3 start, Vector3 end, Vector3 direction, float damage)
    {
        _from = shooter;
        _to = end;
        _targetDirection = direction;
        _damage = damage;

        if (muzzleParticlePrefab)
        {
            // 필요할 경우 Pooling
            muzzleParticle = Instantiate(muzzleParticlePrefab, transform.position, Quaternion.LookRotation(-_targetDirection), Parent);
            Destroy(muzzleParticle, 2.0f); // Lifetime of muzzle effect.
        }

        SetBulletLogic(target, direction, start);
    }

    public override string ToString()
    {
        string fromName = _from != null ? _from.name : "Null";
        string parentName = _parent != null ? _parent.name : "Null";

        string log = $"[{gameObject.name} ({GetType().Name})] From(Shooter): {fromName}, Target Position: {_to}, " +
            $"Bullet Direction: {_targetDirection}, Parent(Muzzle Flash): {_parent}, Damage: {_damage:F2}, Bullet Speed: {bulletSpeed:F2}, " +
            $"Explosion Radius: {explosionRadius:F2}, Max Life Time: {lifeTime:F2}, Current Life Time: {_timer:F2}";
        return log;
    }
    #endregion

    #region Private Methods
    protected virtual void SetBulletLogic(Transform target, Vector3 direction, Vector3 start)
    {
        _rb.velocity = direction * bulletSpeed;
    }

    // Trigger용 함수와 Collision용 함수를 분리
    protected virtual void ShootByPlayer(Collider other)
    {
        TakeDamage(other.transform);
        DestroyBullet(other.transform);
    }

    protected virtual void ShootByEnemy(Collider other)
    {
        IDamagable damagable = other.gameObject.GetComponent<IDamagable>();
        if (damagable != null)
        {
            damagable.ApplyDamage(_damage);
        }

        DestroyBullet(other.transform);
    }

    protected virtual void ImpactObstacle(Collider other)
    {
        DestroyBullet(other.transform);
    }

    protected virtual void ShootByPlayer(Collision collision)
    {
        TakeDamage(collision.transform);
        DestroyBullet(collision);
    }

    protected virtual void ShootByEnemy(Collision collision)
    {
        IDamagable damagable = collision.gameObject.GetComponent<IDamagable>();
        if (damagable != null)
        {
            damagable.ApplyDamage(_damage);
        }

        DestroyBullet(collision);
    }

    protected virtual void ImpactObstacle(Collision collision)
    {
        DestroyBullet(collision);
    }

    protected virtual void DestroyBullet(Transform parent = null)
    {
        // 풀링 전 총알의 상태를 초기화
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _timer = lifeTime;

        if (explosionEffectPrefab)
        {
            Explode();
        }
        if (impactParticle)
        {
            CreateImpaceEffect(parent);
        }

        PoolManager.Instance.ReleaseObject(gameObject);
    }

    protected virtual void DestroyBullet(Collision collision)
    {
        // 풀링 전 총알의 상태를 초기화
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _timer = lifeTime;

        if (explosionEffectPrefab)
        {
            Explode();
        }
        if (impactParticle)
        {
            CreateImpaceEffect(collision);
        }

        PoolManager.Instance.ReleaseObject(gameObject);
    }

    protected virtual void CreateImpaceEffect(Transform parent = null)
    {   
        // 필요할 경우 Pooling
        // Pooling 할 경우 Scale 초기화 할 것
        impactP = Instantiate(impactParticle, transform.position, Quaternion.LookRotation(-transform.forward), parent);
        Destroy(impactP, 2.0f);

        if (parent != null && parent.transform.localScale != Vector3.one)
        {
            // Scale이 1이 아닐 경우 이펙트의 Scale 문제가 발생할 수 있음
            impactP.transform.localScale /= parent.transform.localScale.z;
        }
    }

    protected virtual void CreateImpaceEffect(Collision collision)
    {
        // 필요할 경우 Pooling
        Vector3 contactP = collision.contacts[0].point;
        Vector3 contactN = collision.contacts[0].normal;
        impactP = Instantiate(impactParticle, contactP, Quaternion.FromToRotation(Vector3.up, contactN), collision.transform);
        Destroy(impactP, 5.0f);

        if (collision != null && collision.transform.localScale != Vector3.one)
        {
            // Scale이 1이 아닐 경우 이펙트의 Scale 문제가 발생할 수 있음
            impactP.transform.localScale /= collision.transform.localScale.z;
        }
    }

    protected virtual void Explode()
    {
        if (explosionEffectPrefab)
        {
            // 필요할 경우 Pooling
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    protected void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        IDamagable monster = target.GetComponent<IDamagable>();
        if (monster != null)
        {
            monster.ApplyDamage(_damage * coefficient);
        }
        else
        {
            monster = target.transform.GetComponentInParent<IDamagable>();
            if (monster != null)
            {
                monster.ApplyDamage(_damage * coefficient);
            }
        }
    }
    #endregion
}
