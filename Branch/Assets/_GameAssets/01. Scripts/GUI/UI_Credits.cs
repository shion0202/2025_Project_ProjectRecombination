using Managers;
using UnityEngine;

public class UI_Credits : MonoBehaviour
{
    [SerializeField] private float creditSpeed = 100.0f;
    [SerializeField] private float targetPosition = 1600.0f;
    private Vector3 _creditPosition;

    private void Update()
    {
        if (gameObject.transform.position.y >= targetPosition)
        {
            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;

            // SceneManager.LoadScene("TitleScene");
            GameManager.Instance.EnterTitle();
        }

        _creditPosition = new Vector3(0.0f, Time.deltaTime * creditSpeed, 0.0f);
        gameObject.transform.position += _creditPosition;
    }
}
