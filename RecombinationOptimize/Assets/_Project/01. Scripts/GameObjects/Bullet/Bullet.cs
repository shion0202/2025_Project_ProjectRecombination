using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Monster.AI;
using Managers;
using _Project._01._Scripts.Monster;
using Monster.AI.FSM;
using System.Linq;
using Unity.VisualScripting;

public class Bullet : MonoBehaviour
{
    #region Variables
    [Header("Components")]
    [SerializeField] protected Rigidbody _rb;

    [Header("Bullet Information")]
    [SerializeField] protected float bulletSpeed = 30.0f;
    [SerializeField] protected float explosionRadius = 2.0f;
    [SerializeField] private float lifeTime = 5f; // 총알의 생명 시간
    [SerializeField] protected LayerMask targetMask;
    [SerializeField] private List<string> collisionTags = new();
    private float _timer;
    private float _damage;
    private GameObject _from; // 발사 주체
    protected Vector3 _to; // 타겟 위치
    protected Transform _parent; // Spawn Point (Muzzle Flash 등의 Position을 위한 변수)
    protected Vector3 _targetDirection;
    protected bool _isCollided = false;
    protected List<Transform> _damagedTargets = new();

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

    public LayerMask TargetMask
    {
        get => targetMask;
        set => targetMask = value;
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

        if (targetMask == 0)
        {
            targetMask |= (1 << LayerMask.NameToLayer("Enemy"));
            targetMask |= (1 << LayerMask.NameToLayer("Damagable"));
        }

        if (collisionTags.Count <= 0)
        {
            collisionTags.Add("Wall");
            collisionTags.Add("Breakable");
            collisionTags.Add("Obstacle");
            collisionTags.Add("Platform");
        }
    }

    protected virtual void Start()
    {
        _timer = lifeTime;
    }

    protected void OnEnable()
    {
        ResetBullet();
    }

    protected void OnDisable()
    {
        ResetBullet();
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
        if (From && other && From.CompareTag("Player") && other.CompareTag("Enemy") &&
            ((1 << other.gameObject.layer) & (1 << LayerMask.NameToLayer("MonsterDead"))) == 0)
        {
            ShootByPlayer(other);
            return;
        }
        
        // 적이 발사한 총알
        if (From && other && From.CompareTag("Enemy") && other.CompareTag("Player"))
        {
            ShootByEnemy(other);
            return;
        }
        
        // 벽(또는 기타 오브젝트)에 닿은 경우
        if (other && collisionTags.Contains(other.gameObject.tag))
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
        if (From.CompareTag("Player") && collision.gameObject.CompareTag("Enemy") &&
            ((1 << collision.gameObject.layer) & (1 << LayerMask.NameToLayer("MonsterDead"))) == 0)
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
        if (collision.gameObject && collisionTags.Contains(collision.gameObject.tag))
        {
            ImpactObstacle(collision);
            return;
        }
    }
    #endregion

    #region Public Methods
    public void Init(GameObject shooter, Transform target, Vector3 start, Vector3 end, Vector3 direction, float damage)
    {
        ResetBullet();

        _from = shooter;
        _to = end;
        _targetDirection = direction;
        _damage = damage;

        SetBulletLogic(target, direction, start);

        if (muzzleParticlePrefab)
        {
            if (_parent)
            {
                muzzleParticle = Utils.Instantiate(muzzleParticlePrefab, _parent.position, Quaternion.LookRotation(-_targetDirection), Parent);
            }
            else
            {
                muzzleParticle = Utils.Instantiate(muzzleParticlePrefab, transform.position, Quaternion.LookRotation(-_targetDirection));
            }

            Utils.Destroy(muzzleParticle, 1.0f); // Lifetime of muzzle effect.
        }
    }

    public virtual void ResetBullet()
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _timer = lifeTime;
        muzzleParticle = null;
        impactP = null;

        _isCollided = false;
        _damagedTargets.Clear();
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
        TakeDamage(other.transform);
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
        TakeDamage(collision.transform);
        DestroyBullet(collision);
    }

    protected virtual void ImpactObstacle(Collision collision)
    {
        DestroyBullet(collision);
    }

    protected virtual void DestroyBullet(Transform parent = null)
    {
        if (_isCollided) return;
        _isCollided = true;

        // 풀링 전 총알의 상태를 초기화
        ResetBullet();

        if (impactParticle)
        {
            CreateImpaceEffect();
        }
        if (explosionEffectPrefab)
        {
            Explode();
        }

        Utils.Destroy(gameObject);
    }

    protected virtual void DestroyBullet(Collision collision)
    {
        if (_isCollided) return;
        _isCollided = true;

        // 풀링 전 총알의 상태를 초기화
        ResetBullet();

        if (impactParticle)
        {
            CreateImpaceEffect(collision);
        }
        if (explosionEffectPrefab)
        {
            Explode();
        }

        Utils.Destroy(gameObject);
    }

    protected virtual void CreateImpaceEffect()
    {   
        impactP = Utils.Instantiate(impactParticle, transform.position, Quaternion.LookRotation(-transform.forward));
        Utils.Destroy(impactP, 2.0f);
    }

    protected virtual void CreateImpaceEffect(Collision collision)
    {
        Vector3 contactP = collision.contacts[0].point;
        Vector3 contactN = collision.contacts[0].normal;
        impactP = Utils.Instantiate(impactParticle, contactP, Quaternion.FromToRotation(Vector3.up, contactN));
        Utils.Destroy(impactP, 2.0f);

        if (collision != null && collision.transform.localScale != Vector3.one)
        {
            // Scale이 1이 아닐 경우 이펙트의 Scale 문제가 발생할 수 있음
            // Y Scale을 기준으로 삼았으나 추후 수정될 수 있음
            impactP.transform.localScale /= collision.transform.localScale.y;
        }
    }

    protected virtual void Explode()
    {
        if (explosionEffectPrefab)
        {
            Utils.Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    protected virtual void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        float hitZoneValue = 1.0f;
        PartialBlow partialBlow = target.GetComponent<PartialBlow>();
        if (partialBlow)
        {
            hitZoneValue = partialBlow.fValue;
        }

        IDamagable enemy = target.GetComponent<IDamagable>();
        if (enemy != null)
        {
            Transform otherParent = target.transform;
            if (_damagedTargets.Contains(otherParent)) return;
            _damagedTargets.Add(otherParent);

            enemy.ApplyDamage(_damage * coefficient * hitZoneValue, targetMask);
            //Debug.Log($"원본 데미지: {_damage * coefficient}, 육질 데미지: {_damage * coefficient * hitZoneValue}");

            if (_from.CompareTag("Player"))
            {
                Managers.GUIManager.Instance.StartHitCrosshair();
            }
        }
        else
        {
            enemy = target.transform.GetComponentInParent<IDamagable>();
            if (enemy != null)
            {
                FSM fsm = target.transform.GetComponentInParent<FSM>();
                if (!fsm) return;

                Transform otherParent = fsm.transform;
                if (_damagedTargets.Contains(otherParent)) return;
                _damagedTargets.Add(otherParent);

                enemy.ApplyDamage(_damage * coefficient * hitZoneValue, targetMask);
                //Debug.Log($"원본 데미지: {_damage * coefficient}, 육질 데미지: {_damage * coefficient * hitZoneValue}");

                if (_from.CompareTag("Player"))
                {
                    Managers.GUIManager.Instance.StartHitCrosshair();
                }
            }
        }
    }
    #endregion
}
