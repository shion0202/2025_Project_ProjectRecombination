using UnityEngine;
using Managers;

public class UI_Title : MonoBehaviour
{
    public void OnClickStart()
    {
        GameManager.Instance.EnterPrologue();
    }

    public void OnClickExit()
    {
        GameManager.Instance.ExitGame();
    }
}
