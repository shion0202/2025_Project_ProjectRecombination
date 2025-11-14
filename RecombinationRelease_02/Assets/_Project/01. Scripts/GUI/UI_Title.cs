using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Title : MonoBehaviour
{
    public void OnClickStart()
    {
        SceneManager.LoadScene("PrologueScene");
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;    // 에디터에서는 플레이 중단
#else
        Application.Quit();                                 // 빌드에서는 프로그램 종료
#endif
    }
}
