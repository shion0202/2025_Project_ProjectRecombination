using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 인공 중력 스킬
/// </summary>
public class ArtificialGravity : MonoBehaviour
{
    // 객체가 씬에 존재하는 동안 트리거에 충돌된 모든 객체를 중앙으로 1회 끌어당김
    private HashSet<GameObject> _collidedObjects = new HashSet<GameObject>();
    private bool _hasActivated = false; // 스킬이 이미 활성화되었는지 여부
    [SerializeField] private float pullForce = 10f; // 끌어당기는 힘
    // [SerializeField] private float duration = 3f; // 스킬 지속 시간
    [SerializeField] private float pullDuration = 0.5f; // 끌어당기는 시간
    [SerializeField] private float radius = 5f; // 스킬 범위
    // [SerializeField] private LayerMask affectedLayers; // 영향을 받을 레이어
    [SerializeField] private ParticleSystem gravityEffect; // 중력 효과 파티클 시스템
    private SphereCollider _sphereCollider;
    private void Awake()
    {
        _sphereCollider = GetComponent<SphereCollider>();
        if (_sphereCollider == null)
        {
            Debug.LogError("SphereCollider component is missing.");
        }
        else
        {
            _sphereCollider.isTrigger = true;
            _sphereCollider.radius = radius;
        }

        // if (gravityEffect != null)
        // {
        //     var main = gravityEffect.main;
        //     main.duration = duration;
        //     gravityEffect.Stop();
        // }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        // 플레이어를 대상으로 설정
        _collidedObjects.Add(other.gameObject);
        
        if (!_hasActivated)
        {
            _hasActivated = true;
            StartCoroutine(ActivateSkill());
        }
    }
    
    private IEnumerator ActivateSkill()
    {
        // 중력 효과 시작
        if (gravityEffect != null)
        {
            gravityEffect.Play();
        }

        float elapsed = 0f;
        // while (elapsed < duration)
        // {
        //     elapsed += Time.deltaTime;
        //     yield return null;
        // }

        // 끌어당기는 효과 시작
        elapsed = 0f;
        while (elapsed < pullDuration)
        {
            foreach (GameObject obj in _collidedObjects)
            {
                // if (obj != null)
                // {
                //     Vector3 direction = (transform.position - obj.transform.position).normalized;
                //     // if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
                //     // {
                //     //     rb.AddForce(direction * pullForce, ForceMode.Acceleration);
                //     // }
                //     obj.transform.position += direction * (pullForce * Time.deltaTime);
                // }
                CharacterController controller = obj?.GetComponent<CharacterController>();
                if (controller == null) continue;

                Vector3 direction = (transform.position - obj.transform.position).normalized;
                
                // 중력 방향으로 이동
                // 이동하되 바닥에 붙어 있도록 함
                direction.y = -1f; // 아래 방향으로 약간의 힘 추가
                direction.Normalize();
                
                controller.Move(direction * (pullForce * Time.deltaTime));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 중력 효과 종료
        if (gravityEffect != null)
        {
            gravityEffect.Stop();
        }

        // 초기화
        _collidedObjects.Clear();
        _hasActivated = false;
        Destroy(gameObject);
    }
}
