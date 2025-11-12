using _Project.Scripts.VisualScripting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BreakableData
{
    public float explosionForce;
    public Vector3 explosionPosition;
    public float explosionRadius;
    public float upwardsModifier;
    public ForceMode forceMode;
}

// 오브젝트를 물리적으로 파괴하는 연출 Output
public class BreakObject : ProcessBase
{
    [SerializeField] private GameObject breakableObject;
    [SerializeField] private BreakableData data;
    [SerializeField] private float lapseTime = 5.0f;
    [SerializeField] private LayerMask obstacleLayers; // 파괴된 오브젝트와 충돌하지 않을 레이어
    private Coroutine _breakRoutine = null;

    private void Awake()
    {
        if (obstacleLayers == 0)
        {
            obstacleLayers |= (1 << LayerMask.NameToLayer("Breakable"));
        }
    }

    public override void Execute()
    {
        if (IsOn) return;
        if (breakableObject == null) return;

        BreakWall();
    }

    public void BreakWall()
    {
        foreach (Transform child in breakableObject.transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddExplosionForce(data.explosionForce, child.position + data.explosionPosition, data.explosionRadius, data.upwardsModifier, data.forceMode);
            }

            MeshCollider mc = child.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.excludeLayers = obstacleLayers;
            }
        }

        if (_breakRoutine == null)
        {
            _breakRoutine = StartCoroutine(CoWallDisable());
        }
    }

    private IEnumerator CoWallDisable()
    {
        yield return new WaitForSeconds(lapseTime);

        // 현재는 그냥 삭제하며, 추후 로직 변경 가능
        Destroy(breakableObject);
    }

    public override string ToString()
    {
        string objectName = gameObject.name;
        string breakableObjectName = breakableObject != null ? breakableObject.name : "null";
        string breakData = $"Force: {data.explosionForce:F2}, Position: {data.explosionPosition}, Radius: {data.explosionRadius:F2}, UpwardsMod: {data.upwardsModifier:F2}, ForceMode: {data.forceMode}";
        string layerData = LayerMask.LayerToName(obstacleLayers);
        string routineState = (_breakRoutine != null) ? "Running" : "Stopped";

        string log = $"[{objectName} ({GetType().Name})] IsOn: {IsOn}, breakableObject: {breakableObjectName}, BreakableData: ({breakData}), ObstacleLayer: ({layerData}), LapseTime: {lapseTime:F2}, BreakRoutine: {routineState}";
        return log;
    }
}
