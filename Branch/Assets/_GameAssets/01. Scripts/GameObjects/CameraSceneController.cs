using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSceneController : MonoBehaviour
{
    [SerializeField] private GameObject vcCredit;

    private void Update()
    {
        if (GameManager.Instance == null || vcCredit == null) return;

        bool isCreditState = GameManager.Instance.CurrentState == GameManager.GameState.Credit;
        if (vcCredit.activeSelf != isCreditState)
        {
            vcCredit.SetActive(isCreditState);
        }
    }
}
