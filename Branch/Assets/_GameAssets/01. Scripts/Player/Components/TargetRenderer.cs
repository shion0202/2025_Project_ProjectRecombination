using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetRenderer : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _smr;
    public SkinnedMeshRenderer Smr
    {
        get => _smr;
    }

    private void Start()
    {
        _smr = gameObject.GetComponent<SkinnedMeshRenderer>();
    }
}
