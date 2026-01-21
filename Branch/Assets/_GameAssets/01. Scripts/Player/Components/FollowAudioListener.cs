using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAudioListener : MonoBehaviour
{
    [SerializeField] private Transform positionTarget;
    [SerializeField] private Transform rotationTarget;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool isYRotation = false;

    private bool _isInit;

    private void Update()
    {
        if (!_isInit) return;
        
        transform.position = positionTarget.position + offset;

        if (isYRotation)
        {
            Vector3 currentEuler = transform.rotation.eulerAngles;
            Vector3 targetEuler = rotationTarget.rotation.eulerAngles;

            transform.rotation = Quaternion.Euler(currentEuler.x, targetEuler.y, currentEuler.z);
        }
        else
        {
            transform.rotation = rotationTarget.rotation;
        }
    }

    public void Init(GameObject player)
    {
        if (_isInit) return;
        
        positionTarget = player.GetComponent<Transform>();
        rotationTarget = player.GetComponent<Transform>();
        
        _isInit = true;
    }

    public void Unload()
    {
        if (!_isInit) return;
        _isInit = false;
        
        positionTarget = null;
        rotationTarget = null;
    }
}
