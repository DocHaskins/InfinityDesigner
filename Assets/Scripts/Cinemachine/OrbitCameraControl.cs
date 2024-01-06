using UnityEngine;
using Cinemachine;

public class OrbitCameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    private CinemachineOrbitalTransposer orbitalTransposer;
    public Transform cameraParent; // Parent object to rotate for vertical movement
    public float sensitivity = 1.0f;

    void Start()
    {
        orbitalTransposer = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) // Change 0 to 1 if you want right-click drag
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            orbitalTransposer.m_XAxis.Value += x * sensitivity;

            // Rotate the parent object for vertical movement
            cameraParent.Rotate(Vector3.right, y * sensitivity);
        }
    }
}