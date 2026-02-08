using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_TutorialMenuButton : MonoBehaviour
{
    [SerializeField] private string tutorialKey;
    [SerializeField] private UI_Tutorial controller;

    public void OnClick()
    {
        controller.ShowTutorialByKey(tutorialKey);
    }
}
