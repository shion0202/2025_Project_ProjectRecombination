using Managers;
using UnityEngine;

public class GoToEpilogue : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 에필로그 씬으로 전환
            // SceneManager.LoadScene("EpilogueScene");
            GameManager.Instance.EnterEpilogue();
        }
    }
}
