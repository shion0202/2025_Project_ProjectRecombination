using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_RadialButtonFirst : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int partPosIndex;

    private void Start()
    {
        // 이 스크립트가 부착된 게임오브젝트의 Image 컴포넌트를 가져옵니다.
        Image image = GetComponent<Image>();

        // 알파 값의 최소 한계치를 설정합니다.
        // 이 값(0.1f)보다 알파가 낮은 픽셀은 클릭(Raycast)되지 않습니다.
        // 0.0 ~ 1.0 사이의 값으로, 이미지에 맞게 조절할 수 있습니다.
        image.alphaHitTestMinimumThreshold = 0.1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Managers.GUIManager.Instance.SelectPartPosition(partPosIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Managers.GUIManager.Instance.UnselectPartPosition(partPosIndex);
    }
}
