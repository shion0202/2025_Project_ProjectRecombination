using _Project._01._Scripts.Monster;
using Managers;
using Monster.AI.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionMissile : MonoBehaviour
{
    [SerializeField] protected GameObject collisionBulletPrefab;
    [SerializeField] protected float explosionRadius;
    [SerializeField] protected LayerMask targetMask;
    [SerializeField] protected float _damage;
    [SerializeField] protected float debuffTime = 10.0f;
    protected GameObject _owner;
    protected List<Transform> _damagedTargets = new();
    protected float _curLifeTime;
    protected Coroutine _damageRoutine = null;

    protected void Start()
    {
        _curLifeTime = debuffTime;
        _damagedTargets.Clear();

        // OnEnable에서 실행시키면 제대로 실행이 안 되는 문제 있음 (풀링 시 주의)
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, targetMask);
        _damageRoutine = StartCoroutine(ProcessExplosionColliders(colliders));
    }

    protected void OnEnable()
    {
        //_curLifeTime = debuffTime;
        //_damagedTargets.Clear();
    }

    protected void OnDisable()
    {
        _curLifeTime = debuffTime;
        _damagedTargets.Clear();
        _damageRoutine = null;
    }

    protected void Update()
    {
        if (_curLifeTime > 0.0f)
        {
            _curLifeTime -= Time.deltaTime;
        }
        else
        {
            if (_damageRoutine == null)
            {
                DestroyExplosion();
            }
        }
    }

    public void Init(GameObject owner, float damage, float radius, LayerMask targetMask)
    {
        _owner = owner;
        _damage = damage;
        explosionRadius = radius;
        this.targetMask = targetMask;
    }

    protected virtual void DestroyExplosion()
    {
        _damagedTargets.Clear();
        Utils.Destroy(gameObject);
    }

    protected virtual void TakeDamage(Transform target, float coefficient = 1.0f)
    {
        IDamagable enemy = target.GetComponent<IDamagable>();
        if (enemy != null)
        {
            Transform otherParent = target.transform;
            if (_damagedTargets.Contains(otherParent)) return;
            _damagedTargets.Add(otherParent);

            enemy.ApplyDamage(_damage * coefficient, targetMask);

            if (_owner && _owner.CompareTag("Player"))
            {
                GUIManager.Instance.GameUIController.StartHitCrosshair();
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

                enemy.ApplyDamage(_damage * coefficient, targetMask);

                if (_owner && _owner.CompareTag("Player"))
                {
                    GUIManager.Instance.GameUIController.StartHitCrosshair();
                }
            }
        }
    }

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
            //yield return null;
        }

        if (_curLifeTime <= 0.0f)
        {
            DestroyExplosion();
        }

        _damageRoutine = null;
        yield break;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 폭발 반경을 빨간색 반투명 구로 표시
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, explosionRadius);

        // 폭발 반경 외곽선(선택사항)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
