using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 카메라 회전에 의해 오브젝트 Rotation을 회전시키는 스크립트
public class RotateByCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // 카메라의 Y축 회전에만 반응
        transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
    }

    public override string ToString()
    {
        string log = $"[{gameObject.name} ({GetType().Name})] Camera Rotation: {cameraTransform.rotation}, Object Rotation: {transform.rotation}";
        return log;
    }
}
