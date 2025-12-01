using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAudioListener : MonoBehaviour
{
    [SerializeField] private Transform positionTarget;
    [SerializeField] private Transform rotationTarget;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool isYRotation = false;

    private void Update()
    {
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
}
