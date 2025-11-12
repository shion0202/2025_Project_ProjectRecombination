using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraWalk : MonoBehaviour
{
    [Header("Camera movement speed")]
    public float moveSpeed = 10.0f;
    [Header("Camera rotation sensitivity (mouse)")]
    public float rotateSpeed = 500.0f;
    [Header("Camera zoom speed")]
    public float zoomSpeed = 10.0f;
    [Header("Camera zoom minimum/maximum values")]
    public float minFov = 15.0f;
    public float maxFov = 90.0f;

    protected bool isCursorVisible = true;

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {
        myCameraWalk();
    }
    protected virtual void myCameraWalk()
    {
        // Move the camera with WASD
        float horizontal = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float vertical = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        transform.Translate(horizontal, 0, vertical);
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Translate(0, -moveSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Translate(0, moveSpeed * Time.deltaTime, 0);
        }
        // Zoom in/out with the mouse wheel
        float fov = GetComponent<Camera>().fieldOfView;
        fov -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        GetComponent<Camera>().fieldOfView = fov;

        // Show/hide the mouse cursor with the right mouse button
        VisibleMouse();

        // 
        if (!isCursorVisible)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

            transform.eulerAngles = new Vector3(transform.eulerAngles.x - mouseY, transform.eulerAngles.y + mouseX, 0);
        }
    }
    protected void VisibleMouse()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorVisible = !isCursorVisible;
            Cursor.visible = isCursorVisible;
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
