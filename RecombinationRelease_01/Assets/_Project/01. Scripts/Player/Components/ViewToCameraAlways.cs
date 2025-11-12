using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 오브젝트가 카메라 방향을 향하도록 하는 스크립트
public class ViewToCameraAlways : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        LookCamera();
    }

    private void Update()
    {
        LookCamera();
    }

    private void LookCamera()
    {
        Vector3 direction = transform.position - cameraTransform.position;
        direction.y = 0; // 수평 회전만 원한다면

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public override string ToString()
    {
        string cameraName = cameraTransform != null ? cameraTransform.name : "Null";
        string log = $"[{gameObject.name} ({GetType().Name})] Target Camera: {cameraName}";
        return log;
    }
}
