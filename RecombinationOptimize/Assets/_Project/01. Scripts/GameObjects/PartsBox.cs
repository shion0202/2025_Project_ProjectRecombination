using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartsBox : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            Managers.GUIManager.Instance.SetPartIndicatorTarget(target);
            Managers.GUIManager.Instance.SetIndicator(true, false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            Managers.GUIManager.Instance.SetPartIndicatorTarget(null);
        }
    }
}
