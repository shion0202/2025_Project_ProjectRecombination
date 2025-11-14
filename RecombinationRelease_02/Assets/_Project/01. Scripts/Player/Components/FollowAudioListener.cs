using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAudioListener : MonoBehaviour
{
    [SerializeField] private Transform positionTarget;
    [SerializeField] private Transform rotationTarget;

    private void Update()
    {
        transform.position = positionTarget.position;
        transform.rotation = rotationTarget.rotation;
    }
}
