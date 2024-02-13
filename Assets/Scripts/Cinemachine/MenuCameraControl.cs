using UnityEngine;
using Cinemachine;

public class MenuCameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public float sensitivity = 0.1f; // Adjust this value to change how much the camera moves
    public float smoothTime = 0.3f; // Adjust for smoother or more immediate movement

    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (virtualCamera != null)
        {
            // Initialize targetPosition with the current camera position
            targetPosition = virtualCamera.transform.position;
        }
    }

    void Update()
    {
        if (virtualCamera != null)
        {
            // Calculate new target position based on mouse input
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            // Adjust target position based on mouse movement
            targetPosition += new Vector3(mouseX, mouseY, 0);

            // Smoothly move the camera towards the target position
            virtualCamera.transform.position = Vector3.SmoothDamp(virtualCamera.transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}