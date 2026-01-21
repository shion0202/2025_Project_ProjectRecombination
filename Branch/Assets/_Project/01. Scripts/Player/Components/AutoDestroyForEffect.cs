using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyForEffect : MonoBehaviour
{
    private Transform _parent;

    private void Update()
    {
        if (!_parent.gameObject.activeInHierarchy)
        {
            Utils.Destroy(gameObject);
        }
    }

    public void Init(Transform inParent)
    {
        _parent = inParent;
    }
}
