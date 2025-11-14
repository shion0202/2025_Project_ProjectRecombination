using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_RadialButtonSecond : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int partIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Managers.GUIManager.Instance.SelectedPartIndex = partIndex;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Managers.GUIManager.Instance.SelectedPartIndex = -1;
    }
}
