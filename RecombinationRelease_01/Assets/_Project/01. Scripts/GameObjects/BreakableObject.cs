using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// GOS의 BreakObject(오브젝트 붕괴)를 테스트하기 위한 스크립트
public class BreakableObject : MonoBehaviour
{
    [SerializeField] private BreakableData data;
    [SerializeField] private float lapseTime = 5.0f;
    [SerializeField] private LayerMask obstacleLayers; // 파괴된 오브젝트와 충돌하지 않을 레이어

    private List<Rigidbody> _rigidbodies = new();
    private List<Vector3> _originalPositions = new();
    private List<Quaternion> _originalRotations = new();
    private Coroutine _breakRoutine;

    private void Awake()
    {
        // 초기 위치, 회전, Rigidbody 저장
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _rigidbodies.Add(rb);
                _originalPositions.Add(child.position);
                _originalRotations.Add(child.rotation);
            }
        }

        // Force 값이 0일 경우, 즉 Break 값이 할당되지 않았을 경우 기본값 설정
        if (data.explosionForce == 0.0f)
        {
            data.explosionForce = 500.0f;
            data.explosionPosition = Vector3.zero;
            data.explosionRadius = 5.0f;
            data.upwardsModifier = 0.0f;
            data.forceMode = ForceMode.Impulse;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            BreakWall();
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            ResetWall();
        }
    }

    public void BreakWall()
    {
        foreach (Rigidbody rb in _rigidbodies)
        {
            rb.isKinematic = false;
            rb.AddExplosionForce(data.explosionForce, transform.position + data.explosionPosition, data.explosionRadius, data.upwardsModifier, data.forceMode);

            MeshCollider mc = rb.gameObject.GetComponent<MeshCollider>();
            if (mc != null)
            {
                // 플레이어 이동을 방해하지 않도록 Player 레이어 제외
                // 폐쇄된 공간에서 파괴 연출이 정상적으로 보이도록 Breakable 레이어도 제외
                mc.excludeLayers = obstacleLayers;
            }
        }

        if (_breakRoutine != null)
        {
            StopCoroutine(_breakRoutine);
            _breakRoutine = null;
        }
        _breakRoutine = StartCoroutine(CoWallDisable());
    }

    public void ResetWall()
    {
        if (_breakRoutine != null)
        {
            StopCoroutine(_breakRoutine);
            _breakRoutine = null;
        }

        for (int i = 0; i < _rigidbodies.Count; i++)
        {
            Rigidbody rb = _rigidbodies[i];
            rb.isKinematic = true; // 물리 일시 정지
            rb.gameObject.SetActive(true);

            rb.transform.position = _originalPositions[i];
            rb.transform.rotation = _originalRotations[i];

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            //rb.isKinematic = false; // 물리 다시 활성화

            MeshCollider mc = rb.gameObject.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.excludeLayers = 0; // 모든 레이어와 충돌
            }
        }
    }

    private IEnumerator CoWallDisable()
    {
        yield return new WaitForSeconds(lapseTime);

        for (int i = 0; i < _rigidbodies.Count; i++)
        {
            Rigidbody rb = _rigidbodies[i];
            rb.gameObject.SetActive(false);
        }
    }
}