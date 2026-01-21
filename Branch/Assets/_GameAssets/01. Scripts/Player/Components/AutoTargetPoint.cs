using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 타겟 이동 시 서서히 타겟을 따라가는 Target Point
public class AutoTargetPoint : MonoBehaviour
{
    // 따라갈 플레이어 Transform
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private float moveSpeed = 1.0f;

    private void Awake()
    {
        if (target != null)
        {
            transform.position = target.position + positionOffset;
        }
    }

    private void Update()
    {
        if (target != null)
        {
            // 이동 중에는 항상 Target Point가 타겟과 분리되어 있으므로, 가속도를 추가하는 식으로 조정 가능
            transform.position = Vector3.Lerp(transform.position, target.position + positionOffset, Time.deltaTime * moveSpeed);
        }
    }

    public override string ToString()
    {
        string log = $"[{gameObject.name} ({GetType().Name})] Target: {target.name}, Current Position({transform.position}), Target Position({target.transform.position})";
        return log;
    }
}
